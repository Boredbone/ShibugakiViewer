using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;
using Boredbone.Utility;
using System.Collections.Concurrent;

namespace SparkImageViewer.DataModel
{
    [DataContract]
    public class FileInformation : IFileInformation
    {
        [DataMember]
        public string RelativePath { get; set; }
        [DataMember]
        public string RootDirectoryAccessKey { get; set; }
        [DataMember]
        public List<string> Path { get; set; }
        [DataMember]
        public DateTimeOffset DateModified { get; set; }
        [DataMember]
        public DateTimeOffset DateCreated { get; set; }
        [DataMember]
        public DateTimeOffset DateRegistered { get; set; }
        [DataMember]
        public uint Width { get; set; }
        [DataMember]
        public uint Height { get; set; }
        [DataMember]
        public ulong Size { get; set; }
        [DataMember]
        public HashSet<int> Tags { get; set; }
        [DataMember]
        public int Rating { get; set; }
        [DataMember]
        public string GroupLeaderKey { get; set; }
        
        public GroupLeaderFile GroupLeader { get; set; }
        public string UniqueKey { get; set; }

        public FileInformation()
        {
        }


    }

    [DataContract]
    public class FolderInformation// : INotifyPropertyChanged
    {
        [DataMember]
        public string FolderName { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public string KnownFolderKey { get; set; }
        [DataMember]
        public string DisplayName { get; set; }
        [DataMember]
        public bool AutoRefreshEnable { get; set; }

        //private bool _fieldRefreshEnable;
        //public bool RefreshEnable
        //{
        //    get { return _fieldRefreshEnable; }
        //    set
        //    {
        //        if (_fieldRefreshEnable != value)
        //        {
        //            _fieldRefreshEnable = value;
        //            //RaisePropertyChanged("RefreshEnable");
        //        }
        //    }
        //}
        public bool RefreshEnable { get; set; }
        public bool RefreshTrigger { get; set; }


        [DataMember]
        public ThreeState RefreshMode { get; set; }

        [DataMember]
        public bool Ignored { get; set; }

        //public bool RefreshEnable { get; set; }


        public FolderInformation(string accessToken)
            : this("", accessToken)
        {

        }

        public FolderInformation(string name, string accessToken)
        {
            this.FolderName = name;
            this.AccessToken = accessToken;
            this.Ignored = false;

            this.AutoRefreshEnable = true;
            this.RefreshMode = ThreeState.None;
        }


        //public async Task<StorageFolder> GetStorageFolderAsync()
        //{
        //    if (this.KnownFolderKey != null)
        //    {
        //        IKnownFolder knownFolder;
        //        if (ApplicationCoreData.Instance.KnownFoldersManager.TryGetValue(this.KnownFolderKey, out knownFolder))
        //        {
        //            return knownFolder.GetFolder();
        //        }
        //    }
        //
        //    if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(this.AccessToken))
        //    {
        //        return null;
        //    }
        //
        //    return await StorageApplicationPermissions
        //        .FutureAccessList.GetFolderAsync(this.AccessToken);
        //}


        public bool HasSameToken(string token)
        {
            return this.AccessToken.Equals(token);
        }

        public void SetToken(string token)
        {
            this.AccessToken = token;
        }

        //public bool IsKnownFolder()
        //{
        //    return this.KnownFolderKey != null
        //        && ApplicationCoreData.Instance.KnownFoldersManager.ContainsKey(this.KnownFolderKey);
        //}

        //public event PropertyChangedEventHandler PropertyChanged;
        //
        //protected void RaisePropertyChanged(string propertyName)
        //{
        //    var d = PropertyChanged;
        //    if (d != null)
        //        d(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}
