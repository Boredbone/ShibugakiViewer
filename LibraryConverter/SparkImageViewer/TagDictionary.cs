using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Boredbone.Utility.Extensions;
using SparkImageViewer.DataModel;

namespace SparkImageViewer.DataModel
{
    public class TagDictionary
    {
        private ConcurrentDictionary<int, TagInformation> RegisteredTags { get; set; }
        public bool IsEdited { get; set; }



        public TagDictionary()
        {
            this.RegisteredTags = new ConcurrentDictionary<int, TagInformation>();
            this.IsEdited = false;
        }

        public void SetSource(IEnumerable<KeyValuePair<int, TagInformation>> source)
        {
            if (source == null)
            {
                this.RegisteredTags = new ConcurrentDictionary<int, TagInformation>();
            }
            else
            {
                this.RegisteredTags = new ConcurrentDictionary<int, TagInformation>(source);
            }
        }


        public IEnumerable<KeyValuePair<int,TagInformation>> GetAll()
        {
            return this.RegisteredTags.OrderBy(x => x.Value.Name);
        }


        public TagInformation GetTagValue(int key)
        {
            TagInformation result;
            if (this.RegisteredTags.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                var tag = new TagInformation() { Name = key.ToString(), Shortcut = "" };
                this.RegisteredTags.TryAdd(key, tag);
                this.IsEdited = true;
                return tag;
            }
        }
    
        public KeyValuePair<int, TagInformation> GetTag(string shortcut)
        {
            var result = this.RegisteredTags
                .FirstOrNull(x => x.Value.Shortcut.Equals(shortcut, StringComparison.OrdinalIgnoreCase));

            if (result != null)//.Count > 0)
            {
                return result.Value;//.First();
            }
            return new KeyValuePair<int, TagInformation>(-1, null);
        }

        public int SetTag(TagInformation newTag)
        {
            var existingTag = this.RegisteredTags
                .FirstOrNull(x => x.Value.Name.Equals(newTag.Name));

            if (existingTag != null)//.Count > 0)
            {
                return existingTag.Value.Key;//.First().Key;
            }


            this.RegisteredTags
                .Where(x =>
                {
                    return x.Value.Shortcut != null
                        && x.Value.Shortcut.Length > 0
                        && x.Value.Shortcut.Equals(newTag.Shortcut,
                        StringComparison.OrdinalIgnoreCase);
                })
                .ForEach(x => x.Value.Shortcut = "_" + x.Value.Shortcut);

            var newKey = this.RegisteredTags.Count + 1;

            while (this.RegisteredTags.ContainsKey(newKey))
            {
                newKey++;
            }


            //this.RegisteredTags.Add(newKey, newTag);
            var result = this.RegisteredTags.TryAdd(newKey, newTag);

            //Task.Run(async () => await SaveSettingsAsync());
            //this.SaveSettingsAsync().FireAndForget();
            this.IsEdited = true;

            return newKey;
        }

        public bool ContainsTagKey(int key)
        {
            return this.RegisteredTags.ContainsKey(key);
        }


        public void ClearTags()
        {
            this.RegisteredTags.Clear();
        }

    }
}
