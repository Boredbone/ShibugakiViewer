using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;

namespace ImageLibrary.Creation
{
    /// <summary>
    /// 一つのフォルダとその中のファイルを管理
    /// </summary>
    public class FolderContainer : IFolderContainer
    {
        /// <summary>
        /// ファイル走査の同時実行数
        /// </summary>
        public int Concurrency { get; set; } = 64;

        /// <summary>
        /// フォルダパス
        /// </summary>
        public string Path { get; }

        private string[] FilesFullpath { get; set; }

        private readonly HashSet<string> fileTypeFilter;
        private readonly FileSystem accesser;


        public FolderContainer(string path,
            FileSystem accesser, HashSet<string> fileTypeFilter)
        {
            this.Path = path;
            this.accesser = accesser;
            this.fileTypeFilter = fileTypeFilter;
        }

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
            this.FilesFullpath = path;

            return this.FilesFullpath.LongLength;
        }
#pragma warning restore 

#pragma warning disable 1998
        /// <summary>
        /// フォルダ内ファイルを列挙
        /// </summary>
        /// <param name="containsChildren"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="configureAwait"></param>
        /// <returns></returns>
        public async ValueTask<long> EnumerateFilesAsync(
            Action<int> OnFileEnumerated, 
            bool containsChildren = true,
            CancellationToken cancellationToken = default(CancellationToken), 
            bool configureAwait = false)
        {
            this.FilesFullpath = System.IO.Directory.EnumerateFiles
                (this.Path, "*.*",
                containsChildren ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly)
                .AsParallel()
                .Where(x => this.fileTypeFilter.Contains(System.IO.Path.GetExtension(x).ToLower()))
                .ToArray();

            OnFileEnumerated?.Invoke(this.FilesFullpath.Length);

            return this.FilesFullpath.LongLength;
        }

#pragma warning restore 1998

        /// <summary>
        /// 列挙された各ファイルに対し処理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="configureAwait"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> DoForAllFilesAsync<T>(Func<string, T> action,
            CancellationToken cancellationToken = default(CancellationToken), bool configureAwait = false)
        {
            return (await this.FilesFullpath
                .SelectAsync(path =>
                    Task.Run(() => action(path)), this.Concurrency, default(CancellationToken), false)).ToArray();
        }

        /// <summary>
        /// 指定パスの画像ファイル情報を取得
        /// </summary>
        /// <param name="path"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public ImageFileInformation GetImage(string path, PropertiesLevel level)
        {
            return this.accesser.GetImage(path, level);
        }
        
    }
}
