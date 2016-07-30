using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Creation;
using ImageLibrary.File;

namespace ImageLibrary.Core
{

    public interface ILibraryConfiguration
    {
        string SaveDirectory { get; }
        HashSet<string> FileTypeFilter { get; }

        Task<bool> IsFileExistsAsync(Record file);
        Task<bool> IsFolderExistsAsync(FolderInformation folder);
        Task<bool> IsFolderExistsAsync(string path);
        bool IsKnownFolderEnabled { get; }
        IFolderContainer GetFolderContainer(string path);
        IEnumerable<string> GetChildFolders(string path);
    }

    /// <summary>
    /// ライブラリ設定
    /// </summary>
    public class LibraryConfiguration : ILibraryConfiguration
    {
        public string SaveDirectory { get; set; }
        public bool IsKnownFolderEnabled => true;
        public FileSystem FileSystem { get; set; }
        public int Concurrency { get; set; }
        public HashSet<string> FileTypeFilter { get; set; }


        public LibraryConfiguration(string saveDirectory)
        {
            this.SaveDirectory = saveDirectory;
        }


        public IFolderContainer GetFolderContainer(string path)
        {
            return new FolderContainer(path, this.FileSystem, this.FileTypeFilter)
            {
                Concurrency = this.Concurrency,
            };
        }


        public IEnumerable<string> GetChildFolders(string path)
        {
            return Directory.EnumerateDirectories(path);
        }
#pragma warning disable 1998
        public async Task<bool> IsFileExistsAsync(Record file)
        {
            if (file.IsGroup)
            {
                throw new ArgumentException();
            }

            return System.IO.File.Exists(file.FullPath);
        }
#pragma warning restore 1998

        public async Task<bool> IsFolderExistsAsync(FolderInformation folder)
        {
            return await this.IsFolderExistsAsync(folder.Path);
        }

#pragma warning disable 1998
        public async Task<bool> IsFolderExistsAsync(string path)
        {
            return System.IO.Directory.Exists(path);
        }
#pragma warning restore 1998
    }


    /// <summary>
    /// テスト用のライブラリ設定
    /// </summary>
    public class LibraryConfigurationDummy : ILibraryConfiguration
    {
        public string SaveDirectory { get; set; }
        public bool IsKnownFolderEnabled => false;
        public Func<string, IFolderContainer> GetFolderFunction { get; set; }
        public Func<string, IEnumerable<string>> GetChildFoldersFunction { get; set; }
        public HashSet<string> FileTypeFilter { get; } = new HashSet<string>();


        public LibraryConfigurationDummy(string saveDirectory)
        {
            this.SaveDirectory = saveDirectory;
        }

        public IFolderContainer GetFolderContainer(string path)
        {
            return this.GetFolderFunction(path);
        }

        public IEnumerable<string> GetChildFolders(string path)
        {
            return this.GetChildFoldersFunction(path);
        }

#pragma warning disable 1998
        public async Task<bool> IsFileExistsAsync(Record file)
        {
            if (file.IsGroup)
            {
                throw new ArgumentException();
            }

            return false;
        }
#pragma warning restore 1998

        public async Task<bool> IsFolderExistsAsync(FolderInformation folder)
        {
            return await this.IsFolderExistsAsync(folder.Path);
        }

#pragma warning disable 1998
        public async Task<bool> IsFolderExistsAsync(string path)
        {
            return this.GetChildFoldersFunction(path) != null;
        }
#pragma warning restore 1998
    }


}
