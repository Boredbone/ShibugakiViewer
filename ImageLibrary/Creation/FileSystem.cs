using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility.Tools;
using ImageLibrary.SearchProperty;
using Shell32;

namespace ImageLibrary.Creation
{
    /// <summary>
    /// Windowsファイルシステムへのアクセス
    /// </summary>
    public class FileSystem
    {

        private readonly Dictionary<string, Shell32.Folder> cache;
        object syncObject = new object();

        private Type shellType;
        private object shellInstance;
        private bool isShellInitialized = false;

        private bool isShellEnabled = false;

        private static DateTimeOffset defaultDateTime
            = ImageFileUtility.ConvertDateTime(default(DateTimeOffset));



        public FileSystem()
        {
            this.cache = new Dictionary<string, Shell32.Folder>();
        }

        /// <summary>
        /// シェルのフォルダオブジェクトを取得
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        private Shell32.Folder GetFolder(string directoryPath)
        {
            if (!this.isShellInitialized)
            {
                try
                {
                    this.shellType = Type.GetTypeFromProgID("Shell.Application");
                    this.shellInstance = Activator.CreateInstance(this.shellType);
                    this.isShellEnabled = true;
                }
                catch
                {

                }
                this.isShellInitialized = true;
            }

            if (this.isShellEnabled)
            {
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    return null;
                }

                try
                {
                    var folder = (Shell32.Folder)this.shellType.InvokeMember("NameSpace",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null, this.shellInstance,
                        new object[] { directoryPath });

                    return folder;
                }
                catch
                {

                }
            }

            return null;

        }

        /// <summary>
        /// 画像ファイルの情報を取得
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public ImageFileInformation GetImage(string fullPath, PropertiesLevel level)
        {
            ImageFileInformation image = null;
            var hasDecoration = true;

            try
            {
                var name = System.IO.Path.GetFileName(fullPath);

                var isUnknownType = false;

                var width = 0;
                var height = 0;
                var length = 0L;

                //画像ファイルのヘッダからサイズを抽出
                if (level >= PropertiesLevel.Size)
                {
                    var graphicInfo = new GraphicInformation(fullPath);

                    width = graphicInfo.GraphicSize.Width;
                    height = graphicInfo.GraphicSize.Height;
                    length = graphicInfo.FileSize;
                    isUnknownType = graphicInfo.Type == GraphicFileType.Unknown;

                    //タグ・評価を付加できないファイルの場合は情報読み取りをスキップ
                    if (graphicInfo.Type != GraphicFileType.Jpeg
                        && graphicInfo.Type != GraphicFileType.Tiff
                        && graphicInfo.Type != GraphicFileType.Psd
                        && graphicInfo.Type != GraphicFileType.Unknown)
                    {
                        hasDecoration = false;
                    }
                }

                var creationTime = defaultDateTime;
                var lastWriteTime = defaultDateTime;

                //日付情報
                if (level >= PropertiesLevel.Basic)
                {
                    try
                    {
                        var file = new System.IO.FileInfo(fullPath);

                        creationTime = ImageFileUtility.ConvertDateTime(file.CreationTime);
                        lastWriteTime = ImageFileUtility.ConvertDateTime(file.LastWriteTime);
                    }
                    catch
                    {
                        //No operation
                    }
                }


                image = new ImageFileInformation()
                {
                    DateCreated = creationTime,
                    DateModified = lastWriteTime,
                    Name = name,
                    Path = fullPath,
                    Height = height,
                    Width = width,
                    Size = length,
                    Rating = 0,
                    Keywords = null,
                    IsNotFound = isUnknownType,
                };

            }
            catch
            {
                return null;
            }


            if (image == null)
            {
                return null;
            }


            var rating = 0;
            HashSet<string> tags = null;

            //画像の評価・キーワード(時間かかる)
            if (level >= PropertiesLevel.Shell && hasDecoration)
            {
                try
                {
                    var fileAccesser = this.GetFile(fullPath, image.Name);


                    var kw = fileAccesser.GetDetailsOf(18);
                    if (kw != null)
                    {
                        tags = new HashSet<string>(kw
                            .Split(';')
                            .Select(x => x.Trim())
                            .Where(x => x != null && x.Length > 0)
                            .Distinct());
                    }

                    rating = 0;
                    var rateText = fileAccesser.GetDetailsOf(19);
                    var rateArray = rateText?
                        .Select(x => x - '0')
                        .Where(x => x > 0 && x <= 5)
                        .ToArray();

                    if (rateArray != null && rateArray.Length == 1)
                    {
                        rating = RateConvertingHelper.Reverse(rateArray[0]);
                    }
                }
                catch
                {
                    return null;
                }
            }

            image.Rating = rating;
            image.Keywords = tags;

            return image;
        }

        /// <summary>
        /// シェルのファイルオブジェクト
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private FileAccesser GetFile(string fullPath, string name)
        {
            var dir = System.IO.Path.GetDirectoryName(fullPath);
            var key = dir.ToLower();


            Shell32.Folder folder = null;
            if (!this.cache.TryGetValue(key, out folder))
            {
                lock (this.syncObject)
                {
                    if (!this.cache.ContainsKey(key))
                    {
                        folder = this.GetFolder(dir);
                        this.cache.Add(key, folder);
                    }
                    else
                    {
                        folder = this.cache[key];
                    }
                }
            }


            return new FileAccesser(fullPath, name, folder);

        }

        /// <summary>
        /// シェルのファイル情報を取得
        /// </summary>
        private class FileAccesser
        {
            public string FullPath { get; }

            private readonly Shell32.Folder folder;
            private readonly Shell32.FolderItem file;

            public FileAccesser(string fullPath, string name, Shell32.Folder folder)
            {
                this.FullPath = fullPath;

                this.file = null;
                if (folder != null)
                {
                    try
                    {
                        this.file = folder.ParseName(name);
                        this.folder = folder;
                    }
                    catch
                    {
                        this.file = null;
                        this.folder = null;
                    }
                }
                else
                {
                    this.file = null;
                    this.folder = null;

                }


            }

            public string GetDetailsOf(int key)
            {
                if (this.folder == null || this.file == null)
                {
                    return null;
                }
                try
                {
                    return this.folder.GetDetailsOf(this.file, key);
                }
                catch
                {
                    return null;
                }

            }
        }
    }
}
