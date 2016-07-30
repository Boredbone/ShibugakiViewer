﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;

namespace ImageLibrary.Search
{

    [DataContract]
    public class SortSetting : IEquatable<SortSetting>, INotifyPropertyChanged
    {
        [DataMember]
        public FileProperty Property
        {
            get { return _fieldProperty; }
            set
            {
                if (_fieldProperty != value)
                {
                    _fieldProperty = value;
                    RaisePropertyChanged(nameof(Property));
                }
            }
        }
        private FileProperty _fieldProperty;
        
        [DataMember]
        public bool IsDescending
        {
            get { return _fieldIsDescending; }
            set
            {
                if (_fieldIsDescending != value)
                {
                    _fieldIsDescending = value;
                    RaisePropertyChanged(nameof(IsDescending));
                }
            }
        }
        private bool _fieldIsDescending;
        


        public static SortSetting ById { get; }
            = new SortSetting() { Property = FileProperty.Id, IsDescending = false };


        public static SortSetting FromText(string text)
        {
            if (text == null || text.Length <= 0)
            {
                return null;
            }
            var items = text.Split('-');
            if (items.Length < 2)
            {
                return null;
            }
            var property = FilePropertyManager.FromName(items[0]);
            var isDescending = items[1].Equals("D");
            return new SortSetting() { Property = property, IsDescending = isDescending };
        }

        private string ToSql()
            => this.Property.ToSort(this.IsDescending);


        public static string[] GetFullSql(IEnumerable<SortSetting> items)
        {
            if (items == null)
            {
                return new string[0];
            }
            return items.Select(x => x.ToSql()).ToArray();
        }

        public static string GetOrderFilterSql(IEnumerable<SortSetting> items, Record record)
        {
            //TODO アスペクト比，フルパスが含まれている場合も正常に動作するか？
            //TODO グループが含まれている，グループ内の時は？　

            if (items == null)
            {
                return null;
            }

            var prevOrders = new List<string>();

            var filters = items
                .SelectMany(x => x.Property.GetSortColumns()
                    .Select(s => new SortFilterContainer
                    { Column = s, Symbol = x.IsDescending ? ">" : "<" }))
                .Append(new SortFilterContainer
                { Column = FileProperty.Id.GetSortColumns().First(), Symbol = "<=" })
                .Select((x, c) =>
                {
                    var symbol = x.Symbol;//.IsDescending ? ">" : "<";
                    var reference = $"@C{c}";

                    var str = $"{x.Column} {symbol} {reference}";
                    var fil = DatabaseFunction.And(prevOrders.Append(str).ToArray());
                    prevOrders.Add(DatabaseFunction.AreEqual(x.Column, reference));

                    return fil;
                })
                .ToArray();

            /*
            foreach (var item in items)
            {
                var symbol = item.IsDescending ? ">" : "<";

                item.Property.AppendFilterReferences(record, symbol, filters, prevOrders);

            }

            FileProperty.Id.AppendFilterReferences(record, "<=", filters, prevOrders);*/
            
            return DatabaseFunction.Or(filters.ToArray());
        }

        private class SortFilterContainer
        {
            public string Column { get; set; }
            public string Symbol { get; set; }
        }

        //private void AppendFilter
        //    (string name, string reference, string symbol, List<string> filters, List<string> prevOrders)
        //{
        //    var str = $"{name} {symbol} {reference}";
        //    filters.Add(DatabaseFunction.And(prevOrders.Append(str).ToArray()));
        //    prevOrders.Add(DatabaseFunction.AreEqual(name, reference));
        //}

        public static string GetReferenceSelectorSql(IEnumerable<SortSetting> items)
        {
            if (items == null)
            {
                return null;
            }

            return items
                .SelectMany(x => x.Property.GetSortColumns())
                .Concat(FileProperty.Id.GetSortColumns())
                .Select((x, c) => $"{x} as C{c}")
                .Join(", ");

        }


        public string ToEntryString()
        {
            var direction = this.IsDescending ? "D" : "A";
            return $"{this.Property.GetName()}-{direction}";
        }



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


        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
