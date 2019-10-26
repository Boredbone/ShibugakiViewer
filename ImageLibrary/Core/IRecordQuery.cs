using System.Threading.Tasks;
using Database.Search;
using Database.Table;
using ImageLibrary.File;

namespace ImageLibrary.Core
{
    public interface IRecordQuery<T> where T : ISearchCriteria
    {
        Library Library { get; }
        TypedTable<Record, string> Table { get; }

        Task<long> CountAsync(T criteria);
        IDatabaseExpression GetFilterString(T criteria);
        Task<Record[]> SearchAsync(T criteria, long skip, long take, Record skipUntil);
        long Count(T criteria);
        Record[] Search(T criteria, long skip, long take, Record skipUntil);
    }
}