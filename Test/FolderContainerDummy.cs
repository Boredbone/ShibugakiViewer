using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageLibrary.Creation;

namespace Test
{

    public class FolderContainerDummy : IFolderContainer
    {
        public string Path { get; set; }

        public Dictionary<string, FolderContainerDummy> Folders { get; set; }

        public List<ImageFileInformation> Files { get; } = new List<ImageFileInformation>();

#pragma warning disable 1998
        public async ValueTask<long> EnumerateFilesAsync(
            Action<int> OnFileEnumerated, 
            bool containsChildren = true, 
            CancellationToken cancellationToken = default(CancellationToken), bool configureAwait = false)
        {
            return this.GetAllFiles().Count();
        }
#pragma warning restore 1998

#pragma warning disable 1998
        /// <summary>
        /// 外部から渡されたファイルリストを使用
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="configureAwait"></param>
        /// <returns></returns>
        public async ValueTask<long> EnumerateFilesAsync
            (string[] path,
            CancellationToken cancellationToken = default(CancellationToken), bool configureAwait = false)
        {
            throw new NotImplementedException();
        }
#pragma warning restore 

        public Dictionary<string, FolderContainerDummy> GetChildFolders()
        {
            if (this.Folders == null)
            {
                return new Dictionary<string, FolderContainerDummy>();
            }
            return this.Folders.SelectMany(x => x.Value.GetChildFolders()).ToDictionary(x => x.Key, x => x.Value);
        }

        public IEnumerable<ImageFileInformation> GetAllFiles()
        {
            if (this.Folders == null)
            {
                return this.Files;
            }
            return this.Files.Concat(this.Folders.SelectMany(x => x.Value.GetAllFiles()));
        }

        public ImageFileInformation GetImage(string path, PropertiesLevel level)
        {
            return this.GetAllFiles().FirstOrDefault(x => x.Path.Equals(path));
        }

#pragma warning disable 1998
        public async Task<IEnumerable<T>> DoForAllFilesAsync<T>(Func<string, T> action,
            CancellationToken cancellationToken = default(CancellationToken),
            bool configureAwait = false)
        {
            var files = this.GetAllFiles().ToArray();
            return files.Select(x => action(x.Path)).ToArray();
        }
#pragma warning restore 1998
    }
}
