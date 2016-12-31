using System;
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

        public static IDatabaseExpression GetSkipFilterSql(IEnumerable<SortSetting> items)
            => GetFilterSql(items, CompareMode.Great, CompareMode.Less, CompareMode.Great);

        public static IDatabaseExpression GetOrderFilterSql(IEnumerable<SortSetting> items)
            => GetFilterSql(items, CompareMode.Less, CompareMode.Great, CompareMode.LessEqual);


        private static IDatabaseExpression GetFilterSql(IEnumerable<SortSetting> items,
            CompareMode ascSymbol, CompareMode descSymbol, CompareMode idSymbol)
        {

            if (items == null)
            {
                return null;
            }

            var prevOrders = new List<IDatabaseExpression>();

            var filters = items
                .SelectMany(x => x.Property.GetSortColumns()
                    .Select(s => new SortFilterContainer
                    { Column = s, Symbol = x.IsDescending ? descSymbol : ascSymbol }))
                .Append(new SortFilterContainer
                { Column = FileProperty.Id.GetSortColumns().First(), Symbol = idSymbol })
                .Select((x, c) =>
                {
                    var symbol = x.Symbol;
                    var reference = new DatabaseReference($"@C{c}");

                    var str = DatabaseExpression.Compare(x.Column, symbol, reference);
                    var fil = DatabaseExpression.And(prevOrders.Append(str).ToArray());
                    prevOrders.Add(DatabaseExpression.AreEqual(x.Column, reference));

                    return fil;
                })
                .ToArray();

            return DatabaseExpression.Or(filters.ToArray());
        }

        private class SortFilterContainer
        {
            public string Column { get; set; }
            public CompareMode Symbol { get; set; }
        }


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


        public override int GetHashCode()
        {
            return this.IsDescending.GetHashCode() ^ this.Property.GetHashCode();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
