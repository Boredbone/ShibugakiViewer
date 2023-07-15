using System;
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
        private Dictionary<int, TagInformation> registeredTags;
        public bool IsEdited { get; set; }

        private Subject<TagInformation> AddedSubject { get; }
        public IObservable<TagInformation> Added => this.AddedSubject.AsObservable();

        private object gate = new object();

        public TagDictionary()
        {
            this.AddedSubject = new Subject<TagInformation>().AddTo(this.Disposables);

            this.registeredTags = new Dictionary<int, TagInformation>();
            this.IsEdited = false;
        }

        /// <summary>
        /// 辞書を初期化
        /// </summary>
        /// <param name="source"></param>
        public void SetSource(IEnumerable<KeyValuePair<int, TagInformation>> source)
        {
            lock (this.gate)
            {
                if (source == null)
                {
                    this.registeredTags = new Dictionary<int, TagInformation>();
                }
                else
                {
                    this.registeredTags = source.ToDictionary(x => x.Key, x => x.Value);
                }
            }
        }

        /// <summary>
        /// 全てのタグを取得
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<int, TagInformation>[] GetAll()
        {
            lock (this.gate)
            {
                return this.registeredTags.Where(x => !x.Value.IsIgnored).OrderBy(x => x.Value.Name).ToArray();
            }
        }

        /// <summary>
        /// Keyからタグを取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TagInformation GetTagValue(int key)
        {
            TagInformation? tag;

            lock (this.gate)
            {
                if (this.registeredTags.TryGetValue(key, out tag))
                {
                    return tag;
                }

                tag = new TagInformation()
                {
                    Name = key.ToString(),
                    Shortcut = "",
                    Id = key,
                };

                this.registeredTags.Add(key, tag);
            }

            this.AddedSubject.OnNext(tag);

            this.IsEdited = true;
            return tag;
        }

        /// <summary>
        /// ショートカットからタグを取得
        /// </summary>
        /// <param name="shortcut"></param>
        /// <returns></returns>
        public KeyValuePair<int, TagInformation?> GetTag(string shortcut)
        {
            lock (this.gate)
            {
                var result = this.registeredTags
                    .FirstOrNull(x => x.Value.Shortcut.Equals(shortcut, StringComparison.OrdinalIgnoreCase));

                if (result != null)
                {
                    return new(result.Value.Key, result.Value.Value);
                }

                var sc = shortcut.ToUpper();
                var res2 = this.registeredTags
                    .Where(x => x.Value.Shortcut.Contains(sc))
                    .OrderBy(x => x.Value.Shortcut.Length)
                    .FirstOrNull();

                if (res2 != null)
                {
                    res2.Value.Value.Shortcut = sc;
                    return new(res2.Value.Key, res2.Value.Value);
                }
            }
            return new(-1, null);
        }

        /// <summary>
        /// キーボードショートカットを設定
        /// </summary>
        /// <param name="tag"></param>
        public void SetShortcut(TagInformation tag)
        {
            if (!tag.Shortcut.IsNullOrWhiteSpace())
            {
                TagInformation[] tags;

                lock (this.gate)
                {
                    tags = this.registeredTags
                        .Where(x =>
                        {
                            return x.Value != tag
                                && x.Value.Shortcut != null
                                && x.Value.Shortcut.Length > 0
                                && x.Value.Shortcut.Equals(tag.Shortcut,
                                StringComparison.OrdinalIgnoreCase);
                        })
                        .Select(x => x.Value)
                        .ToArray();
                }

                foreach (var x in tags)
                {
                    x.Shortcut = "_" + x.Shortcut;
#if DEBUG
                    System.Windows.MessageBox.Show(x.Shortcut);
#endif
                }
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

            KeyValuePair<int, TagInformation> existingTag;

            lock (this.gate)
            {
                existingTag = this.registeredTags
                    .FirstOrDefault(x => x.Value.Name.Equals(newTag.Name));
            }

            if (existingTag.Value != null)
            {
                if (!newTag.Shortcut.IsNullOrWhiteSpace())
                {
                    existingTag.Value.Shortcut = newTag.Shortcut;
                }
                return existingTag.Key;
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

            var added = false;

            lock (this.gate)
            {
                TagInformation tag = null;
                while (this.registeredTags.TryGetValue(key, out tag))
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
                    this.registeredTags.Add(key, value);
                    value.Id = key;
                    added = true;
                    //this.AddedSubject.OnNext(value);

                }
            }
            if (added)
            {
                this.AddedSubject.OnNext(value);
            }

            this.IsEdited = true;
            return key;
        }

    }
}
