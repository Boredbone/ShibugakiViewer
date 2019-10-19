using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Models.Utility
{

    public class KeyReceiver<T> : DisposableBase
    {
        private Subject<KeyActionContainer> KeySubject { get; }
        private bool acted;
        private List<PreFilter> PreFilters { get; }

        public int Mode { get; set; } = 0;

        private bool isFilterReset = false;

        public KeyReceiver()
        {
            this.KeySubject = new Subject<KeyActionContainer>().AddTo(this.Disposables);
            this.PreFilters = new List<PreFilter>();

            this.PreFilters.Add(new PreFilter(_ => true));

            this.acted = false;
        }

        public IDisposable Register
            (Key key, Action<T, Key> action,
            int preFilterIndex, int mode = 0, bool isPreview = false, ModifierKeys modifier = ModifierKeys.None)
        {
            return this.Register(x => x == key, action, preFilterIndex, mode, isPreview, modifier);
        }

        public IDisposable[] Register
            (IEnumerable<Key> keys, Action<T, Key> action,
            int preFilterIndex, int mode = 0, bool isPreview = false, ModifierKeys modifier = ModifierKeys.None)
        {
            return keys
                .Select(k => Register(k, action, preFilterIndex, mode, isPreview, modifier))
                .ToArray();
        }

        public IDisposable Register
            (Func<Key, bool> match, Action<T, Key> action,
            int preFilterIndex, int mode = 0, bool isPreview = false, ModifierKeys modifier = ModifierKeys.None)
        {
            if (preFilterIndex < 0)
            {
                preFilterIndex = 0;
            }

            return this.KeySubject
                    .Subscribe(x =>
                    {
                        if (x.IsPreview != isPreview
                            || this.Mode != mode
                            || preFilterIndex >= this.PreFilters.Count
                            || !this.PreFilters[preFilterIndex].Result)
                        {
                            return;
                        }

                        if (match(x.Key) && x.Modifier == modifier)
                        {
                            action(x.Target, x.Key);
                            this.acted = true;
                        }
                    })
                    .AddTo(this.Disposables);
        }




        public bool Check(T target, Key key, Key systemKey, object focusedControl, bool isPreview)
        {
            if (this.IsDisposed)
            {
                return false;
            }
            if (!this.isFilterReset)
            {
                this.ResetPreFilters();
            }
            this.acted = false;
            var modifier = Keyboard.Modifiers;

            if (key == Key.System && modifier == ModifierKeys.Alt)
            {
                key = systemKey;
            }

            var preFilterArgs = new PreFilterArgs(focusedControl)
            {
                IsPreview = isPreview,
            };

            this.PreFilters.ForEach(x => x.Check(preFilterArgs));


            this.KeySubject.OnNext(new KeyActionContainer()
            {
                Target = target,
                Key = key,
                Modifier = modifier,
                IsPreview = isPreview,
            });

            if (!isPreview)
            {
                this.isFilterReset = false;
            }

            return this.acted;
        }

        public bool IsKeyPressed(ModifierKeys key) => Keyboard.Modifiers.HasFlag(key);

        public int AddPreFilter(Func<PreFilterArgs, bool> filter)
        {
            this.PreFilters.Add(new PreFilter(filter));
            return this.PreFilters.Count - 1;
        }

        private void ResetPreFilters()
        {
            this.PreFilters.ForEach(x => x.Reset());
            this.isFilterReset = true;
        }

        private class KeyActionContainer
        {
            public T Target { get; set; }
            public Key Key { get; set; }
            public ModifierKeys Modifier { get; set; }
            public bool IsPreview { get; set; }
        }

        private class PreFilter
        {
            public Func<PreFilterArgs, bool> Filter { get; set; }
            public bool Result { get; private set; }

            public PreFilter(Func<PreFilterArgs, bool> filter)
            {
                this.Filter = filter;
            }
            public void Reset()
            {
                this.Result = false;
            }
            public void Check(PreFilterArgs args)
            {
                this.Result = this.Filter(args);
            }
        }

        public class PreFilterArgs
        {
            public object FocusedControl { get; private set; }
            public bool IsPreview { get; set; }

            public PreFilterArgs(object focused)
            {
                this.FocusedControl = focused;
            }
        }
    }
    
    class KeyBoardHelper
    {
        public static bool KeyToChar(Key key, out char c)
        {
            if (key >= Key.D0 && key <= Key.D9)
            {
                c = (char)(key - Key.D0 + '0');
                return true;
            }
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                c = (char)(key - Key.NumPad0 + '0');
                return true;
            }
            else if (key >= Key.A && key <= Key.Z)
            {
                c = (char)(key - Key.A + 'a');
                return true;
            }
            else
            {
                c = '\0';
                //System.Diagnostics.Debug.WriteLine($"key={key}");
                return false;
            }
        }
    }
}
