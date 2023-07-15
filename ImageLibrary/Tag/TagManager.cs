using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using ImageLibrary.Core;

namespace ImageLibrary.Tag
{
    [DataContract]
    public class TagManager : INotifyCollectionChanged, IEnumerable<TagInformation>
    {
        [DataMember]
        private HashSet<int> Tags { get; }
        public bool IsEdited { get; set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private Library library;


        public TagManager(HashSet<int> collection)
        {
            this.Tags = collection;
            this.library = LibraryOwner.GetCurrent();
        }



        public TagManager(string entry)
        {
            this.Tags = this.DecodeEntry(entry);
            this.library = LibraryOwner.GetCurrent();
        }

        public void Add(TagInformation tag)
        {
            tag.LastUsed = DateTimeOffset.Now;
            tag.IsIgnored = false;
            this.Add(tag.Id);
        }
        public void TryAdd(TagInformation tag)
        {
            if (!this.Tags.Contains(tag.Id))
            {
                this.Add(tag);
            }
        }

        public void Add(int item)
        {
            if (this.Tags.Contains(item))
            {
                return;
            }

            this.IsEdited = true;
            this.Tags.Add(item);
            this.CollectionChanged?.Invoke
                (this, new NotifyCollectionChangedEventArgs
                (NotifyCollectionChangedAction.Reset));
        }
        public void Edit(IEnumerable<int>? addedTags, IEnumerable<int>? removedTags)
        {
            if (addedTags is not null)
            {
                foreach (var t in addedTags)
                {
                    if (!this.Tags.Contains(t))
                    {
                        this.Tags.Add(t);
                    }
                }
            }
            if (removedTags is not null)
            {
                foreach (var t in removedTags)
                {
                    if (this.Tags.Contains(t))
                    {
                        this.Tags.Remove(t);
                    }
                }
            }
            this.IsEdited = true;
            this.CollectionChanged?.Invoke
                (this, new NotifyCollectionChangedEventArgs
                (NotifyCollectionChangedAction.Reset));
        }

        public void Remove(TagInformation tag)
        {
            this.Remove(tag.Id);
        }
        public void Remove(int item)
        {
            if (!this.Tags.Contains(item))
            {
                return;
            }

            this.IsEdited = true;
            this.Tags.Remove(item);
            this.CollectionChanged?.Invoke
                (this, new NotifyCollectionChangedEventArgs
                (NotifyCollectionChangedAction.Reset));
        }

        public void Toggle(TagInformation tag)
        {
            if (this.Tags.Contains(tag.Id))
            {
                this.Remove(tag.Id);
            }
            else
            {
                this.Add(tag);
            }
        }

        public bool Contains(int item)
        {
            return this.Tags.Contains(item);
        }


        public IEnumerable<int> Read()
        {
            foreach (var item in this.Tags)
            {
                yield return item;
            }
        }
        public List<int> ReadAll()
        {
            return this.Tags.ToList();
        }
        public List<int> ReadAllSorted()
        {
            return this.Tags.OrderBy(x => library.Tags.GetTagValue(x).Name).ToList();
        }


        private HashSet<int> DecodeEntry(string entry)
        {
            if (entry.IsNullOrWhiteSpace())
            {
                return new HashSet<int>();
            }

            var set = new HashSet<int>();

            foreach (var str in entry.Split(',').Where(s => s.Length > 0))
            {
                int num;
                if (int.TryParse(str, out num))
                {
                    set.Add(num);
                }
            }
            return set;
        }

        public string ToEntry()
        {
            return $",{this.Tags.Select(x => x.ToString()).Join(",")},";
        }


        public int Count()
        {
            return this.Tags.Count;
        }

        public IEnumerator<TagInformation> GetEnumerator()
            => this.Tags.Select(x => library.Tags.GetTagValue(x)).OrderBy(x => x.Name).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<TagInformation>)this).GetEnumerator();
    }
}
