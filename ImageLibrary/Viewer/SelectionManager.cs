using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;
using Boredbone.Utility.Extensions;
using System.ComponentModel;
using ImageLibrary.File;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using ImageLibrary.Core;
using ImageLibrary.Tag;
using System.Windows;
using System.Windows.Threading;
using Boredbone.Utility.Notification;
using Reactive.Bindings;
using System.Reactive.Disposables;
using System.Diagnostics;
using System.Threading;

namespace ImageLibrary.Viewer
{
    public class SelectionManager : NotificationBase
    {

        public int Count => this.ItemsSet.Count;
        private Dictionary<string, Record> ItemsSet { get; }

        private Subject<Dictionary<string, Record>> AddedSubject { get; }
        public IObservable<Dictionary<string, Record>> Added => this.AddedSubject.AsObservable();
        private Subject<Dictionary<string, Record>> RemovedSubject { get; }
        public IObservable<Dictionary<string, Record>> Removed => this.RemovedSubject.AsObservable();
        private Subject<Unit> ClearedSubject { get; }
        public IObservable<Unit> Cleared => this.ClearedSubject.AsObservable();

        private BehaviorSubject<Record> SelectedItemSubject { get; }
        public IObservable<Record> SelectedItemChanged => this.SelectedItemSubject.AsObservable();
        public Record SelectedItem => this.SelectedItemSubject.Value;

        public ObservableCollection<string> Ids { get; }

        public Record LastSelectedItem { get; private set; } = null;


        public ObservableCollection<TagInformation> CommonTags { get; }

        public ReactiveProperty<int> CommonRating { get; }
        public ReadOnlyReactiveProperty<bool> IsRatingUnknown { get; }

        private int CommonRatingInner
        {
            get { return _fieldCommonRatingInner; }
            set
            {
                if (_fieldCommonRatingInner != value)
                {
                    _fieldCommonRatingInner = value;
                    this.CommonRating.Value = value;
                }
            }
        }
        private int _fieldCommonRatingInner = -1;


        private readonly Library library;


        public SelectionManager(Library library)
        {
            this.library = library;

            this.CommonTags = new ObservableCollection<TagInformation>();

            this.ItemsSet = new Dictionary<string, Record>();

            this.AddedSubject = new Subject<Dictionary<string, Record>>().AddTo(this.Disposables);
            this.RemovedSubject = new Subject<Dictionary<string, Record>>().AddTo(this.Disposables);
            this.ClearedSubject = new Subject<Unit>().AddTo(this.Disposables);

            this.SelectedItemSubject = new BehaviorSubject<Record>(null).AddTo(this.Disposables);

            this.Ids = new ObservableCollection<string>();

            this.AddedSubject.Select(_ => Unit.Default)
                .Merge(this.RemovedSubject.Select(_ => Unit.Default))
                .Merge(this.ClearedSubject)
                .Subscribe(_ => this.RefreshCommonInformation())
                .AddTo(this.Disposables);

            this.CommonRating = new ReactiveProperty<int>(this.CommonRatingInner).AddTo(this.Disposables);
            this.IsRatingUnknown = this.CommonRating.Select(x => x < 0)
                .ToReadOnlyReactiveProperty().AddTo(this.Disposables);

            this.CommonRating.Subscribe(x =>
            {
                if (this.CommonRatingInner != x)
                {
                    this.CommonRatingInner = x;
                    if (x >= 0)
                    {
                        this.UpdateRating(x);
                    }
                }
            })
            .AddTo(this.Disposables);
        }


        public bool AddOrReplace(Record item)
        {

            this.LastSelectedItem = item;

            if (!this.ItemsSet.ContainsKey(item.Id))
            {
                this.ItemsSet.Add(item.Id, item);
                this.SelectedItemSubject.OnNext(item);

                this.NotifyCountChanged();

                var dic = new Dictionary<string, Record>()
                {
                    [item.Id] = item
                };
                this.Ids.Add(item.Id);
                this.AddedSubject.OnNext(dic);

                return true;
            }
            else
            {
                this.ItemsSet[item.Id] = item;
                if (this.SelectedItemSubject.Value == null)
                {
                    this.SelectedItemSubject.OnNext(item);
                }

                return false;
            }
        }

        public void Add(string id)
        {
            this.LastSelectedItem = null;

            if (!this.ItemsSet.ContainsKey(id))
            {
                this.ItemsSet.Add(id, null);

                this.NotifyCountChanged();

                var dic = new Dictionary<string, Record>()
                {
                    [id] = null
                };
                this.Ids.Add(id);
                this.AddedSubject.OnNext(dic);

            }
        }

        public void AddOrReplaceRange(IEnumerable<Record> items)
        {
            var dic = new Dictionary<string, Record>();
            Record lastItem = null;

            foreach (var item in items)
            {
                if (!this.ItemsSet.ContainsKey(item.Id))
                {
                    this.ItemsSet.Add(item.Id, item);
                    dic.Add(item.Id, item);
                    lastItem = item;
                    this.Ids.Add(item.Id);
                }
                else
                {
                    this.ItemsSet[item.Id] = item;
                }
                this.LastSelectedItem = item;
            }


            if (dic.Count <= 0)
            {
                return;
            }

            if (lastItem != null)
            {
                this.SelectedItemSubject.OnNext(lastItem);
            }

            this.NotifyCountChanged();

            this.AddedSubject.OnNext(dic);

        }

        public void AddRange(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return;
            }

            var dic = new Dictionary<string, Record>();

            this.LastSelectedItem = null;

            foreach (var id in ids)
            {
                if (!this.ItemsSet.ContainsKey(id))
                {
                    this.ItemsSet.Add(id, null);
                    dic.Add(id, null);
                    this.Ids.Add(id);
                }
            }


            if (dic.Count <= 0)
            {
                return;
            }

            this.NotifyCountChanged();

            this.AddedSubject.OnNext(dic);
        }


        public bool Remove(Record item) => this.Remove(item.Id);


        public bool Remove(string id)
        {
            this.LastSelectedItem = null;

            if (this.ItemsSet.ContainsKey(id))
            {
                this.ItemsSet.Remove(id);

                if (this.SelectedItemSubject.Value?.Id == id)
                {
                    this.SelectedItemSubject.OnNext(this.ItemsSet.Select(x => x.Value).FirstOrDefault());
                }

                this.NotifyCountChanged();

                var dic = new Dictionary<string, Record>()
                {
                    [id] = null
                };
                this.Ids.Remove(id);
                this.RemovedSubject.OnNext(dic);

                return true;

            }
            return false;
        }

        public void RemoveRange(IEnumerable<Record> items)
        {
            this.LastSelectedItem = null;

            var dic = new Dictionary<string, Record>();
            Record lastItem = null;

            foreach (var item in items)
            {
                if (this.ItemsSet.ContainsKey(item.Id))
                {
                    this.ItemsSet.Remove(item.Id);
                    dic.Add(item.Id, item);
                    lastItem = item;
                    this.Ids.Remove(item.Id);
                }
            }

            if (dic.Count <= 0)
            {
                return;
            }

            if (lastItem != null && this.SelectedItemSubject.Value == lastItem)
            {
                this.SelectedItemSubject.OnNext(this.ItemsSet.Select(x => x.Value).FirstOrDefault());
            }

            this.NotifyCountChanged();

            this.RemovedSubject.OnNext(dic);
        }

        public void RemoveRange(IEnumerable<string> ids)
        {
            this.LastSelectedItem = null;

            var dic = new Dictionary<string, Record>();
            string lastId = null;

            foreach (var id in ids)
            {
                if (this.ItemsSet.ContainsKey(id))
                {
                    this.ItemsSet.Remove(id);
                    dic.Add(id, null);
                    lastId = id;
                    this.Ids.Remove(id);
                }
            }

            if (dic.Count <= 0)
            {
                return;
            }
            if (lastId != null && this.SelectedItemSubject.Value.Id == lastId)
            {
                this.SelectedItemSubject.OnNext(this.ItemsSet.Select(x => x.Value).FirstOrDefault());
            }

            this.NotifyCountChanged();

            this.RemovedSubject.OnNext(dic);
        }



        public void Clear()
        {
            this.LastSelectedItem = null;
            this.ItemsSet.Clear();

            this.SelectedItemSubject.OnNext(null);

            this.NotifyCountChanged();

            this.Ids.Clear();
            this.ClearedSubject.OnNext(Unit.Default);
        }

        public void ClearCache()
        {
            var keys = this.ItemsSet.Select(x => x.Key).ToArray();
            keys.ForEach(x => this.ItemsSet[x] = null);

            if (this.ItemsSet.Count > 1)
            {
                this.SelectedItemSubject.OnNext(null);
            }

            this.RefreshCommonInformation();
        }

        public bool Contains(Record item)
        {
            return this.ItemsSet.ContainsKey(item.Id);
        }
        public bool Contains(string id)
        {
            return this.ItemsSet.ContainsKey(id);
        }

        public bool Toggle(Record item)
        {
            if (!this.ItemsSet.ContainsKey(item.Id))
            {
                this.AddOrReplace(item);
                return true;
            }
            this.Remove(item);
            return false;
        }
        public bool Toggle(string id)
        {
            if (this.ItemsSet.ContainsKey(id))
            {
                this.Add(id);
                return true;
            }
            this.Remove(id);
            return false;
        }


        public void UpdateRating(int value)
            => this.library.QueryHelper.UpdateRatingAsync(this.ItemsSet.Select(x => x.Key), value).FireAndForget();

        public void AddTag(TagInformation tag)
            => this.library.QueryHelper.AddTagAsync(this.ItemsSet.Select(x => x.Key), tag).FireAndForget();

        public void RemoveTag(TagInformation tag)
            => this.library.QueryHelper.RemoveTagAsync(this.ItemsSet.Select(x => x.Key), tag).FireAndForget();


        private void RefreshCommonInformation()
        {
            Task.Run(async () => await this.RefreshCommonInformationAsync()).FireAndForget();
        }

        private async Task RefreshCommonInformationAsync()
        {
            IEnumerable<int> tags;
            string[] ids;

            var items = this.ItemsSet.ToArray();

            tags = this.GetCommonTagsFromCache(items);

            int rating;
            var ratingResult = this.GetCommonRatingFromCache(items, out rating);

            if (tags == null || !ratingResult)
            {
                ids = items.Select(x => x.Key).ToArray();

                if (tags == null)
                {
                    tags = await this.library.QueryHelper.GetCommonTagsAsync(ids);
                }
                if (!ratingResult)
                {
                    rating = await this.library.QueryHelper.GetCommonRatingAsync(ids);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                this.CommonTags.Clear();
                tags.Select(x => this.library.Tags.GetTagValue(x))
                    .OrderBy(x => x.Name)
                    .ForEach(x => this.CommonTags.Add(x));

                this.CommonRatingInner = rating;

            }, DispatcherPriority.Background);

        }

        private int[] GetCommonTagsFromCache(KeyValuePair<string, Record>[] items)
        {
            //var items = this.ItemsSet.ToArray();

            if (items.Length <= 0)
            {
                return new int[0];
            }
            if (items.Any(x => x.Value == null))
            {
                return null;
            }

            var record = items.First().Value;

            return record.TagSet.Read()
                .Where(tag => items.All(x => x.Value != null && x.Value.TagSet.Contains(tag)))
                .ToArray();
        }

        private bool GetCommonRatingFromCache(KeyValuePair<string, Record>[] items, out int rating)
        {
            //var items = this.ItemsSet.ToArray();

            if (items.Length <= 0)
            {
                rating = -1;
                return true;
            }
            if (items.Any(x => x.Value == null))
            {
                rating = -1;
                return false;
            }

            var values = items.Select(x => x.Value.Rating).Distinct().Take(2).ToArray();

            if (values.Length == 1)
            {
                rating = values[0];
                return true;
            }

            rating = -1;
            return true;
        }


        public IDisposable SubscribeCollectionChanged
            (Func<bool> valueGetter, Action<bool> valueSetter, Func<string> keyGetter)
        {
            var subscription = new CompositeDisposable();

            this.Added.Subscribe(x =>
            {
                if (!valueGetter())
                {
                    var key = keyGetter();
                    if (key != null)
                    {
                        if (x.ContainsKey(key))
                        {
                            valueSetter(true);
                        }
                    }
                }
            })
            .AddTo(subscription);

            this.Removed.Subscribe(x =>
            {
                if (valueGetter())
                {
                    var key = keyGetter();
                    if (key != null)
                    {
                        if (x.ContainsKey(key))
                        {
                            valueSetter(false);
                        }
                    }
                }
            })
            .AddTo(subscription);

            this.Cleared.Subscribe(x =>
            {
                var key = keyGetter();
                if (key != null)
                {
                    valueSetter(this.Contains(key));
                }
            })
            .AddTo(subscription);

            return subscription;
        }


        public IEnumerable<KeyValuePair<string, Record>> GetAll()
            => this.ItemsSet;

        private void NotifyCountChanged()
        {
            this.RaisePropertyChanged(nameof(Count));
        }
    }
}
