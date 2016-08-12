using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;

namespace SparkImageViewer.DataModel
{
    public class ImageLibrary
    {


        private const string libraryListFileName = "liblist.xml";
        private XmlSettingManager<SavedLibraryList> libraryListXml
            = new XmlSettingManager<SavedLibraryList>(libraryListFileName);


        private const string libraryDiffFileName = "libdiff.xml";
        private XmlSettingManager<List<KeyValuePair<string, FileInformation>>> libraryDiffXml
            = new XmlSettingManager<List<KeyValuePair<string, FileInformation>>>(libraryDiffFileName);

        private const int xmlVersion = 3;

        private const string libraryFileNameHeader = "lib";
        private const string libraryFileNameExt = ".xml";

        private static AsyncLock asyncLock = new AsyncLock();
        private AsyncLock storageAccessLock = new AsyncLock();

        //private ApplicationCoreData settings;
        private SearchSortManager searcher;

        public Dictionary<string, FileInformation> FileDictionary { get; private set; }

        private string directory;

        public ImageLibrary(SearchSortManager searcher, string directory)
        {
            this.searcher = searcher;
            this.directory = directory;

            libraryListXml = new XmlSettingManager<SavedLibraryList>
                (System.IO.Path.Combine(directory, libraryListFileName));

            libraryDiffXml = new XmlSettingManager<List<KeyValuePair<string, FileInformation>>>
                ((System.IO.Path.Combine(directory, libraryDiffFileName)));
        }

        private void SendMessage(string text)
        {

        }


        /// <summary>
        /// 保存したファイルからライブラリをロード
        /// </summary>
        /// <returns></returns>
        public async Task LoadLibraryAsync()
        {

            using (await this.storageAccessLock.LockAsync())
            {
                var succeeded = false;
                var dictionary = new Dictionary<string, FileInformation>();

                try
                {

                    //ライブラリファイルのリストを読み込み
                    var loadedList = await this.libraryListXml.LoadXmlAsync(XmlLoadingOptions.ThrowAll);
                    var list = loadedList.Value.FileNames;

                    if (list != null && list.Count > 0)
                    {
                        //正常に読み込まれた

                        //リストに記載されたファイルからライブラリデータ本体を読み込み
                        var libraries = await list
                            .Select(x => new XmlSettingManager<List<KeyValuePair<string, FileInformation>>>
                                (System.IO.Path.Combine(directory, x)))
                            .Select(async x => await x.LoadXmlAsync(XmlLoadingOptions.ThrowAll))
                            .WhenAll();

                        //結合
                        dictionary = libraries.Select(x => x.Value)
                            .SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);

                        succeeded = true;
                    }

                }
                catch
                {
                    succeeded = false;
                }

                if (!succeeded)
                {
                    //ライブラリファイルのリストを読み込み
                    var loadedList = this.libraryListXml.LoadBackupXml(XmlLoadingOptions.IgnoreNotFound);
                    var list = loadedList.Value.FileNames;


                    if (loadedList.Message != null)
                    {
                        this.SendMessage(loadedList.Message.ToString());
                    }

                    if (list != null && list.Count > 0)
                    {
                        //正常に読み込まれた

                        //リストに記載されたファイルからライブラリデータ本体を読み込み
                        var libraries = list
                            .Select(x => new XmlSettingManager<List<KeyValuePair<string, FileInformation>>>
                                (System.IO.Path.Combine(directory, x)))
                            .Select(x => x.LoadBackupXml(XmlLoadingOptions.IgnoreNotFound))
                            .ToArray();

                        var error = libraries.Select(x => x.Message).Where(x => x != null).ToArray();
                        if (error.Length > 0)
                        {
                            this.SendMessage(error.Select(x => x.Message).Join("\n"));
                        }

                        //結合
                        dictionary = libraries.Select(x => x.Value)
                            .SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);

                        succeeded = true;
                    }
                }




                if (!succeeded)
                {
                    //ライブラリリストが読み込めなかった
                    //旧フォーマットのライブラリがあるかチェック
                    var result = await (new XmlSettingManager<Dictionary<string, FileInformation>>
                                (System.IO.Path.Combine(directory, "lib.xml")))
                        .LoadXmlAsync(XmlLoadingOptions.IgnoreNotFound);

                    if (result != null && result.Value.Count > 0)
                    {
                        dictionary = result.Value;
                    }
                    else
                    {
                        //新規にライブラリを作成
                        dictionary = new Dictionary<string, FileInformation>();
                    }
                }


                var diffKey = new HashSet<string>();

                //差分セーブデータをチェック
                try
                {
                    var diff = await this.libraryDiffXml.LoadXmlAsync
                        (XmlLoadingOptions.IgnoreAllException | XmlLoadingOptions.ReturnNull);
                    if (diff != null && diff.Value != null)
                    {
                        foreach (var item in diff.Value)
                        {
                            //item.Value.IsEdited = true;
                            dictionary[item.Key] = item.Value;
                            diffKey.Add(item.Key);
                        }
                        //dictionary.Merge(diff.Value);
                    }

                    //await this.libraryDiffXml.DeleteFileAsync();
                }
                catch
                {
                    //例外は無視
                }







                var unexistingGroupLeader = new HashSet<string>();

                //各アイテムごとに
                foreach (var item in dictionary)
                {
                    if (item.Value.GroupLeaderKey != null)
                    {
                        //グループリーダーが辞書に登録されていたら参照を取得
                        if (this.searcher.GroupLeaderDictionary.ContainsKey(item.Value.GroupLeaderKey))
                        {
                            item.Value.GroupLeader
                                = this.searcher.GroupLeaderDictionary[item.Value.GroupLeaderKey];
                        }
                        else if (!unexistingGroupLeader.Contains(item.Value.GroupLeaderKey))
                        {
                            //辞書にリーダーが見つからなかったらそのリーダーを保持
                            unexistingGroupLeader.Add(item.Value.GroupLeaderKey);
                        }
                    }
                }

                //辞書に見つからなかったリーダーは新しく作り直す
                foreach (var leader in unexistingGroupLeader)
                {
                    var key = this.GenerateNewGroupKey();

                    var files = dictionary
                        .Where(x => leader.Equals(x.Value.GroupLeaderKey));
                    //.OrderBy(x => x.Value.RelativePath);

                    var defaultCoverFile = files.First();
                    defaultCoverFile.Value.UniqueKey = defaultCoverFile.Key;

                    var newLeader = new GroupLeaderFile(key, defaultCoverFile.Value);

                    this.searcher.GroupLeaderDictionary.Add(key, newLeader);

                    files.ForEach(x => x.Value.GroupLeader = newLeader);
                }

                /*
                var emptyGroups = this.searcher.GroupLeaderDictionary
                    .Where(x => x.Value.ChildrenCount == 0)
                    .Select(x => x.Key)
                    .ToArray();

                foreach (var item in emptyGroups)
                {
                    this.searcher.GroupLeaderDictionary.Remove(item);
                }
                */



                foreach (var item in dictionary)
                {
                    item.Value.UniqueKey = item.Key;
                }
                foreach (var key in diffKey)
                {
                    //dictionary[key].IsEdited = true;
                }


                this.FileDictionary = dictionary;

            }


        }


        private string GenerateNewGroupKey()
        {
            string key;

            for (int i = 0; i < 99; i++)
            {
                key = Guid.NewGuid().ToString();
                if (!this.searcher.GroupLeaderDictionary.ContainsKey(key))
                {
                    return key;
                }
            }

            throw new Exception("Guid Generation Error");
        }




    }
}
