using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Reactive.Bindings.Extensions;

namespace Database.Table
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class Tracker<TRecord, TKey> : IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
        where TRecord : INotifyPropertyChanged, ITrackable, IRecord<TKey>
    {
        private ITypedTable<TRecord> table;

        private Subject<DatabaseUpdatedEventArgs> UpdatedSubject { get; }
        public IObservable<DatabaseUpdatedEventArgs> Updated => this.UpdatedSubject.AsObservable();

        private const double autoSaveTimeMillisecondsDefault = 300.0;
        public double AutoSaveTimeMilliseconds { get; } = autoSaveTimeMillisecondsDefault;
        public bool IsAutoSaving { get; set; } = true;

        private readonly Subject<PropertyChangedContainer<TRecord>> updateSubject;
        public IObservable<PropertyChangedContainer<TRecord>> PropertyChanged => this.updateSubject.AsObservable();

        public Tracker(ITypedTable<TRecord> table) : this(table, autoSaveTimeMillisecondsDefault)
        {
        }

        public Tracker(ITypedTable<TRecord> table, double autoSaveTimeMilliseconds)
        {
            this.table = table;

            this.UpdatedSubject = new Subject<DatabaseUpdatedEventArgs>().AddTo(this.Disposables);
            this.updateSubject = new Subject<PropertyChangedContainer<TRecord>>().AddTo(this.Disposables);

            this.AutoSaveTimeMilliseconds = autoSaveTimeMilliseconds;

            var update = this.updateSubject
                .Where(x => this.IsAutoSaving && this.table.TargetProperties.ContainsKey(x.PropertyName))
                .Publish().RefCount();

            update
                .Buffer(update.Throttle(TimeSpan.FromMilliseconds(this.AutoSaveTimeMilliseconds)))
                .Where(x => x.Count > 0)
                .Subscribe(updatedItems =>
                {

                    Task.Run(async () =>
                    {
                        var succeeded = false;
                        using (var connection = await this.table.Parent.ConnectAsync())
                        {
                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    foreach (var item in updatedItems.GroupBy(x => x.Source))
                                    {
                                        var properties = item.Select(x => x.PropertyName).Distinct().ToArray();
                                        await this.table.UpdateAsync
                                            (item.Key, connection, transaction, properties)
                                            .ConfigureAwait(false);

                                        Debug.WriteLine(item.Key.Id.ToString() + " update " + properties.Join(","));

                                    }

                                    transaction.Commit();
                                    succeeded = true;
                                }
                                catch (Exception e)
                                {
                                    transaction.Rollback();
                                    Debug.WriteLine(e.ToString());
                                }
                            }
                        }

                        if (succeeded)
                        {
                            this.UpdatedSubject.OnNext(new DatabaseUpdatedEventArgs()
                            {
                                Sender = this,
                                Action = DatabaseAction.Update,
                            });
                        }
                    });

                })
                .AddTo(this.Disposables);
        }

        public void Track(TRecord target)
        {
            target.PropertyChanged += (o, e)
                => this.RequestSaving(new PropertyChangedContainer<TRecord>((TRecord)o, e.PropertyName));
            target.IsLoaded = true;
        }


        private void RequestSaving(PropertyChangedContainer<TRecord> source)
        {
            if (this.updateSubject.HasObservers && source.Source.IsLoaded)
            {
                this.updateSubject.OnNext(source);
            }
        }




        public bool IsDisposed => this.Disposables.IsDisposed;
        private CompositeDisposable Disposables { get; } = new CompositeDisposable();
#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose() => this.Disposables.Dispose();
#pragma warning restore CA1063 // Implement IDisposable Correctly
    }

    public struct PropertyChangedContainer<T>
    {
        public T Source { get; }
        public string PropertyName { get; }

        public PropertyChangedContainer(T source, string propertyName)
        {
            this.Source = source;
            this.PropertyName = propertyName;
        }
    }

    public interface ITrackable
    {
        bool IsLoaded { get; set; }
    }
}
