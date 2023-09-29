using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.Models.ImageViewer;
using ShibugakiViewer.Models.Utility;
using ShibugakiViewer.Views.Controls;
using WpfTools;

namespace ShibugakiViewer.ViewModels
{
    public class ViewerPageViewModel : DisposableBase
    {
        private const double edgeTapThreshold = 0.2;
        private const double zoomFactorDisplayTime = 1500.0;

        public ReadOnlyReactivePropertySlim<Record> Record => this.client.ViewerDisplaying;
        public ReactivePropertySlim<double> ZoomFactor { get; }
        public ReadOnlyReactivePropertySlim<double> CurrentZoomFactorPercent { get; }
        public ReactivePropertySlim<double> DesiredZoomFactor { get; }
        public ReactiveProperty<double> DisplayZoomFactor { get; }
        public ReadOnlyReactivePropertySlim<Visibility> ZoomFactorVisibility { get; }
        public ReactivePropertySlim<bool> IsImageChanging { get; }

        public ReactivePropertySlim<bool> IsInHorizontalMirror { get; }
        public ReactivePropertySlim<bool> IsInVerticalMirror { get; }
        public ReactivePropertySlim<bool> IsAutoScalingEnabled { get; }

        public ReactiveProperty<long> DisplayIndex { get; }
        public ReadOnlyReactivePropertySlim<long> Length { get; }

        public ReactivePropertySlim<bool> IsRandom { get; }
        private readonly RandomNumber randomNumber;
        
        public ReactiveCommandSlim TapCommand { get; }
        public ReactiveCommandSlim<object?> PointerMoveCommand { get; }
        public ReactiveCommandSlim<object?> OpenPaneCommand { get; }
        public ReactiveCommandSlim<object?> TogglePaneCommand { get; }
        public ReactiveCommandSlim<object?> BackCommand { get; }
                              
        public ReactiveCommandSlim<object?> MoveToGroupCommand { get; }
                              
                              
        public ReactiveCommandSlim<object?> HorizontalMirrorCommand { get; }
        public ReactiveCommandSlim<object?> VerticalMirrorCommand { get; }
        public ReactiveCommandSlim<object?> RotateCwCommand { get; }
        public ReactiveCommandSlim<object?> RotateCcwCommand { get; }
        public ReactivePropertySlim<bool> IsTransformDialogEnabled { get; }

        public ReactivePropertySlim<double> ViewWidth { get; }
        public ReactivePropertySlim<double> ViewHeight { get; }
        public ReadOnlyReactivePropertySlim<bool> UsePhysicalPixel { get; }
        public ReadOnlyReactivePropertySlim<int> ScalingMode { get; }

        public ReactivePropertySlim<bool> IsTopBarOpen { get; }
        public ReactivePropertySlim<bool> IsTopBarFixed { get; }
        public ReadOnlyReactivePropertySlim<Visibility> SplitViewButtonVisibility { get; }

        public ReactivePropertySlim<bool> IsScrollRequested { get; }
        public ReactivePropertySlim<int> Orientation { get; }

        public ReactivePropertySlim<bool> IsGifAnimationEnabled { get; }
        public ReactivePropertySlim<bool> IsExifOrientationDisabled { get; }

        public ReadOnlyReactivePropertySlim<bool> IsFill { get; }
        public ReadOnlyReactivePropertySlim<bool> IsZoomoutOnly { get; }

        public ReactiveProperty<bool> IsSlideshowPlaying { get; }
        private Subject<Unit> SlideshowSubject { get; }

        public Func<int> CheckHorizontalScrollRequestFunction { get; }
        public Func<int> CheckVerticalScrollRequestFunction { get; }

        public Action<object, MouseWheelEventArgs> TopBarWheelAction { get; }

        public Action<object, MouseEventArgs> PointerMoveAction { get; }
        public Action<object, MouseEventArgs> PointerLeaveAction { get; }
        public Action<object, MouseEventArgs> PointerDownAction { get; }
        public Action<object, MouseEventArgs> PointerUpAction { get; }
        
        public ReactivePropertySlim<bool> IsLeftButtonEnter { get; }
        public ReactivePropertySlim<bool> IsRightButtonEnter { get; }
        public ReactivePropertySlim<bool> IsLeftButtonPressed { get; }
        public ReactivePropertySlim<bool> IsRightButtonPressed { get; }

        public ReadOnlyReactivePropertySlim<Visibility> MoveButtonVisibility { get; }

        private readonly ClientWindowViewModel parent;
        private readonly Client client;
        private readonly Library library;

        private bool CursorKeyForMove => this.parent.Core.CursorKeyBind == 1;

        private bool topBarOpenedByPointer = false;

        private DateTime lastTopbarChanged;


        public ViewerPageViewModel(ClientWindowViewModel parent)
        {
            this.parent = parent;
            this.client = parent.Client;

            this.library = parent.Library;

            this.DisplayIndex = client.ViewerIndex
                .Select(x => x + 1)
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.DisplayIndex.Subscribe(x =>
            {
                client.ViewerIndex.Value = x - 1;
            })
            .AddTo(this.Disposables);

            this.Length = client.Length.ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);

            this.randomNumber = new RandomNumber();
            this.IsRandom = parent.Core
                .ToReactivePropertySlimAsSynchronized(x => x.IsSlideshowRandom).AddTo(this.Disposables);
            
            this.IsRandom.Select(_ => Unit.Default)
                .Merge(this.Length.Select(_ => Unit.Default))
                .Subscribe(x =>
                {
                    if (this.IsRandom.Value && this.Length.Value > 0)
                    {
                        this.randomNumber.Length = (int)this.Length.Value;
                        this.randomNumber.Clear();
                        this.client.PrepareNext(this.randomNumber.GetNext());
                    }
                })
                .AddTo(this.Disposables);

            this.client.ViewerCacheClearedTrigger
                .Subscribe(x =>
                {
                    if (this.IsRandom.Value && this.Length.Value > 0)
                    {
                        this.client.PrepareNext(this.randomNumber.GetNext());
                    }
                })
                .AddTo(this.Disposables);
            


            this.ZoomFactor = new ReactivePropertySlim<double>().AddTo(this.Disposables);
            this.DesiredZoomFactor = new ReactivePropertySlim<double>(0.0).AddTo(this.Disposables);

            this.CurrentZoomFactorPercent = this.ZoomFactor
                .Select(x => x * 100.0)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.DisplayZoomFactor = this.CurrentZoomFactorPercent
                .ToReactiveProperty().AddTo(this.Disposables);

            this.DisplayZoomFactor.Where(x => x != this.CurrentZoomFactorPercent.Value)
                .Subscribe(x => this.ChangeZoomFactor(x / 100.0))
                .AddTo(this.Disposables);

            this.UsePhysicalPixel = parent.Core
                .ObserveProperty(x => x.UseLogicalPixel)
                .Select(x => !x)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.ScalingMode = parent.Core
                .ObserveProperty(x => x.ScalingMode)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IsImageChanging = new ReactivePropertySlim<bool>().AddTo(this.Disposables);

            //拡大率表示ポップアップ
            this.ZoomFactorVisibility = this.ZoomFactor.Select(x => true)
                .Merge(this.ZoomFactor.Throttle(TimeSpan.FromMilliseconds(zoomFactorDisplayTime)).Select(x => false))
                .Where(x => !this.IsImageChanging.Value)
                .Merge(this.IsImageChanging.Where(x => x).Select(x => !x))
                .Select(x => VisibilityHelper.Set(x))
                .ToReadOnlyReactivePropertySlim(Visibility.Collapsed)
                .AddTo(this.Disposables);

            this.IsGifAnimationEnabled = parent.Core
                .ToReactivePropertySlimAsSynchronized(x => x.IsAnimatedGifEnabled)
                .AddTo(this.Disposables);

            this.IsExifOrientationDisabled = parent.Core
                .ToReactivePropertySlimAsSynchronized(x => x.IsExifOrientationDisabled)
                .AddTo(this.Disposables);


            this.IsFill = parent.Core
                .ObserveProperty(x => x.IsSlideshowResizeToFill)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IsZoomoutOnly = parent.Core
                .ObserveProperty(x => x.IsSlideshowResizingAlways)
                .Select(x => !x)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            parent.MouseExButtonPressed
                .Subscribe(x =>
                {
                    if (parent.Core.UseExtendedMouseButtonsToSwitchImage
                        && client.SelectedPage.Value == PageType.Viewer)
                    {
                        if (x)
                        {
                            this.MoveRight();
                        }
                        else
                        {
                            this.MoveLeft();
                        }
                    }
                })
                .AddTo(this.Disposables);
            


            this.ViewWidth = new ReactivePropertySlim<double>().AddTo(this.Disposables);
            this.ViewWidth.Subscribe(x => client.ViewWidth = x).AddTo(this.Disposables);
            this.ViewHeight = new ReactivePropertySlim<double>().AddTo(this.Disposables);
            this.ViewHeight.Subscribe(x => client.ViewHeight = x).AddTo(this.Disposables);
            
            this.IsInHorizontalMirror = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsInVerticalMirror = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsAutoScalingEnabled = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.Orientation = new ReactivePropertySlim<int>().AddTo(this.Disposables);

            this.IsScrollRequested = new ReactivePropertySlim<bool>().AddTo(this.Disposables);

            this.IsTopBarOpen = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsTopBarFixed = parent.Core
                .ToReactivePropertySlimAsSynchronized(x => x.IsViewerPageTopBarFixed)
                .AddTo(this.Disposables);
            if (this.IsTopBarFixed.Value)
            {
                this.IsTopBarOpen.Value = true;
            }

            this.OpenPaneCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => parent.TogglePane(OptionPaneType.ItemInfo))
                .AddTo(this.Disposables);

            this.TogglePaneCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => this.TogglePane())
                .AddTo(this.Disposables);

            this.TapCommand = new ReactiveCommandSlim().AddTo(this.Disposables);

            var tapped = this.TapCommand.OfType<ViewerTapEventArgs>()
                .Publish().RefCount();

            tapped
                .Subscribe(e =>
                {
                    if (e.IsLongTouch)
                    {
                        this.TogglePane();
                    }
                    else
                    {
                        if (e.HolizontalRate < edgeTapThreshold)
                        {
                            this.MoveLeft();
                        }
                        else if (e.HolizontalRate > (1.0 - edgeTapThreshold))
                        {
                            this.MoveRight();
                        }
                    }
                })
                .AddTo(this.Disposables);

            tapped
                .Where(x => parent.Core.IsOpenNavigationWithSingleTapEnabled && !x.IsLongTouch)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(e =>
                {
                    if (e.Count == 1
                        && parent.Core.IsOpenNavigationWithSingleTapEnabled
                        && e.HolizontalRate >= edgeTapThreshold
                        && e.HolizontalRate <= (1.0 - edgeTapThreshold)
                        && (!this.IsTopBarFixed.Value || !this.IsTopBarOpen.Value))
                    {
                        if (DateTime.Now - lastTopbarChanged > TimeSpan.FromMilliseconds(600))
                        {
                            this.IsTopBarOpen.Toggle();
                        }
                    }
                })
                .AddTo(this.Disposables);


            this.PointerMoveCommand = new ReactiveCommandSlim()
                .WithSubscribe(x =>
                {
                    var y = ((Point)x).Y;

                    if (y < 100)
                    {
                        this.IsTopBarOpen.Value = true;
                        this.topBarOpenedByPointer = true;
                        lastTopbarChanged = DateTime.Now;
                    }
                    else if (y > 150)
                    {
                        if (this.topBarOpenedByPointer && !this.IsTopBarFixed.Value)
                        {
                            this.IsTopBarOpen.Value = false;
                            lastTopbarChanged = DateTime.Now;
                        }
                        this.topBarOpenedByPointer = false;
                    }
                })
                .AddTo(this.Disposables);


            this.BackCommand = client.BackHistoryCount
                .Select(x => x > 0)
                .ToReactiveCommandSlim()
                .WithSubscribe(_ => client.Back())
                .AddTo(this.Disposables);

            this.SplitViewButtonVisibility = parent.IsPaneFixed
                .Select(x => VisibilityHelper.Set(!x))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IsSlideshowPlaying = this.client.SelectedPage
                .Select(_ => false)
                .ToReactiveProperty(false)
                .AddTo(this.Disposables);

            this.IsSlideshowPlaying.Subscribe(x =>
            {
                if (x)
                {
                    this.StartSlideShow();
                }
                else
                {
                    this.parent.IsFullScreen.Value = false;
                }
            })
            .AddTo(this.Disposables);

            this.SlideshowSubject = new Subject<Unit>().AddTo(this.Disposables);

            var slideshowSubscription = new SerialDisposable().AddTo(this.Disposables);

            this.parent.Core.ObserveProperty(x => x.SlideshowFlipTimeMillisec)
                .Subscribe(x =>
                {
                    slideshowSubscription.Disposable = this.SlideshowSubject
                        .Throttle(TimeSpan.FromMilliseconds(x))
                        .Where(_ => this.IsSlideshowPlaying.Value)
                        .ObserveOnUIDispatcher()
                        .Subscribe(y =>
                        {
                            if (this.IsSlideshowPlaying.Value)
                            {
                                this.MoveNext();
                            }
                        });

                    if (this.IsSlideshowPlaying.Value)
                    {
                        this.SlideshowSubject.OnNext(Unit.Default);
                    }

                })
                .AddTo(this.Disposables);



            // Transform Dialog

            this.IsTransformDialogEnabled = new ReactivePropertySlim<bool>(true).AddTo(this.Disposables);

            this.VerticalMirrorCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => { this.VerticalMirror(); this.HidePopup(); })
                .AddTo(this.Disposables);
            this.HorizontalMirrorCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => { this.HorizontalMirror(); this.HidePopup(); })
                .AddTo(this.Disposables);
            this.RotateCwCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => { this.Rotate(1); this.HidePopup(); })
                .AddTo(this.Disposables);
            this.RotateCcwCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => { this.Rotate(-1); this.HidePopup(); })
                .AddTo(this.Disposables);

            this.MoveToGroupCommand = this.Record
                .Select(x => x?.IsGroup ?? false)
                .ToReactiveCommandSlim()
                .WithSubscribe(_ => this.client.DisplayGroup(0))
                .AddTo(this.Disposables);

            this.CheckHorizontalScrollRequestFunction = () =>
            {
                var k = this.CheckKeyboardScrollModifier();

                return (Keyboard.IsKeyDown(Key.Right)) ? k
                    : (Keyboard.IsKeyDown(Key.Left)) ? -k
                    : 0;
            };

            this.CheckVerticalScrollRequestFunction = () =>
            {
                var k = this.CheckKeyboardScrollModifier();

                return (Keyboard.IsKeyDown(Key.Down)) ? k
                    : (Keyboard.IsKeyDown(Key.Up)) ? -k
                    : 0;
            };

            this.TopBarWheelAction = (o, e) =>
            {
                if (e.Delta > 0)
                {
                    this.MovePrev();
                }
                else if (e.Delta < 0)
                {
                    this.MoveNext();
                }
            };


            //画像変更ボタン
            this.MoveButtonVisibility = parent.Core
                .ObserveProperty(x => x.IsViewerMoveButtonDisabled)
                .Select(x => VisibilityHelper.Set(!x))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            
            this.IsLeftButtonEnter = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsRightButtonEnter = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsLeftButtonPressed = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsRightButtonPressed = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);

            this.PointerMoveAction = (o, e) =>
            {
                if (this.IsDisposed)
                {
                    return;
                }
                var element = o as FrameworkElement;
                if (element == null)
                {
                    return;
                }
                var point = (Vector)e.GetPosition(element) / element.ActualWidth;

                if (point.X < edgeTapThreshold)
                {
                    this.IsLeftButtonEnter.Value = true;
                }
                else
                {
                    this.IsLeftButtonEnter.Value = false;
                    this.IsLeftButtonPressed.Value = false;
                }
                if (point.X > (1.0 - edgeTapThreshold))
                {
                    this.IsRightButtonEnter.Value = true;
                }
                else
                {
                    this.IsRightButtonEnter.Value = false;
                    this.IsRightButtonPressed.Value = false;
                }
            };
            this.PointerLeaveAction = (o, e) =>
            {
                if (this.IsDisposed)
                {
                    return;
                }
                this.IsLeftButtonEnter.Value = false;
                this.IsRightButtonEnter.Value = false;
                this.IsLeftButtonPressed.Value = false;
                this.IsRightButtonPressed.Value = false;
            };
            this.PointerDownAction = (o, e) =>
            {
                if (this.IsDisposed)
                {
                    return;
                }
                var element = o as FrameworkElement;
                if (element == null)
                {
                    return;
                }
                var point = (Vector)e.GetPosition(element) / element.ActualWidth;

                this.IsLeftButtonPressed.Value = point.X < edgeTapThreshold;
                this.IsRightButtonPressed.Value = point.X > (1.0 - edgeTapThreshold);
            };
            this.PointerUpAction = (o, e) =>
            {
                if (this.IsDisposed)
                {
                    return;
                }
                this.IsLeftButtonPressed.Value = false;
                this.IsRightButtonPressed.Value = false;
            };

            //キーボード操作
            this.RegisterKeyReceiver(parent);
        }

        /// <summary>
        /// カーソルキーによる画像移動の速度
        /// </summary>
        /// <returns></returns>
        private int CheckKeyboardScrollModifier()
        {
            var modifier = Keyboard.Modifiers;

            if (this.CursorKeyForMove)
            {
                if (modifier == ModifierKeys.None)
                {
                    return 2;
                }
                return 0;
            }

            if (modifier == ModifierKeys.Control)
            {
                return 1;
            }
            if (modifier == ModifierKeys.Shift)
            {
                return 3;
            }

            return 0;
        }

        private void HidePopup()
        {
            this.IsTransformDialogEnabled.Value = false;
        }

        private void StartSlideShow()
        {
            if (this.parent.Core.IsSlideshowFullScreen)
            {
                this.parent.IsFullScreen.Value = true;
            }

            this.IsSlideshowPlaying.Value = true;
            this.SlideshowSubject.OnNext(Unit.Default);
        }

        private void SetRating(bool up)
        {
            var item = this.Record.Value;
            if (item == null)
            {
                return;
            }

            //Rating(評価,0->0,1->1,2->25,3->50,4->75,5->99)
            var rating = item.GetRate();

            if (up)
            {
                rating++;
            }
            else
            {
                rating--;
            }
            item.SetRate(rating);
        }


        private void ToggleTagWithShortCut(string code)
        {
            var tag = this.library.Tags.GetTag(code).Value;
            var record = this.Record.Value;
            if (record is not null && tag is not null)
            {
                record.TagSet.Toggle(tag);
            }
        }
        private void SetTagWithShortCut(string code)
        {
            var tag = this.library.Tags.GetTag(code).Value;
            var record = this.Record.Value;
            if (record is not null && tag is not null)
            {
                record.TagSet.TryAdd(tag);
            }
        }
        private void RemoveTagWithShortCut(string code)
        {
            var tag = this.library.Tags.GetTag(code).Value;
            var record = this.Record.Value;
            if (record is not null && tag is not null)
            {
                record.TagSet.Remove(tag);
            }
        }

        private void ToggleTag(TagInformation tag)
            => this.Record.Value?.TagSet.Toggle(tag);

        private void SetTag(TagInformation tag)
            => this.Record.Value?.TagSet.Add(tag);

        private void RemoveTag(TagInformation tag)
            => this.Record.Value?.TagSet.Remove(tag);


        private void ZoomImage(double coefficient)
            => this.ChangeZoomFactor(this.ZoomFactor.Value * coefficient);

        private void ChangeZoomFactor(double value)
        {
            if (this.DesiredZoomFactor.Value == value)
            {
                value += 0.00001;
            }
            this.DesiredZoomFactor.Value = value;
        }

        private void StartAutoScaling()
            => this.IsAutoScalingEnabled.Toggle();
        
        private void HorizontalMirror()
            => this.IsInHorizontalMirror.Toggle();


        private void VerticalMirror()
            => this.IsInVerticalMirror.Toggle();


        private void Rotate(int num)
        {
            this.Orientation.Value += num * 90;
        }

        /// <summary>
        /// 左へ移動
        /// </summary>
        private void MoveLeft()
        {
            if (this.IsReversed())
            {
                this.MoveNext();
            }
            else
            {
                this.MovePrev();
            }
        }

        /// <summary>
        /// 右へ移動
        /// </summary>
        private void MoveRight()
        {
            if (this.IsReversed())
            {
                this.MovePrev();
            }
            else
            {
                this.MoveNext();
            }
        }

        /// <summary>
        /// ページめくり方向
        /// </summary>
        /// <returns></returns>
        private bool IsReversed()
        {
            if (this.client.IsGroupMode.Value && this.client.FeaturedGroup != null)
            {
                var direction = this.client.FeaturedGroup.GetFlipDirection();

                if (direction == FlipDirection.LeftToRight)
                {
                    return false;
                }
                if (direction == FlipDirection.RightToLeft)
                {
                    return true;
                }
            }

            return this.parent.Core.IsFlipReversed;
        }

        /// <summary>
        /// 前の画像
        /// </summary>
        private void MovePrev()
        {
            if (this.IsRandom.Value)
            {
                this.client.ViewerIndex.Value = this.randomNumber.MovePrev();
            }
            else
            {
                if (this.client.ViewerIndex.Value > 0)
                {
                    this.client.ViewerIndex.Value--;
                }
                else
                {
                    this.client.ViewerIndex.Value = this.Length.Value - 1;
                }
            }

            if (this.IsSlideshowPlaying.Value)
            {
                this.SlideshowSubject.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// 次の画像
        /// </summary>
        private void MoveNext()
        {
            if (this.IsRandom.Value)
            {
                this.randomNumber.ReplaceIfDifferent((int)this.client.ViewerIndex.Value);
                this.client.ViewerIndex.Value = this.randomNumber.MoveNext();
                this.client.PrepareNext(this.randomNumber.GetNext());
            }
            else
            {
                if (this.client.ViewerIndex.Value < this.Length.Value - 1)
                {
                    this.client.ViewerIndex.Value++;
                }
                else
                {
                    this.client.ViewerIndex.Value = 0;
                }
            }

            if (this.IsSlideshowPlaying.Value)
            {
                this.SlideshowSubject.OnNext(Unit.Default);
            }
        }

        private void TogglePane()
        {
            if (!parent.IsPaneOpen.Value || parent.IsPaneFixed.Value)
            {
                if (this.IsTopBarOpen.Value)
                {
                    if (!this.IsTopBarFixed.Value)
                    {
                        this.IsTopBarOpen.Value = false;
                    }
                }
                else
                {
                    this.IsTopBarOpen.Value = true;
                }
            }
            if (!parent.IsPaneFixed.Value)
            {
                parent.ToggleInformationPane();
            }
        }

        /// <summary>
        /// キーボード操作を登録
        /// </summary>
        /// <param name="keyReceiver"></param>
        /// <param name="client"></param>
        private void RegisterKeyReceiver(ClientWindowViewModel parent)
        {
            var keyReceiver = parent.KeyReceiver;

            var pageFilter = keyReceiver.AddPreFilter(x =>
                (client.SelectedPage.Value == PageType.Viewer));

            var cursorFilter = keyReceiver.AddPreFilter(x =>
            {
                if (client.SelectedPage.Value != PageType.Viewer)
                {
                    return false;
                }
                return !(x.FocusedControl is TextBox);
            });

            var buttonFilter = keyReceiver.AddPreFilter(x =>
            {
                if (client.SelectedPage.Value != PageType.Viewer)
                {
                    return false;
                }
                return !(x.FocusedControl is ButtonBase) && !(x.FocusedControl is TextBox);
            });


            //var scrollDelta = 100;
            var zoom = 1.2;
            var shiftOrControlKey = new[] { ModifierKeys.Shift, ModifierKeys.Control };

            shiftOrControlKey.ForEach(m =>
            {
                keyReceiver.Register(
                    new[] { Key.Up, Key.Down, Key.Right, Key.Left },
                    (t, key) => this.ReceiveCursorKey(key, !this.CursorKeyForMove, zoom),
                    cursorFilter, isPreview: true, modifier: m);
            });


            keyReceiver.Register(
                new Key[] { Key.Up, Key.Down, Key.Right, Key.Left },
                (t, key) => this.ReceiveCursorKey(key, this.CursorKeyForMove, zoom),
                cursorFilter, isPreview: true);




            keyReceiver.Register(new[] { Key.Add, Key.OemPlus },
                (t, key) => this.ZoomImage(zoom), cursorFilter, modifier: ModifierKeys.Control);
            //keyReceiver.Register(k => (int)k == 187, (t, key) => this.ZoomImage(zoom), 
            //    pageFilter, false, ModifierKeys.Control);

            keyReceiver.Register(new[] { Key.Subtract, Key.OemMinus },
                (t, key) => this.ZoomImage(1.0 / zoom), cursorFilter, modifier: ModifierKeys.Control);
            //keyReceiver.Register(k => (int)k == 189, (t, key) => this.ZoomImage(1.0 / zoom), 
            //    pageFilter, false, ModifierKeys.Control);



            keyReceiver.Register(Key.PageUp, (t, key) => this.MovePrev(), pageFilter, isPreview: true);
            keyReceiver.Register(Key.PageDown, (t, key) => this.MoveNext(), pageFilter, isPreview: true);


            keyReceiver.Register(Key.Home, (t, key) => this.client.ViewerIndex.Value = 0,
                cursorFilter, isPreview: true);
            keyReceiver.Register(Key.End, (t, key) => this.client.ViewerIndex.Value = this.Length.Value - 1,
                cursorFilter, isPreview: true);


            keyReceiver.Register(Key.Escape, (t, key) =>
            {
                if (!this.IsTopBarOpen.Value && !this.parent.IsPaneOpen.Value)
                {
                    this.parent.IsFullScreen.Value = false;
                }
                else
                {
                    this.IsTopBarOpen.Value = false;
                }
            }, cursorFilter);


            keyReceiver.Register(new[] { Key.Space, Key.Enter },
                (t, key) => this.StartAutoScaling(), buttonFilter);
            //keyReceiver.Register(k => (int)k == 190, (t, key) => this.StartAutoScaling(), pageFilter);

            keyReceiver.Register(new[] { Key.Decimal, Key.OemPeriod },
                (t, key) => this.StartAutoScaling(), cursorFilter);


            keyReceiver.Register(new[] { Key.Space, Key.Enter },
                (t, key) => this.IsSlideshowPlaying.Toggle(), buttonFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(new[] { Key.Decimal, Key.OemPeriod },
                (t, key) => this.IsSlideshowPlaying.Toggle(), cursorFilter, modifier: ModifierKeys.Control);
            //keyReceiver.Register(k => (int)k == 190, (t, key) => this.StartSlideShow(),
            //    pageFilter, false, ModifierKeys.Control);



            keyReceiver.Register(new[] { Key.Add, Key.OemPlus },
                (t, key) => this.SetRating(true), cursorFilter);
            keyReceiver.Register(new[] { Key.Subtract, Key.OemMinus },
                (t, key) => this.SetRating(false), cursorFilter);
            //keyReceiver.Register(k => (int)k == 187, (t, key) => this.SetRating(true), pageFilter);
            //keyReceiver.Register(k => (int)k == 189, (t, key) => this.SetRating(false), pageFilter);

            if (ApplicationCore.ExpandTagShortcut)
            {
                keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                    (t, key) => this.SetTagWithShortCut(((char)(key - Key.A + 'a')).ToString()),
                    cursorFilter);
                keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                    (t, key) => this.SetTagWithShortCut("^" + ((char)(key - Key.A + 'a')).ToString()),
                    cursorFilter, modifier: ModifierKeys.Shift);

                keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                    (t, key) => this.RemoveTagWithShortCut(((char)(key - Key.A + 'a')).ToString()),
                    cursorFilter, modifier: ModifierKeys.Alt);
                keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                    (t, key) => this.RemoveTagWithShortCut("^" + ((char)(key - Key.A + 'a')).ToString()),
                    cursorFilter, modifier: ModifierKeys.Control | ModifierKeys.Shift);
            }
            else
            {
                keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                    (t, key) => this.ToggleTagWithShortCut(((char)(key - Key.A + 'a')).ToString()),
                    cursorFilter);
            }

            keyReceiver.Register(Key.H, (t, key) => this.HorizontalMirror(),
                cursorFilter, modifier: ModifierKeys.Control);
            keyReceiver.Register(Key.V, (t, key) => this.VerticalMirror(),
                cursorFilter, modifier: ModifierKeys.Control);
            keyReceiver.Register(Key.E, (t, key) => this.Rotate(-1),
                cursorFilter, modifier: ModifierKeys.Control);
            keyReceiver.Register(Key.R, (t, key) => this.Rotate(1),
                cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.T, (t, key) => this.parent.ShowTagSelector(null),
                cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.P, (t, key) =>
            {
                if (!parent.IsPaneFixed.Value)
                {
                    parent.TogglePane(OptionPaneType.ItemInfo);
                }
            }, cursorFilter, modifier: ModifierKeys.Control);



            keyReceiver.Register(Key.S, (t, key) => this.IsRandom.Toggle(),
                cursorFilter, modifier: ModifierKeys.Control);


            keyReceiver.Register(Key.L, (t, key) =>
            {
                SharePathOperation.OpenExplorer(this.Record.Value?.FullPath);
            }, cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.C, (t, key) =>
            {
                SharePathOperation.CopyPath(this.Record.Value?.FullPath);
            }, cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.Apps, (t, key) => this.TogglePane(), cursorFilter);


            keyReceiver.Register(Key.G, (t, key) => this.client.DisplayGroup(0),
                cursorFilter, modifier: ModifierKeys.Control);


            keyReceiver.Register(Key.Delete,
                async (t, key) => await this.client.DeleteDisplayingFile(false),
                cursorFilter);

            keyReceiver.Register(Key.Delete,
                async (t, key) => await this.client.DeleteDisplayingFile(true),
                cursorFilter, modifier: ModifierKeys.Control);



            keyReceiver.Register(Key.F5, (t, key) => this.client.Refresh(), cursorFilter);

            keyReceiver.Register(Key.F11, (t, key) => this.parent.IsFullScreen.Toggle(), cursorFilter);

#if DEBUG
            keyReceiver.Register(Key.Divide, async (t, key) =>
            {
                var index = await this.client.FindIndexFromDatabaseAsync(this.Record.Value);
                MessageBox.Show((index + 1).ToString());
            }, cursorFilter);
#endif



        }

        private void ReceiveCursorKey(Key direction, bool move, double zoom)
        {
            if (move)
            {
                this.IsScrollRequested.Value = true;
            }
            else
            {
                switch (direction)
                {
                    case Key.Up:
                        this.ZoomImage(zoom);
                        return;
                    case Key.Down:
                        this.ZoomImage(1.0 / zoom);
                        return;
                    case Key.Left:
                        this.MoveLeft();
                        return;
                    case Key.Right:
                        this.MoveRight();
                        return;
                }
            }
        }
    }
}

