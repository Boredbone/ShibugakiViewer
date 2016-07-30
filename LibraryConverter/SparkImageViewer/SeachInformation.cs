using SparkImageViewer.DataModel;
using System;
using System.Collections.Generic;
using System.Text;
using Boredbone.Utility.Extensions;
using System.Runtime.Serialization;
using System.Linq;
using SparkImageViewer.FileSort;

namespace SparkImageViewer.FileSearch
{
    [DataContract]
    public class SearchInformation
    {
        [DataMember]
        public ComplexSearchSetting Root { get; set; }

        [DataMember]
        public DateTimeOffset DateLastUsed { get; set; }

        [DataMember]
        public List<SortSetting> SortSettings { get; private set; }

        [DataMember]
        public string Name { get; set; }


        [DataMember]
        public string ThumbnailFilekey { get; set; }
        


        public string Key { get; set; }
        

        public SearchInformation(ComplexSearchSetting root)
        {
            //this.settings = ApplicationCoreData.Instance;
            this.Root = root;
            this.SortSettings = new List<SortSetting>();
        }

        //public bool SetSort(IEnumerable<SortSetting> source)
        //{
        //    if (SortSettings == null
        //        || !SortSettings.SequenceEqual(source, (x, y) => x.Equals(y)))
        //    {
        //        this.SortSettings = new List<SortSetting>(source);
        //        ApplicationCoreData.Instance.Searcher.SetDefaultSort(source);
        //        return true;
        //    }
        //    return false;
        //}

        public SearchInformation Clone()
        {
            return new SearchInformation((ComplexSearchSetting)this.Root.Clone())
            {
                DateLastUsed = this.DateLastUsed,
                SortSettings = new List<SortSetting>(this.SortSettings.Select(x => x.Clone())),
                ThumbnailFilekey = this.ThumbnailFilekey,
                Key = this.Key,
                Name = this.Name,
            };
        }

        //public bool HasSameSearch(SearchInformation other)
        //{
        //    return this.Root.ValueEquals(other.Root);
        //}

        //public bool HasSameSort(SearchInformation other)
        //{
        //    return this.SortSettings.SequenceEqual(other.SortSettings, (x, y) => x.ValueEquals(y));
        //}

        //public bool SettingEquals(SearchInformation other)
        //{
        //    return this.HasSameSearch(other) && this.HasSameSort(other);
        //}

        public void SetDateToNow()
        {
            this.DateLastUsed = DateTimeOffset.Now;
        }
    }
}
