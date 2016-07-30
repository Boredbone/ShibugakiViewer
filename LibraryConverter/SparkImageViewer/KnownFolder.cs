using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SparkImageViewer.DataModel
{
    /*
    public interface IKnownFolder
    {
        StorageFolder GetFolder();
    }


    public class PicturesLibrary : IKnownFolder
    {

        public StorageFolder GetFolder()
        {
            return KnownFolders.PicturesLibrary;
        }
    }

    public class KnownFoldersManager
    {

        private Dictionary<string, IKnownFolder> folders
            = new Dictionary<string, IKnownFolder>()
            {
                {"PicturesLibrary-d8267550-af90-4b46-a0f5-0f666dc1259d",new PicturesLibrary()},
            };

        public IEnumerable<KeyValuePair<string, IKnownFolder>> GetFolders()
        {
            return this.folders.ToArray();
        }

        public bool TryGetValue(string key, out IKnownFolder value)
        {
            return this.folders.TryGetValue(key, out value);
        }

        public bool ContainsKey(string key)
        {
            return this.folders.ContainsKey(key);
        }
    }*/
}
