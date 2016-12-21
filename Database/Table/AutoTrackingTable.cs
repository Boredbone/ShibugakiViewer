using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;
using System.Data;

namespace Database.Table
{
    public class AutoTrackingTable<TRecord, TKey> : DisposableBase
        where TRecord : INotifyPropertyChanged, ITrackable, IRecord<TKey>
    {
        public TypedTable<TRecord, TKey> Table { get; }
        public Tracker<TRecord, TKey> Tracker { get; }

        public AutoTrackingTable
            (DatabaseFront database, string name,
            double trackIntervalTime, IObservable<TRecord> itemAddedObservable,
            int version)
        {
            this.Table = new TypedTable<TRecord, TKey>(database, name)
            {
                IsIdAuto = false,
                Version = version,
            };


            this.Tracker = new Tracker<TRecord, TKey>(this.Table).AddTo(this.Disposables);

            itemAddedObservable
                .Buffer(itemAddedObservable.Throttle(TimeSpan.FromMilliseconds(trackIntervalTime)))
                .Subscribe(items =>
                {
                    Task.Run(async () =>
                    {
                        await this.Table.Parent.RequestThreadSafeTransactionAsync
                            (context => this.Table.AddRangeAsync(items, context.Connection, context.Transaction))
                            .ConfigureAwait(false);
                        items.ForEach(x => this.Tracker.Track(x));
                    });
                })
                .AddTo(this.Disposables);
        }

        public async Task<TRecord[]> GetAllAsTrackingAsync(IDbConnection connection)
        {
            var data = await this.Table.GetAllAsync(connection).ConfigureAwait(false);

            foreach(var item in data)
            {
                this.Tracker.Track(item);
            }

            return data;
        }

    }
}
