using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SparkImageViewer.DataModel;

namespace SparkImageViewer.FileSearch
{
    public interface ISearchSetting
    {
        bool IsUnit { get; }
        ISearchSetting Clone();
        bool ValueEquals(ISearchSetting other);
    }

}
