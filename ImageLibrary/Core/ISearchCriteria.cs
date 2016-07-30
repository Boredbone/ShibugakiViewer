using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.File;
using ImageLibrary.Search;

namespace ImageLibrary.Core
{

    public interface ISearchCriteria
    {
        string GetFilterString(Library library);

        Task<long> CountAsync(Library library);
        Task<Record[]> SearchAsync(Library library, long skip, long take);
        //Task<long> FindIndexAsync(Library library, Record record);
        //Task<string[]> GetAllIdsAsync(Library library);

        string SortEntry { get; }
        bool SetSort(IEnumerable<SortSetting> source);
        IEnumerable<SortSetting> GetSort();
    }
}
