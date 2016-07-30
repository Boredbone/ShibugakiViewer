using SparkImageViewer.DataModel;
using MikanViewer.PropertySearch;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SparkImageViewer.FileSort
{
    [DataContract]
    public class SortSetting : IEquatable<SortSetting>
    {
        [DataMember]
        public FileProperty Property { get; set; }
        [DataMember]
        public bool IsDescending { get; set; }

        //public SortMethod<IFileInformation> ToSortMethod()
        //{
        //    return this.Property.ToNewFileSort(this.IsDescending);
        //}

        public SortSetting Clone()
        {
            return new SortSetting()
            {
                Property = this.Property,
                IsDescending = this.IsDescending,
            };
        }

        public bool ValueEquals(SortSetting other)
        {
            return other != null && this.Property == other.Property
                && this.IsDescending == other.IsDescending;
        }

        /// <summary>
        /// Check whether two objects have same value
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SortSetting other)
        {
            return this.ValueEquals(other);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.ValueEquals((SortSetting)obj);
        }

        //Equalsがtrueを返すときに同じ値を返す
        public override int GetHashCode()
        {
            return this.IsDescending.GetHashCode() ^ this.Property.GetHashCode();
        }
    }
}
