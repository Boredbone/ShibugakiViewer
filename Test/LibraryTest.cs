using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Search;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Extensions;

namespace Test
{
    [TestClass]
    public class LibraryTest
    {

        private LibrarySettings settings;
        private ILibraryConfiguration config;
        private Dictionary<string, FolderContainerDummy> FileEnumerator;

#pragma warning disable 1998
        private async Task WriteLineAsync(string text)
        {
            Debug.WriteLine(text);
        }
#pragma warning restore 1998

        //private async Task WriteSqlLogLineAsync(string text)
        //{
        //    Debug.WriteLine(text);
        //}


        [TestMethod]
        public async Task LibraryCreationTest()
        {

            await this.InitializeTestAsync();
            var library = await this.CreateLibraryAsync();

            //Create Library

            var files = await new SearchInformation(new ComplexSearch(false))
                .SearchAsync(library, 0, 0);


            var originalFiles = this.FileEnumerator
                .SelectMany(x => x.Value.Files)
                .OrderBy(x => x.Path)
                .ToArray();

            Assert.AreEqual(originalFiles.Length, files.Length);

            foreach (var file in files.OrderBy(x => x.Id).Zip
                (originalFiles, (Target, Reference) => new { Target, Reference }))
            {
                Assert.IsTrue(await IsFileEquals(file.Target, file.Reference, library));
            }


        }


        [TestMethod]
        public async Task PropertyChangeTest()
        {

            await this.InitializeTestAsync();
            var library = await this.CreateLibraryAsync();

            var file = this.FileEnumerator
                .SelectMany(x => x.Value.GetAllFiles())
                .First();

            var f1 = await library.GetRecordAsync(file.Path);

            //var f1s = await this.SearchAsync(FileProperty.FileName, file.Name, CompareMode.Equal);
            //var f1 = f1s.Where(x => x.Id == file.GetFileId()).First();

            Assert.AreEqual(500, f1.Height);

            f1.Height = 123;
            f1.Width = 456;
            f1.Size = 789L;

            await Task.Delay(500);

            var f2 = await library.GetRecordAsync(file.Path);

            Assert.AreEqual(123, f2.Height);
            Assert.AreEqual(456, f2.Width);
            Assert.AreEqual(789L, f2.Size);


            var rf = (await this.SearchAsync(FileProperty.Rating, 3, CompareMode.Equal)).First();
            var rfkey = rf.Id;
            var or = rf.GetRate();
            rf.SetRate(or + 1);
            var nr = rf.GetRate();


            var rf3 = await library.GetRecordAsync(rfkey);

            Assert.AreEqual(or, rf3.GetRate());
            Assert.AreNotEqual(nr, rf3.GetRate());


            await Task.Delay(500);
            var rf2 = await library.GetRecordAsync(rfkey);

            Assert.AreNotEqual(or, rf2.GetRate());

            Assert.AreEqual(nr, rf2.GetRate());
        }

        [TestMethod]
        public async Task LibraryTagTest()
        {

            await this.InitializeTestAsync();
            var library = await this.CreateLibraryAsync();

            var files = await new SearchInformation(new ComplexSearch(false))
                .SearchAsync(library, 0, 0);

            //Tag
            var f1 = files[5];
            var f2 = files.Where(x => x.TagSet.Read().ToArray().Length > 0).First();

            var f1Id = f1.Id;
            var f1Tags = f1.TagSet.Read().ToArray();//.OrderBy(x => x).Select(x => x.ToString()).Join();

            var f2Id = f2.Id;
            var f2Tags = f2.TagSet.Read().ToArray();//.OrderBy(x => x).Select(x => x.ToString()).Join();

            var tag1 = new TagInformation() { Name = "newTag1" };
            var tag1Key = library.Tags.SetTag(tag1);
            f1.TagSet.Add(tag1Key);

            var tag2Key = f2.TagSet.Read().First();
            f2.TagSet.Remove(tag2Key);

            var f1TagsNew = f1Tags.Append(tag1Key).OrderBy(x => x).Select(x => x.ToString()).Join();
            var f2TagsNew = f2Tags.Where(x => x != tag2Key).OrderBy(x => x).Select(x => x.ToString()).Join();

            await Task.Delay(500);



            //var search2 = new SearchInformation(new ComplexSearchSetting(false));
            //search2.Root.Children
            //    .Add(new UnitSearchSetting() { Property = FileProperty.Tags, Reference = tag1Key });
            //library.Searcher.AddSearchToDictionaryAndActivate(search2);
            var files2 = await this.SearchAsync(FileProperty.ContainsTag, tag1Key, CompareMode.Equal);

            await this.AreEqualAsync(1, files2.Length);
            await this.AreEqualAsync(f1Id, files2[0].Id);
            await this.AreEqualAsync
                (files2[0].TagSet.Read().OrderBy(x => x).Select(x => x.ToString()).Join(), f1TagsNew);


            //var search3 = new SearchInformation(new ComplexSearchSetting(false));
            //search3.Root.Children
            //    .Add(new UnitSearchSetting() { Property = FileProperty.Tags, Reference = tag2Key });
            //library.Searcher.AddSearchToDictionaryAndActivate(search3);
            var files3 = await this.SearchAsync(FileProperty.ContainsTag, tag2Key, CompareMode.Equal);

            await this.AreEqualAsync(0, files3.Where(x => x.Id == f2Id).ToArray().Length);

            var f2r = await library.GetRecordAsync(f2Id);

            await this.AreEqualAsync
                (f2r.TagSet.Read().OrderBy(x => x).Select(x => x.ToString()).Join(), f2TagsNew);

        }

        [TestMethod]
        public async Task PathTest()
        {
            //C:\folder
            //\f1
            //\
            //
            //
            //C:\folder2\folder1b
            //C:\folder2\folder1b\
            //
            //C:\folder2\folder1b\fol2
            //C:\folder2\folder1b\fol2\

            await this.InitializeTestAsync();
            var library = await this.CreateLibraryAsync();

            var root = library.TreeRootNode;

            //var files = await library.SearchAsync();
            //var file = files.Last();
            //var ph = file.PathEntry;

            {
                var files2 = await this.SearchAsync
                    (FileProperty.DirectoryPathStartsWith, "C:\\folder2\\folder1b", CompareMode.Equal);

                Assert.AreEqual(3, files2.Length);
            }

            {
                var files2 = await this.SearchAsync
                    (FileProperty.DirectoryPathStartsWith, "C:\\folder2\\folder1b\\", CompareMode.Equal);

                Assert.AreEqual(2, files2.Length);
            }

            var id = "";
            {
                var files2 = await this.SearchAsync
                    (FileProperty.DirectoryPathStartsWith, "C:\\folder2\\folder1b\\fol2", CompareMode.Equal);

                Assert.AreEqual(1, files2.Length);
                id = files2.First().Id;
            }

            {
                var files2 = await this.SearchAsync
                    (FileProperty.DirectoryPathStartsWith, "C:\\folder2\\folder1b\\fol2\\", CompareMode.Equal);

                Assert.AreEqual(1, files2.Length);
                Assert.AreEqual(id, files2.First().Id);

                var path = files2.First().Directory;

                Assert.AreEqual(path, @"C:\folder2\folder1b\fol2\");
            }
        }

        [TestMethod]
        public async Task GroupTest()
        {
            await this.InitializeTestAsync();
            var library = await this.CreateLibraryAsync();

            var groupKey = "";

            var files = await this.SearchAsync
                (FileProperty.Size, 500L, CompareMode.GreatEqual);

            Record group1a;
            {

                var leader1 = files.OrderBy(x => x.FileName).First();

                var group1Id = await library.Grouping.GroupAsync(files.Select(x => x.Id).ToArray());

                await Task.Delay(300);

                var group1 = await library.GetRecordAsync(group1Id);

                group1.SetSort(new[]
                {
                    new SortSetting() {Property=FileProperty.FileNameSequenceNumRight,IsDescending=false },
                    new SortSetting() {Property=FileProperty.AspectRatio,IsDescending=true },
                    new SortSetting() {Property=FileProperty.DateTimeRegistered,IsDescending=false },
                });

                await Task.Delay(350);

                var member = await library.GroupQuery.SearchAsync(group1, 0, 1000);

                await this.IsGroupEquals(group1, leader1, member);

                group1a = await library.GetRecordAsync(group1.Id);

                await this.IsFileEquals(group1, group1a);

                groupKey = group1.Id;
            }


            var key = group1a.GroupKey;
            var group1aMember = await group1a.SearchAsync(library, 0, 0);

            var leader2 = group1aMember.First(x => x.Id != key);

            //group1a.IsLoaded = false;
            group1a.SetGroupLeader(leader2);

            await this.WriteLineAsync($"leader2:{leader2.Id}");

            group1a.Rating = 5;

            //await Task.Delay(500);

            //await library.SaveGroupChangeAsync(group1a);

            await Task.Delay(500);

            var group1b = await library.GetRecordAsync(groupKey);
            var group1bMember = await group1b.SearchAsync(library, 0, 0);

            Assert.AreEqual(5, group1b.Rating);

            await this.IsGroupEquals(group1b, leader2, group1bMember);


            await this.IsFileOptionsEquals(group1a, group1b);

            //return;

            var members2 = group1bMember.ToList();

            //グループ内のサムネイルでないファイル削除

            var mid1 = members2
                .Where(x => x.Id != group1b.GroupKey)
                .OrderByDescending(x => x.DateCreated)
                .Take(2).ToList();

            foreach (var m in mid1)
            {
                members2.Remove(m);
                //this.FileEnumerator.
                foreach (var folder in this.FileEnumerator)
                {
                    var f = folder.Value.Files.FirstOrDefault(y => y.Path == m.Id);
                    if (f != null)
                    {
                        folder.Value.Files.Remove(f);
                        await this.WriteLineAsync($"remove {f.Path}");
                        break;
                    }
                }
            }

            await this.WriteLineAsync("remove 2");

            //ライブラリ更新
            var result1 = await this.RefreshLibraryAsync(library);

            await this.AreEqualAsync(2, result1.RemovedFiles.Count);


            //情報更新されるか？ソート・サムネイル設定維持されるか？

            var group1c = await library.GetRecordAsync(groupKey);
            var group1cMember = await group1c.SearchAsync(library, 0, 0);


            await this.AreEqualAsync(group1bMember.Length - 2, group1cMember.Length);

            {
                var g2 = await library.GetRecordAsync(groupKey);
                var l2 = await library.GetRecordAsync(g2.GroupKey);
                await this.AreEqualAsync(g2.GroupKey, l2.Id);
                await this.AreEqualAsync(g2.Id, l2.GroupKey);

            }

            await this.IsGroupEquals(group1c, leader2, group1cMember);

            await this.IsFileOptionsEquals(group1b, group1c);


            {
                await this.WriteLineAsync("thumbnail");
                //グループ内のサムネイルファイル削除

                var mid2 = members2
                    .Where(x => x.Id == group1b.GroupKey)
                    .ToList();

                foreach (var m in mid2)
                {
                    members2.Remove(m);
                    //this.FileEnumerator.
                    foreach (var folder in this.FileEnumerator)
                    {
                        var f = folder.Value.Files.FirstOrDefault(y => y.Path == m.Id);
                        if (f != null)
                        {
                            folder.Value.Files.Remove(f);
                            await this.WriteLineAsync($"remove {f.Path}");
                            break;
                        }
                    }
                }
                //ライブラリ更新
                var result2 = await this.RefreshLibraryAsync(library);

                await this.AreEqualAsync(1, result2.RemovedFiles.Count);


                //情報更新されるか？ソート設定維持されるか？サムネイル更新されるか？

                var group1d = await library.GetRecordAsync(groupKey);
                var group1dMember = await group1d.SearchAsync(library, 0, 0);
                var leader3 = await library.GetRecordAsync(group1d.GroupKey);

                await this.AreEqualAsync(group1bMember.Length - 3, group1dMember.Length);

                Assert.AreNotEqual(leader2.Id, group1d.GroupKey);

                await this.IsGroupEquals(group1d, leader3, group1dMember);

                await this.IsFileOptionsEquals(group1b, group1d, true);
            }


            {
                await this.WriteLineAsync("all");

                //グループ内ファイル全削除
                var mid2 = members2
                    .ToList();

                foreach (var m in mid2)
                {
                    members2.Remove(m);
                    //this.FileEnumerator.
                    foreach (var folder in this.FileEnumerator)
                    {
                        var f = folder.Value.Files.FirstOrDefault(y => y.Path == m.Id);
                        if (f != null)
                        {
                            folder.Value.Files.Remove(f);
                            await this.WriteLineAsync($"remove {f.Path}");
                            break;
                        }
                    }
                }
                //ライブラリ更新
                var result2 = await this.RefreshLibraryAsync(library);

                await this.AreEqualAsync(mid2.Count, result2.RemovedFiles.Count);

                //グループ削除され，検索にかからなくなるか？


                var group1d = await library.GetRecordAsync(groupKey);


                Assert.AreEqual(null, group1d);

            }



        }

        [TestMethod]
        public async Task RefreshLibraryTest()
        {
            await this.InitializeTestAsync();
            var library = await this.CreateLibraryAsync();

            //this.config.FileExistingChecker = null;

            //this.settings.Folders.Clear();

            this.FileEnumerator.Clear();


            //this.config.FileExistingChecker = _ => false;

            data2.ForEach((x, c) =>
            {
                var f = LoadTestData(x);
                this.AddFolder(f);



                var existingFolder = library.Folders
                    .GetAvailable()
                    .Where(y => y.Path.Equals(f.Path))
                    .FirstOrDefault();

                if (existingFolder == null)
                {
                    library.Folders.Add(new FolderInformation(f.Path));
                }
                else
                {
                    //this.settings.Folders[existingFolder.Value.Key] = new FolderInformation(f.Path);
                }
            });


            using (var disposables = new CompositeDisposable())
            {

                var receiver = new BehaviorSubject<LibraryLoadResult>(null).AddTo(disposables);

                library.Loaded.Subscribe(receiver).AddTo(disposables);

                //Assert.AreEqual(0, this.settings.Folders.Count);
                library.Folders.GetAvailable().ForEach(x => x.RefreshEnable = true);

                await library.RefreshLibraryAsync(true);
                //library.StartRefreshLibrary();

                await Task.Delay(50);

                var result = receiver.Value;// await receiver.Where(x => x != null).Take(1);


                Debug.WriteLine("Add");
                foreach (var file in result.AddedFiles.OrderBy(x => x.Value.Id))
                {
                    Debug.WriteLine(file.Value.Id);
                }
                Debug.WriteLine("Remove");
                foreach (var file in result.RemovedFiles.OrderBy(x => x.Value.Id))
                {
                    Debug.WriteLine(file.Value.Id);
                }
                Debug.WriteLine("Update");
                foreach (var file in result.UpdatedFiles.OrderBy(x => x.Value.Id))
                {
                    Debug.WriteLine(file.Value.Id);
                }

                await this.AreEqualAsync(1, result.AddedFiles.Count);
                await this.AreEqualAsync("fa5.png", result.AddedFiles.First().Value.FileName);


                await this.AreEqualAsync(1, result.RemovedFiles.Count);
                await this.AreEqualAsync("fb4.png", result.RemovedFiles.First().Value.FileName);


                await this.AreEqualAsync(10, result.UpdatedFiles.Count);
            }
        }

        private async Task<LibraryLoadResult> RefreshLibraryAsync(Library library)
        {
            using (var disposables = new CompositeDisposable())
            {
                var receiver = new BehaviorSubject<LibraryLoadResult>(null).AddTo(disposables);

                library.Loaded.Subscribe(receiver).AddTo(disposables);

                //Assert.AreEqual(0, this.settings.Folders.Count);
                library.Folders.GetAvailable().ForEach(x => x.RefreshEnable = true);

                await library.RefreshLibraryAsync(true);
                //library.StartRefreshLibrary();

                await Task.Delay(50);

                var result = receiver.Value;

                return result;
            }
        }

        private async Task InitializeTestAsync()
        {
            await this.WriteLineAsync("");
            await this.WriteLineAsync(DateTimeOffset.Now.ToString());
        }


        private async Task<Library> CreateLibraryAsync()
        {
            LibraryOwner.Reset();
            //this.settings?.Folders?.Clear();
            this.FileEnumerator?.Clear();

            var path = System.IO.Directory.GetCurrentDirectory();
            var config = new LibraryConfigurationDummy(path);

            this.FileEnumerator = new Dictionary<string, FolderContainerDummy>();// = new FolderAccesserDummy();
            //config.FolderAccesser = this.FileEnumerator;

            config.GetChildFoldersFunction = s =>
            {
                var key = s.TrimEnd(System.IO.Path.DirectorySeparatorChar);

                if (!this.FileEnumerator.ContainsKey(key))
                {
                    Debug.WriteLine(key);
                    this.FileEnumerator.ForEach(x => Debug.WriteLine(x.Key + "," + x.Value.Path));
                    return null;
                }
                return this.FileEnumerator[key]
                    .Folders?.Select(x => x.Value.Path) ?? new string[0];
            };

            config.GetFolderFunction = s =>
            {
                var key = s.TrimEnd(System.IO.Path.DirectorySeparatorChar);
                FolderContainerDummy f;
                this.FileEnumerator.TryGetValue(key, out f);
                return f;
            };

            this.config = config;
            LibraryOwner.SetConfig(config);

            //config.Folders.Add(folder);
            //config.FileExistingChecker = _ => false;

            var library = LibraryOwner.GetCurrent();

            this.settings = new LibrarySettings()
            {
                Version = 10,
            };


            library.InitializeLibrarySettings(settings);

            library.Load();


            library.Clear();

            data.ForEach((x, c) =>
            {
                var f = LoadTestData(x);
                this.AddFolder(f);
                library.Folders.Add(new FolderInformation(f.Path));
            });

            await library.RefreshLibraryAsync(true);

            var search = new SearchInformation(new ComplexSearch(false));

            library.Searcher.AddSearchToDictionary(search);

            return library;
        }

        private async Task<Record[]> SearchAsync
            (FileProperty property, object reference, CompareMode mode)
        {

            var search = new SearchInformation(new ComplexSearch(false));
            search.Root
                .Add(new UnitSearch()
                {
                    Property = property,
                    Reference = reference,
                    Mode = mode,
                });

            var library = LibraryOwner.GetCurrent();

            var files = await search.SearchAsync(library, 0, 0);

            return files;
        }

        private async Task AreEqualAsync<T>(T a, T b)
        {
            await this.WriteLineAsync((a?.ToString() ?? "null") + ", " + (b?.ToString() ?? "null"));
            Assert.AreEqual(a, b);
        }
        private async Task AreNotEqualAsync<T>(T a, T b)
        {
            await this.WriteLineAsync((a?.ToString() ?? "null") + ", " + (b?.ToString() ?? "null"));
            Assert.AreNotEqual(a, b);
        }


        private async Task<bool> IsFileEquals(Record a, Record b)
        {

            await this.AreEqualAsync(a.Id, b.Id);
            await this.AreEqualAsync(a.FullPath, b.FullPath);
            await this.AreEqualAsync(a.Directory, b.Directory);
            await this.AreEqualAsync(a.FileName, b.FileName);
            await this.AreEqualAsync(a.DateModified, b.DateModified);
            await this.AreEqualAsync(a.DateCreated, b.DateCreated);
            await this.AreEqualAsync(a.DateRegistered, b.DateRegistered);
            await this.AreEqualAsync(a.Width, b.Width);
            await this.AreEqualAsync(a.Height, b.Height);
            await this.AreEqualAsync(a.Size, b.Size);

            return await IsFileOptionsEquals(a, b);
        }
        private async Task<bool> IsFileOptionsEquals(Record a, Record b, bool differentThumbnail = false)
        {
            await this.AreEqualAsync(a.TagEntry, b.TagEntry);


            Assert.IsTrue(a.TagSet.Read().OrderBy(x => x).SequenceEqual
                (b.TagSet.Read().OrderBy(x => x), (x, y) => x.Equals(y)));

            await this.AreEqualAsync(
                a.TagSet.Read().Select(x => x.ToString()).OrderBy(x => x).Join(";"),
                b.TagSet.Read().Select(x => x.ToString()).OrderBy(x => x).Join(";"));

            await this.AreEqualAsync(a.Rating, b.Rating);
            //await this.AreEqualAsync(a.IsLeader, b.IsLeader);

            if (differentThumbnail)
            {
                await this.AreNotEqualAsync(a.GroupKey, b.GroupKey);
            }
            else
            {
                await this.AreEqualAsync(a.GroupKey, b.GroupKey);
            }

            if (a.GetSort() == null)
            {
                Assert.IsNull(b.GetSort());
            }
            else
            {
                Assert.IsTrue(a.GetSort().SequenceEqual
                    (b.GetSort(), (x, y) => x.ValueEquals(y)));
            }

            await this.AreEqualAsync(a.SortEntry, b.SortEntry);
            //await this.AreEqualAsync(a.IsParticularFlipDirectionEnabled, b.IsParticularFlipDirectionEnabled);
            //await this.AreEqualAsync(a.IsFlipReversed, b.IsFlipReversed);
            await this.AreEqualAsync(a.FlipDirection, b.FlipDirection);
            await this.AreEqualAsync(a.IsGroup, b.IsGroup);



            await this.AreEqualAsync(a.PreNameLong, b.PreNameLong);
            await this.AreEqualAsync(a.PostNameShort, b.PostNameShort);
            await this.AreEqualAsync(a.NameNumberRight, b.NameNumberRight);
            await this.AreEqualAsync(a.PreNameShort, b.PreNameShort);
            await this.AreEqualAsync(a.PostNameLong, b.PostNameLong);
            await this.AreEqualAsync(a.NameNumberLeft, b.NameNumberLeft);
            await this.AreEqualAsync(a.Extension, b.Extension);
            await this.AreEqualAsync(a.NameLength, b.NameLength);

            return true;
        }




        private async Task<bool> IsFileEquals(Record a, ImageFileInformation b, Library library)
        {
            await this.AreEqualAsync(a.Id, b.Path);
            await this.AreEqualAsync(a.FileName, b.Name);


            await this.AreEqualAsync(a.DateCreated, b.DateCreated);
            await this.AreEqualAsync(a.DateModified, b.DateModified);
            await this.AreEqualAsync(a.Size, b.Size);
            await this.AreEqualAsync(a.Height, b.Height);
            await this.AreEqualAsync(a.Width, b.Width);

            await this.AreEqualAsync(a.Rating, b.Rating);

            var aa = a.TagSet.Read()
                .Select(x => library.Tags.GetTagValue(x).Name)
                .OrderBy(x => x).ToArray();

            var bb = b.Keywords?.OrderBy(x => x).ToArray() ?? new string[0];

            Assert.IsTrue(aa.SequenceEqual(bb, (x, y) => x.Equals(y)));

            return true;
        }


        private async Task<bool> IsGroupEquals
            (Record a, Record leader, Record[] member)
        {

            var dateModified = member.Select(x => x.DateModified).DefaultIfEmpty().Max();
            var dateCreated = member.Select(x => x.DateCreated).DefaultIfEmpty().Max();
            var dateRegistered = member.Select(x => x.DateRegistered).DefaultIfEmpty().Max();

            var offset = dateCreated.Offset;

            await this.AreEqualAsync(a.GroupKey, leader.Id);

            //Assert.IsTrue(leader.IsLeader);
            Assert.IsTrue(a.IsGroup);

            //Debug.WriteLine(leader.Id);

            foreach (var file in member)
            {
                await this.AreEqualAsync(file.GroupKey, a.Id);

                //Debug.WriteLine(file.IsLeader);
                //Debug.WriteLine(file.Id);
                //
                //if (!file.IsLeader)
                //{
                //    Assert.IsTrue((file.Id.Equals(leader.Id) && file.IsLeader) || !file.IsLeader);
                //}
                //continue;
                //Assert.IsTrue((file.Id.Equals(leader.Id) && file.IsLeader) || !file.IsLeader);
                Assert.IsTrue(!file.IsGroup);
            }
            //return true;

            //Debug.WriteLine(a == null);
            //Debug.WriteLine(a.FullPath == null);
            //Debug.WriteLine(leader == null);
            //Debug.WriteLine(leader.FullPath == null);

            await this.AreEqualAsync(a.Directory, leader.Directory);

            await this.AreEqualAsync(a.DateModified, dateModified);
            await this.AreEqualAsync(a.DateCreated, dateCreated);
            await this.AreEqualAsync(a.DateRegistered, dateRegistered);

            await this.AreEqualAsync(a.Width, leader.Width);
            await this.AreEqualAsync(a.Height, leader.Height);
            await this.AreEqualAsync(a.Size, leader.Size);


            return true;
        }

        public static async Task<bool> SequenceEqualAsync<T1, T2>
            (IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, Task<bool>> match)
        {
            using (var e1 = first.GetEnumerator())
            using (var e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && await match(e1.Current, e2.Current))) return false;
                }
                if (e2.MoveNext()) return false;
            }
            return true;
        }

        private FolderContainerDummy LoadTestData(string text)
        {

            FolderContainerDummy folder = null;

            using (var sr = new StringReader(text))
            {
                var line = "";
                while ((line = sr.ReadLine()) != null)
                {

                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    var items = line.Split(',');

                    if (items.Count(x => x != null && x.Length > 0) <= 0)
                    {
                        continue;
                    }
                    else if (folder == null)
                    {
                        folder = new FolderContainerDummy()
                        {
                            Path = items.Join(),
                        };
                        continue;
                    }
                    else if (folder.Folders == null)
                    {
                        folder.Folders = items
                            .Where(x => x != null && x.Length > 0)
                            .ToDictionary(x => x.Replace("\\", ""),
                                x => new FolderContainerDummy()
                                {
                                    Path = folder.Path + x
                                });

                        foreach (var tt in folder.Folders)
                        {
                            this.AddFolder(tt.Value);
                        }

                        continue;
                    }
                    else if (items.Length < 9)
                    {
                        continue;
                    }

                    var name = items[0] + ".png";
                    var relativePath = items[1].Split('\\').Where(x => x != null && x.Length > 0).ToArray();
                    var created = ParseDateTime(items[2]);
                    var modified = ParseDateTime(items[3]);
                    var size = ParseInt(items[4]);
                    var height = ParseInt(items[5]);
                    var width = ParseInt(items[6]);
                    var rating = ParseInt(items[7]);
                    var keywords = items[8].Split(';').Where(x => x != null && x.Length > 0).ToArray();


                    var targetFolder = folder;
                    //if (relativePath.Length > 0)


                    foreach (var str in relativePath.Select(x => x))
                    {
                        var cfPath = targetFolder.Path + "\\" + str;
                        if (!targetFolder.Folders.ContainsKey(cfPath))
                        {
                            var childFolder = new FolderContainerDummy()
                            {
                                Path = cfPath,
                            };

                            childFolder.Folders = new Dictionary<string, FolderContainerDummy>();

                            targetFolder.Folders[cfPath] = childFolder;

                        }

                        foreach (var tt in targetFolder.Folders)
                        {
                            this.AddFolder(tt.Value);
                        }
                        targetFolder = targetFolder.Folders[cfPath];

                        foreach (var tt in targetFolder.Folders)
                        {
                            this.AddFolder(tt.Value);
                        }
                    }


                    var path = targetFolder.Path + @"\" + name;

                    var file = new ImageFileInformation()
                    {
                        Name = name,
                        Path = path,
                        DateCreated = created,
                        DateModified = modified,
                        Size = size,
                        Height = height,
                        Width = width,
                        Rating = rating,
                        Keywords = (keywords.Length > 0) ? new HashSet<string>(keywords) : null,
                    };

                    targetFolder.Files.Add(file);

                }
            }
            return folder;
        }


        private DateTimeOffset ParseDateTime(string text)
        {
            var date = DateTime.ParseExact(text, "yyyy/MM/dd HH:mm:ss", null);
            return new DateTimeOffset(date);
        }


        private int ParseInt(string text)
        {
            int result = 0;
            int.TryParse(text, out result);
            return result;
        }

        private void AddFolder(FolderContainerDummy folder)
        {
            this.FileEnumerator[folder.Path] = folder;


            foreach (var cf in folder.GetChildFolders())
            {
                this.FileEnumerator[cf.Value.Path] = cf.Value;
            }
        }

        /*
        private class DummyLibraryConfiguration : ILibraryConfiguration
        {
            public bool IsKnownFolderEnabled => false;

            public string SaveDirectory
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Func<Record, bool> FileExistingChecker;

            //public HashSet<StorageFolderDummy> Folders { get; private set; } = new HashSet<StorageFolderDummy>();

            public ITraceableStorageFolder GetNewFolderTracer()
            {
                return new TraceableStorageFolder();
            }
            

            public async Task<bool> IsFileExistsAsync(Record file)
            {
                return this.FileExistingChecker?.Invoke(file) ?? true;
            }

            public async Task<bool> IsFolderExistsAsync(FolderInformation folder)
            {
                return true;
            }
        }*/

        private string[] data = new[] { @"//C:\folder1\
//Name=folder1,Path=C:\folder1
C:,\folder1
\folder1a,\folder1b,\folder1c

//Files
//Path=folder.Path+RelativePath+@""\""+Name+"".png""
//Name,RelativePath,Created,Modified,Size,Height,Width,Rating,Keywords

fa1,,2010/05/12 10:58:19,2010/05/14 12:05:29,1135,1000,500,0,
fa2,,2010/05/12 10:59:20,2010/05/12 10:59:20,1200,500,800,0,
fa3,,2011/03/08 16:28:19,2011/03/08 16:28:19,700,800,800,99,
fa4,,2011/03/09 16:28:19,2011/03/09 16:30:19,700,800,800,0,tag1;tag2
fb1,\folder1a,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,50,
fb2,\folder1a,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,99,tag2;tag3
fb3,\folder1b,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,0,
fb4,\folder1b,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,0,tag4

",
            @"C:,\folder2
\folder2a,\folder2b

fa0,,2010/05/12 10:58:19,2010/05/14 12:05:29,1135,1000,500,0,
fa2,,2010/05/12 10:59:20,2010/05/12 10:59:20,1200,500,800,0,
fa3,,2011/03/08 16:28:19,2011/03/08 16:28:19,700,800,800,99,
fa4,,2011/03/09 16:28:19,2011/03/09 16:30:19,700,800,800,0,tag1;tag2
fa1,\folder1a,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,50,
fa2,\folder1a,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,99,tag2;tag3
fa3,\folder1b,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,0,
fa4,\folder1b,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,0,tag4
fa4,\folder1b\fol2,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,0,tag4

",
        };
        private string[] data2 = new[] { @"//C:\folder1\
//Name=folder1,Path=C:\folder1
C:,\folder1
\folder1a,\folder1b,\folder1c

//Files
//Path=folder.Path+RelativePath+@""\""+Name+"".png""
//Name,RelativePath,Created,Modified,Size,Height,Width,Rating,Keywords

fa1,,2010/05/12 10:58:19,2010/05/14 12:05:29,1135,1000,500,0,
fa2,,2010/05/12 10:59:20,2010/05/12 10:59:20,1200,500,800,0,
fa3,,2011/03/08 16:28:19,2011/03/08 16:28:19,700,800,800,99,
fa4,,2011/03/09 16:28:19,2011/03/09 16:30:19,700,800,800,0,tag1;tag2
fa5,,2015/08/23 10:05:33,2015/08/25 09:33:55,200,600,300,0,
fb1,\folder1a,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,50,
fb2,\folder1a,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,99,tag2;tag3
fb3,\folder1a,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,0,
//fb4,\folder1b,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,0,tag4

",
            @"C:,\folder2p
\folder2a,\folder2b

fa1,,2010/05/12 10:58:19,2010/05/14 12:05:29,1135,1000,500,0,
fa2,,2010/05/12 10:59:20,2010/05/12 10:59:20,1200,500,800,0,
fa3,,2011/03/08 16:28:19,2011/03/08 16:28:19,700,800,800,99,
fa4,,2011/03/09 16:28:19,2011/03/09 16:30:19,700,800,800,0,tag1;tag2
fa1,\folder1a,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,50,
fa2,\folder1a,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,99,tag2;tag3
fa3,\folder1b,2010/05/12 10:59:20,2010/06/12 10:30:00,1000,500,800,0,
fa4,\folder1b,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,0,tag4
fa4,\folder1b\fol2,2000/05/01 10:59:20,2000/06/01 10:30:00,200,500,30,0,tag4

", @"C:,\folder2
\folder2a

",
        };
    }
}
