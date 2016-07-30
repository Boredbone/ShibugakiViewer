
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ImageLibrary.Search
{
    public interface ISqlSearch : INotifyPropertyChanged
    {
        string ToSql();
        bool IsUnit { get; }
        ISqlSearch Clone();
        bool ValueEquals(ISqlSearch other);
        INotifyCollectionChanged Children { get; }
        ComplexSearch Parent { get; set; }
        void RemoveSelf();
    }
}
