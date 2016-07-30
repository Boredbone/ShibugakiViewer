using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SparkImageViewer.DataModel
{

    [DataContract]
    public class SavedLibraryList
    {
        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public List<string> FileNames { get; set; }

        public SavedLibraryList()
        {
        }

        public SavedLibraryList(List<string> list)
        {
            this.FileNames = list;
        }
    }
}
