using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.File;

namespace ImageLibrary.Core
{
    public class LibraryLoadResult
    {
        public Dictionary<string, Record> AddedFiles { get; set; }
        public Dictionary<string, Record> RemovedFiles { get; set; }
        public Dictionary<string, Record> UpdatedFiles { get; set; }
        public LibraryLoadAction Action { get; set; }
        public DateTimeOffset DateTime { get; set; }
    }
    public enum LibraryLoadAction
    {
        UserOperation,
        Startup,
        Activation,
        FolderChanged,
    }
}
