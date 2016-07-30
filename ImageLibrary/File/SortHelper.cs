using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using ImageLibrary.Search;

namespace ImageLibrary.File
{
    public static class SortHelper
    {
        public static string ToEntry(IEnumerable<SortSetting> source)
        {
            return source?.Select(x => x.ToEntryString()).Join(",");
        }
        public static List<SortSetting> FromEntry(string code)
        {

            if (code == null || code.Length <= 0)
            {
                return new List<SortSetting>();
            }
            else
            {
                return code.Split(',')
                    .Select(x => SortSetting.FromText(x))
                    .Where(x => x != null)
                    .ToList();
            }
        }
    }

}
