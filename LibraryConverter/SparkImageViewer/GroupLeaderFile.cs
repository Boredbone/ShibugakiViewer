using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using SparkImageViewer.DataModel;
using SparkImageViewer.FileSort;

namespace SparkImageViewer.DataModel
{

    [DataContract]
    public class GroupLeaderFile : IFileInformation
    {
        [DataMember]
        public string LeaderFilekey { get; set; }
        [DataMember]
        public List<SortSetting> SortSettings { get; set; }
        [DataMember]
        public HashSet<int> Tags { get; set; }
        [DataMember]
        public int Rating { get; set; }
        [DataMember]
        public string displayName;
        [DataMember]
        public bool IsParticularFlipDirectionEnabled { get; set; }
        [DataMember]
        public bool IsFlipReversed { get; set; }


        public string RelativePath
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string RootDirectoryAccessKey
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public List<string> Path
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DateTimeOffset DateModified
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DateTimeOffset DateCreated
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DateTimeOffset DateRegistered
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public uint Width
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public uint Height
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ulong Size
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public string UniqueKey { get; set; }

        private HashSet<IFileInformation> _groupMember;
        private HashSet<IFileInformation> GroupMember
        {
            get
            {
                if (this._groupMember == null)
                {
                    this._groupMember = new HashSet<IFileInformation>();
                }
                return this._groupMember;
            }
            set
            {
                this._groupMember = value;
            }
        }
        public int ChildrenCount { get { return this.GroupMember.Count; } }

        public IFileInformation LeaderFile { get; set; }

        public GroupLeaderFile(string uniqueKey, IFileInformation cover)
        {
            this.GroupMember = new HashSet<IFileInformation>();
            this.Tags = new HashSet<int>();
            this.UniqueKey = uniqueKey;
            SetThumbnail(cover);
        }

        public GroupLeaderFile()
        {
        }

        public void SetThumbnail(IFileInformation file)
        {
            this.LeaderFile = file;
        }
    }
}
