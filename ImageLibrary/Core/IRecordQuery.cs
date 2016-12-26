using System.Threading.Tasks;
using Database.Table;
using ImageLibrary.File;
using ImageLibrary.Search;

namespace ImageLibrary.Core
{
    public interface IRecordQuery<T> where T : ISearchCriteria
    {
        Library Library { get; }
        TypedTable<Record, string> Table { get; }

        Task<long> CountAsync(T criteria);
        string GetFilterString(T criteria);
        Task<Record[]> SearchAsync(T criteria, long skip, long take, Record skipUntil);
    }
}