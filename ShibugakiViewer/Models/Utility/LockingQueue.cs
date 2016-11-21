using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShibugakiViewer.Models.Utility
{

    public class LockingQueue<T>
    {
        private object gate = new object();

        private Queue<T> queue;

        public LockingQueue()
        {
            this.queue = new Queue<T>();
        }

        public void Enqueue(T item)
        {
            lock (this.gate)
            {
                this.queue.Enqueue(item);
            }
        }

        public bool TryDequeue(out T result)
        {
            lock (this.gate)
            {
                var count = this.queue.Count;

                if (count <= 0)
                {
                    result = default(T);
                    return false;
                }

                result = this.queue.Dequeue();
                return true;
            }
        }

        public bool TryDequeue(int capacity, out T result)
        {
            lock (this.gate)
            {
                var count = this.queue.Count;

                if (count <= 0)
                {
                    result = default(T);
                    return false;
                }


                while (count > capacity)
                {
                    this.queue.Dequeue();
                    count--;
                }
                result = this.queue.Dequeue();
                return true;
            }
        }

        public void Clear()
        {
            lock (this.gate)
            {
                this.queue.Clear();
            }
        }
    }
}
