using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Database.Table
{
    /// <summary>
    /// http://neue.cc/2013/03/09_400.html
    /// </summary>
    public static class DisposableThreadLocal
    {
        public static DisposableThreadLocal<T> Create<T>(Func<T> valueFactory)
            where T : IDisposable
        {
            return new DisposableThreadLocal<T>(valueFactory);
        }
    }

    public class DisposableThreadLocal<T> : ThreadLocal<T>
        where T : IDisposable
    {
        public DisposableThreadLocal(Func<T> valueFactory)
            : base(valueFactory, trackAllValues: true)
        {

        }

        protected override void Dispose(bool disposing)
        {
            var exceptions = new List<Exception>();
            foreach (var item in this.Values.OfType<IDisposable>())
            {
                try
                {
                    item.Dispose();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            base.Dispose(disposing);

            if (exceptions.Any()) throw new AggregateException(exceptions);
        }
    }
}
