using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Database.Search;

namespace ImageLibrary.Search
{
    public interface ISqlSearch : INotifyPropertyChanged
    {
        ComplexSearch Parent { get; set; }
        INotifyCollectionChanged Children { get; }
        bool IsUnit { get; }
        bool IsEdited { get; }
        void DownEdited();
        ISqlSearch Clone();
        IDatabaseExpression ToSql();
        bool ValueEquals(ISqlSearch other);
        void RemoveSelf();
    }
}
