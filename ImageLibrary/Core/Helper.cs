using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Table;

namespace ImageLibrary.Core
{
    class Helper
    {
        public static IDisposable TrackAdded<T, TKey>
            (IObservable<T> observable, double time,
            ITypedTable<T> table, Tracker<T, TKey> tracker)
            where T : INotifyPropertyChanged, ITrackable, IRecord<TKey>
        {
            return observable
                .Buffer(observable.Throttle(TimeSpan.FromMilliseconds(time)))
                .Subscribe(items =>
                {
                    Task.Run(async () =>
                    {
                        await table.Parent.RequestThreadSafeTransactionAsync
                            (context => table.AddRangeAsync(items, context.Connection, context.Transaction));
                        items.ForEach(x => tracker.Track(x));
                    });
                });
        }
    }
}
