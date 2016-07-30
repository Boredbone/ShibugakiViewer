using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkImageViewer.DataModel
{
    

    public interface IFileInformation
    {
        string RelativePath { get; }
        string RootDirectoryAccessKey { get; }
        List<string> Path { get; }
        DateTimeOffset DateModified { get; }
        DateTimeOffset DateCreated { get; }
        DateTimeOffset DateRegistered { get; }
        uint Width { get; }
        uint Height { get; }
        ulong Size { get; }
        int Rating { get; set; }
    }
}
