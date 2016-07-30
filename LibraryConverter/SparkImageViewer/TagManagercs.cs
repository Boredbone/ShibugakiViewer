using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkImageViewer.DataModel
{


    public class TagManager
    {
        private HashSet<int> Tags { get; }
        public bool IsEdited { get; set; }


        public TagManager(HashSet<int> collection)
        {
            this.Tags = collection;
        }


        public void Add(int item)
        {
            this.IsEdited = true;
            this.Tags.Add(item);
        }

        public void Remove(int item)
        {
            this.IsEdited = true;
            this.Tags.Remove(item);
        }

        public bool Contains(int item)
        {
            return this.Tags.Contains(item);
        }

        //public HashSet<int> Copy()
        //{
        //    return new HashSet<int>(this.Tags);
        //}

        public IEnumerable<int> Read()
        {
            foreach (var item in this.Tags)
            {
                yield return item;
            }
        }

        public int Count()
        {
            return this.Tags.Count;
        }
    }

}
