using MikanViewer.PropertySearch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SparkImageViewer.FileSearch;

namespace SparkImageViewer.DataModel
{
    
    [DataContract]
    public class UnitSearchSetting : ISearchSetting
    {
        
        [DataMember(Name = "Property")]
        public FileProperty Property { get; set; }
        
        [DataMember(Name = "CompareModeSetting")]
        public CompareMode Mode { get; set; }
        
        [DataMember(Name = "IsNot")]
        public bool IsNot { get; set; }
        
        
        [DataMember]
        public object SingleReference { get; set; }

        [DataMember]
        public List<string> StringListReference { get; set; }

        public bool IsUnit => true;

        public UnitSearchSetting()
        {
        }

        public ISearchSetting Clone()
        {
            return new UnitSearchSetting()
            {
                IsNot = this.IsNot,
                Mode = this.Mode,
                Property = this.Property,
                SingleReference = this.SingleReference,
                StringListReference = this.StringListReference,
            };
        }

        public bool ValueEquals(ISearchSetting other)
        {
            throw new NotImplementedException();
        }
        
    }

    [DataContract]
    public class ComplexSearchSetting : ISearchSetting
    {
        [DataMember]
        public List<ISearchSetting> SavedChildren { get; set; }
        
        [DataMember(Name = "IsOr")]
        public bool IsOr { get; set; }

        public bool IsUnit => false;

        public ComplexSearchSetting()
        {
        }

        public ISearchSetting Clone()
        {
            return new ComplexSearchSetting()
            {
                IsOr = this.IsOr,
                SavedChildren = new List<ISearchSetting>(this.SavedChildren.Select(x => x.Clone())),
            };
        }

        public bool ValueEquals(ISearchSetting other)
        {
            throw new NotImplementedException();
        }
        
    }
}
