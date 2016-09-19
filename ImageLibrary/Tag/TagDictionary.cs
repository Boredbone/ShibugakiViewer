using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;

namespace ImageLibrary.Tag
{

    public class TagDictionary : DisposableBase
    {
        private ConcurrentDictionary<int, TagInformation> RegisteredTags { get; set; }
        public bool IsEdited { get; set; }

        private Subject<TagInformation> AddedSubject { get; }
        public IObservable<TagInformation> Added => this.AddedSubject.AsObservable();


        public TagDictionary()
        {
            this.AddedSubject = new Subject<TagInformation>().AddTo(this.Disposables);

            this.RegisteredTags = new ConcurrentDictionary<int, TagInformation>();
            this.IsEdited = false;
        }

        /// <summary>
        /// 辞書を初期化
        /// </summary>
        /// <param name="source"></param>
        public void SetSource(IEnumerable<KeyValuePair<int, TagInformation>> source)
        {
            if (source == null)
            {
                this.RegisteredTags = new ConcurrentDictionary<int, TagInformation>();
            }
            else
            {
                this.RegisteredTags = new ConcurrentDictionary<int, TagInformation>(source);
            }
        }

        /// <summary>
        /// 全てのタグを取得
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<int, TagInformation>> GetAll()
        {
            return this.RegisteredTags.Where(x => !x.Value.IsIgnored).OrderBy(x => x.Value.Name);
        }

        /// <summary>
        /// Keyからタグを取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TagInformation GetTagValue(int key)
        {
            TagInformation result;
            if (this.RegisteredTags.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                var tag = new TagInformation() { Name = key.ToString(), Shortcut = "" };
                if (this.RegisteredTags.TryAdd(key, tag))
                {
                    tag.Id = key;
                    this.AddedSubject.OnNext(tag);
                }
                this.IsEdited = true;
                return tag;
            }
        }

        /// <summary>
        /// ショートカットからタグを取得
        /// </summary>
        /// <param name="shortcut"></param>
        /// <returns></returns>
        public KeyValuePair<int, TagInformation> GetTag(string shortcut)
        {
            var result = this.RegisteredTags
                .FirstOrNull(x => x.Value.Shortcut.Equals(shortcut, StringComparison.OrdinalIgnoreCase));

            if (result != null)
            {
                return result.Value;
            }

            var sc = shortcut.ToUpper();
            var res2 = this.RegisteredTags
                .Where(x => x.Value.Shortcut.Contains(sc))
                .OrderBy(x => x.Value.Shortcut.Length)
                .FirstOrNull();

            if (res2 != null)
            {
                res2.Value.Value.Shortcut = sc;
                return res2.Value;
            }

            return new KeyValuePair<int, TagInformation>(-1, null);
        }

        /// <summary>
        /// キーボードショートカットを設定
        /// </summary>
        /// <param name="tag"></param>
        public void SetShortcut(TagInformation tag)
        {
            if (!tag.Shortcut.IsNullOrWhiteSpace())
            {

                this.RegisteredTags
                    .Where(x =>
                    {
                        return x.Value != tag
                            && x.Value.Shortcut != null
                            && x.Value.Shortcut.Length > 0
                            && x.Value.Shortcut.Equals(tag.Shortcut,
                            StringComparison.OrdinalIgnoreCase);
                    })
                    .ForEach(x =>
                    {
                        x.Value.Shortcut = "_" + x.Value.Shortcut;
#if DEBUG
                        System.Windows.MessageBox.Show(x.Value.Shortcut);
#endif
                    });
            }

        }

        /// <summary>
        /// 新しいタグを登録
        /// </summary>
        /// <param name="newTag"></param>
        /// <returns></returns>
        public int SetTag(TagInformation newTag)
        {
            this.SetShortcut(newTag);

            var existingTag = this.RegisteredTags
                .FirstOrNull(x => x.Value.Name.Equals(newTag.Name));

            if (existingTag != null)
            {
                if (!newTag.Shortcut.IsNullOrWhiteSpace())
                {
                    existingTag.Value.Value.Shortcut = newTag.Shortcut;
                }
                return existingTag.Value.Key;
            }

            return this.AddOrReplace(newTag);
        }


        public void AddTags(IEnumerable<TagInformation> source)
        {
            var now = DateTimeOffset.Now;
            foreach (var group in source.GroupBy(x => x.Name))
            {
                if (group.Key.IsNullOrWhiteSpace())
                {
                    continue;
                }
                var key = this.SetTag(group.First());
                group.ForEach(x =>
                {
                    x.Id = key;
                    x.LastUsed = now;
                });
            }
        }


        private int AddOrReplace(TagInformation value)
        {
            var key = 1;
            TagInformation tag = null;
            while (this.RegisteredTags.TryGetValue(key, out tag))
            {
                if (tag != null && tag.IsIgnored)
                {
                    break;
                }
                tag = null;
                key++;
            }
            if (tag != null)
            {
                tag.CopyFrom(value);
                tag.Id = key;
                tag.IsIgnored = false;
            }
            else
            {
                var isAdded = this.RegisteredTags.TryAdd(key, value);

                if (isAdded)
                {
                    value.Id = key;
                    this.AddedSubject.OnNext(value);
                }
            }
            this.IsEdited = true;
            return key;
        }

    }
}
