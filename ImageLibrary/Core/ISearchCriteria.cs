using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database.Search;
using ImageLibrary.File;
using ImageLibrary.Search;

namespace ImageLibrary.Core
{
    /// <summary>
    /// 検索条件
    /// </summary>
    public interface ISearchCriteria
    {
        IDatabaseExpression GetFilterString(Library library);

        Task<long> CountAsync(Library library);
        Task<Record[]> SearchAsync(Library library, long skip, long take, Record skipUntil);

        string SortEntry { get; }
        bool SetSort(IEnumerable<SortSetting> source, bool replaceDefaultSort);
        IEnumerable<SortSetting> GetSort();
    }
}
