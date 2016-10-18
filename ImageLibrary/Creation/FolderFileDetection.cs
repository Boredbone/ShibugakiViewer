using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.Tag;

namespace ImageLibrary.Creation
{

    public class FolderFileDetection
    {
        private string Path { get; set; }
        private string TopPath { get; set; }
        public ILibraryConfiguration Config { get; set; }

        public ConcurrentDictionary<string, Record> AddedFiles { get; }
        public Dictionary<string, Record> RemovedFiles { get; private set; }
        private ConcurrentBag<string> DetectedFiles { get; }
        public ConcurrentDictionary<string, Record> UpdatedFiles { get; }
        public ConcurrentBag<string> SkippedFiles { get; }

        public event Action<string> ChildFolderLoaded;
        public event Action<int> FileEnumerated;
        public event Action<int> FileLoaded;



        public FolderFileDetection(ILibraryConfiguration config)
        {
            this.Config = config;

            this.AddedFiles = new ConcurrentDictionary<string, Record>();
            this.DetectedFiles = new ConcurrentBag<string>();
            this.UpdatedFiles = new ConcurrentDictionary<string, Record>();
            this.SkippedFiles = new ConcurrentBag<string>();
        }

        /// <summary>
        /// フォルダ内のファイルを列挙
        /// </summary>
        /// <param name="information"></param>
        /// <param name="relatedFiles"></param>
        /// <param name="completely"></param>
        /// <param name="level"></param>
        /// <param name="prospectedTags"></param>
        /// <returns></returns>
        public async Task<bool> ListupFilesAsync(FolderInformation information,
            IReadOnlyDictionary<string, Record> relatedFiles,
            bool completely, PropertiesLevel level,
            ConcurrentDictionary<string, ConcurrentBag<TagManager>> prospectedTags)
        {
            this.Path = PathUtility.WithPostSeparator(information.Path);

            this.TopPath = this.Path;

            var exists = await this.Config.IsFolderExistsAsync(information);

            //フォルダ取得に失敗
            if (!exists)
            {
                this.RemovedFiles = relatedFiles.ToDictionary(x => x.Key, x => x.Value);

                information.RefreshEnable = false;

                return false;
            }


            //ファイル検索オプション
            var options = new LoadingOptions()
            {
                LightMode = (information.Mode == FolderCheckMode.Light) ? true
                    : (information.Mode == FolderCheckMode.Detail) ? false
                    : !completely,

                ContainsChildren = !information.IsTopDirectoryOnly,
                Level = level,//PropertiesLevel.Basic,
            };
            

            this.ChildFolderLoaded?.Invoke(this.Path);
            this.FileEnumerated?.Invoke(0);
            this.FileLoaded?.Invoke(0);

            var folder = this.Config.GetFolderContainer(this.Path);

            var count = await folder.EnumerateFilesAsync
                (x => this.FileEnumerated?.Invoke(x),
                options.ContainsChildren, default(CancellationToken), false);
            

            await this.DoForAllFilesAsync(folder, options, relatedFiles, prospectedTags);

            this.FileLoaded?.Invoke((int)count);
            

            //旧ライブラリに存在するのに検査して見つからなかったファイルを
            //削除された(可能性のある)ファイルとしてリストアップしておく

            var detected = new HashSet<string>(this.DetectedFiles);

            this.RemovedFiles = relatedFiles
                .AsParallel()
                .Where(x => !detected.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
            

            information.RefreshEnable = false;
            return true;
        }

        

        /// <summary>
        /// 指定されたファイルについて検査
        /// </summary>
        /// <param name="files"></param>
        /// <param name="relatedFiles"></param>
        /// <param name="prospectedTags"></param>
        /// <returns></returns>
        public async Task<bool> CheckFolderUpdateAsync(
            string[] files, PropertiesLevel level,
            IReadOnlyDictionary<string, Record> relatedFiles,
            ConcurrentDictionary<string, ConcurrentBag<TagManager>> prospectedTags)
        {
            var folder = this.Config.GetFolderContainer(this.Path);

            var count = await folder.EnumerateFilesAsync
                (files, default(CancellationToken), false);

            var options = new LoadingOptions()
            {
                LightMode = true,
                ContainsChildren = false,
                Level = level,// PropertiesLevel.Basic,
            };


            this.FileEnumerated?.Invoke(0);
            this.FileLoaded?.Invoke(0);

            await this.DoForAllFilesAsync(folder, options, relatedFiles, prospectedTags);

            var detected = new HashSet<string>(this.DetectedFiles);

            this.RemovedFiles = relatedFiles
                .AsParallel()
                .Where(x => !detected.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            return true;
        }

        /// <summary>
        /// フォルダ内の全ファイルに対して処理
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="options"></param>
        /// <param name="library"></param>
        /// <param name="prospectedTags"></param>
        /// <returns></returns>
        private Task<IEnumerable<bool>> DoForAllFilesAsync(
            IFolderContainer folder,LoadingOptions options,
            IReadOnlyDictionary<string, Record> library,
            ConcurrentDictionary<string, ConcurrentBag<TagManager>> prospectedTags)
        {
            var loadCount = 0;
            return folder.DoForAllFilesAsync(path =>
            {
                if (options.LightMode && library.ContainsKey(path))
                {
                    this.DetectedFiles.Add(path);
                }
                else
                {
                    this.CheckImage(folder.GetImage(path, options.Level), library, prospectedTags);
                }
                this.FileLoaded?.Invoke(++loadCount);
                return true;
            });
        }

        /// <summary>
        /// 画像ファイルの情報を読み込み、新規or変更されたレコードに追加
        /// </summary>
        /// <param name="file"></param>
        /// <param name="library"></param>
        /// <param name="prospectedTags"></param>
        private void CheckImage(ImageFileInformation file,
            IReadOnlyDictionary<string, Record> library,
            ConcurrentDictionary<string, ConcurrentBag<TagManager>> prospectedTags)
        {
            if (file == null || file.IsNotFound)
            {
                return;
            }
            
            var newItemId = file.Path;

            this.DetectedFiles.Add(newItemId);

            Record existingItem;
            var itemExists = library.TryGetValue(newItemId, out existingItem);



            if (!itemExists
                || existingItem.DateModified < file.DateModified
                || existingItem.Size != file.Size)
            {

                var newItem = new Record(file.Path)
                {
                    DateModified = file.DateModified,
                    DateCreated = file.DateCreated,
                    Height = file.Height,
                    Rating = file.Rating,
                    Width = file.Width,
                    Size = file.Size,
                };

                

                if (!itemExists || existingItem == null)
                {
                    //新規
                    this.AddedFiles[newItemId] = newItem;
                }
                else
                {
                    newItem.CopyAdditionalInformation(existingItem);
                    //更新
                    this.UpdatedFiles[newItemId] = newItem;

                    Debug.WriteLine($"updated : {newItemId}");
                }

                if (file.Keywords != null)
                {
                    foreach (var tag in file.Keywords)
                    {
                        var bag = prospectedTags.GetOrAdd(tag, new ConcurrentBag<TagManager>());
                        bag.Add(newItem.TagSet);
                    }
                }
            }
            else
            {
                this.SkippedFiles.Add(newItemId);
            }
        }

        /// <summary>
        /// ファイル検査設定
        /// </summary>
        private class LoadingOptions
        {
            public bool LightMode { get; set; }
            public bool ContainsChildren { get; set; }
            public PropertiesLevel Level { get; set; }
        }
    }
}
