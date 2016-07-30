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
    public class Tracker<TRecord, TKey> : IDisposable
        where TRecord : INotifyPropertyChanged, ITrackable, IRecord<TKey>
    {
        private ITypedTable<TRecord> table;

        private Subject<DatabaseUpdatedEventArgs> UpdatedSubject { get; }
        public IObservable<DatabaseUpdatedEventArgs> Updated => this.UpdatedSubject.AsObservable();


        public double AutoSaveTimeMilliseconds { get; set; } = 300;
        public bool IsAutoSaving { get; set; } = true;

        private readonly Subject<PropertyChangedContainer<TRecord>> updateSubject;
        public IObservable<PropertyChangedContainer<TRecord>> PropertyChanged => this.updateSubject.AsObservable();

        //private readonly Subject<TRecord> savingSubject;
        //public IObservable<TRecord> Saving => this.savingSubject.AsObservable();


        public Tracker(ITypedTable<TRecord> table)
        {
            this.table = table;

            this.UpdatedSubject = new Subject<DatabaseUpdatedEventArgs>().AddTo(this.Disposables);
            this.updateSubject = new Subject<PropertyChangedContainer<TRecord>>().AddTo(this.Disposables);
            //this.savingSubject = new Subject<TRecord>().AddTo(this.Disposables);

            this.updateSubject
                .Where(x => this.IsAutoSaving && this.table.TargetProperties.ContainsKey(x.PropertyName))
                .Buffer(this.updateSubject.Throttle(TimeSpan.FromMilliseconds(this.AutoSaveTimeMilliseconds)))
                .Where(x => x.Count > 0)
                .Subscribe(updatedItems =>
                {
                    //Debug.WriteLine(updatedItems.Count);

                    Task.Run(async () =>
                    {
                        var succeeded = false;
                        using (var connection = this.table.Parent.Connect())
                        {
                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    //Parallel.ForEach(updatedItems.GroupBy(x => x.Source.Id), x =>
                                    //{
                                    //    
                                    //    x.ForEach(item =>
                                    //    {
                                    //        this.table.Update
                                    //            (item.Source, connection, transaction, item.PropertyName);
                                    //
                                    //    });
                                    //});

                                    //Debug.WriteLine(updatedItems.Select(y=>y.PropertyName).Join(","));

                                    //updatedItems.ForEach(item =>
                                    //{
                                    //    this.table.Update
                                    //        (item.Source, connection, transaction, item.PropertyName);
                                    //
                                    //});

                                    //updatedItems.GroupBy(x => x.Source).ForEach(item =>
                                    foreach(var item in updatedItems.GroupBy(x => x.Source))
                                    {
                                        var properties = item.Select(x => x.PropertyName).Distinct().ToArray();
                                        await this.table.UpdateAsync
                                            (item.Key, connection, transaction,
                                            item.Select(x => x.PropertyName).ToArray());

                                        Debug.WriteLine(item.Key.Id.ToString() + " update " + properties.Join(","));

                                    }//);

                                    transaction.Commit();
                                    succeeded = true;
                                }
                                catch
                                {
                                    transaction.Rollback();
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
                        //using (var connection = this.table.Parent.ConnectAsThreadSafe())
                        //{
                        //    using (var transaction = connection.Value.BeginTransaction())
                        //    {
                        //        try
                        //        {
                        //            updatedItems.AsParallel().ForAll(item =>
                        //            {
                        //                this.table.Update
                        //                    (item.Source, connection.Value, transaction, item.PropertyName);
                        //
                        //            });
                        //
                        //            transaction.Commit();
                        //        }
                        //        catch
                        //        {
                        //            transaction.Rollback();
                        //        }
                        //    }
                        //}
                    });

                    //await this.table.RequestTransactionAsync(context =>
                    //{
                    //    foreach (var item in updatedItems)
                    //    {
                    //        this.table.Update
                    //            (item.Source, context.Connection, context.Transaction, item.PropertyName);
                    //
                    //    }
                    //});


                    //var groups = updatedItems.GroupBy(x => x.Source.Id);
                    //
                    //this.table.RequestTransaction(context =>
                    //{
                    //    foreach (var group in groups)
                    //    {
                    //        var target = group.Last().Source;
                    //        var properties = group.Select(x => x.PropertyName).Distinct().ToArray();
                    //
                    //        this.table.Update
                    //            (target, context.Connection, context.Transaction, properties);
                    //
                    //        //this.savingSubject.OnNext(target);
                    //    }
                    //});
                })
                .AddTo(this.Disposables);
        }

        public void Track(TRecord target)
        {
            target.PropertyChanged += (o, e)
                => this.RequestSaving(new PropertyChangedContainer<TRecord>((TRecord)o, e.PropertyName));
            target.IsLoaded = true;
            /*
            target.PropertyChanged += (o, e) =>
            {
                this.table.RequestTransaction(context =>
                {
                    this.table.Update((T)o, context.Connection, context.Transaction, e.PropertyName);
                });
            };*/
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
        public void Dispose() => this.Disposables.Dispose();
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
