﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using ShibugakiViewer.Views.Behaviors;
using ShibugakiViewer.Views.Controls;
using WpfTools;

namespace ShibugakiViewer.ViewModels
{
    public class ViewerPageViewModel : DisposableBase
    {
        private const double edgeTapThreshold = 0.2;
        private const double zoomFactorDisplayTime = 1500.0;

        public ReadOnlyReactiveProperty<Record> Record => this.client.ViewerDisplaying;
        public ReactiveProperty<double> ZoomFactor { get; }
        public ReactiveProperty<double> CurrentZoomFactorPercent { get; }
        public ReactiveProperty<double> DesiredZoomFactor { get; }
        public ReactiveProperty<double> DisplayZoomFactor { get; }
        public ReactiveProperty<Visibility> ZoomFactorVisibility { get; }
        public ReactiveProperty<bool> IsImageChanging { get; }

        public ReactiveProperty<bool> IsInHorizontalMirror { get; }
        public ReactiveProperty<bool> IsInVerticalMirror { get; }
        public ReactiveProperty<bool> IsAutoScalingEnabled { get; }

        public ReactiveProperty<long> DisplayIndex { get; }
        public ReadOnlyReactiveProperty<long> Length { get; }

        //public ReactiveCommand MouseExButtonLeftCommand { get; }
        //public ReactiveCommand MouseExButtonRightCommand { get; }
        public ReactiveCommand TapCommand { get; }
        public ReactiveCommand PointerMoveCommand { get; }
        public ReactiveCommand OpenPaneCommand { get; }
        public ReactiveCommand TogglePaneCommand { get; }
        public ReactiveCommand BackCommand { get; }

        public ReactiveCommand SlideshowCommand { get; }


        public ReactiveCommand HorizontalMirrorCommand { get; }
        public ReactiveCommand VerticalMirrorCommand { get; }
        public ReactiveCommand RotateCwCommand { get; }
        public ReactiveCommand RotateCcwCommand { get; }
        public ReactiveProperty<bool> IsTransformDialogEnabled { get; }

        public ReactiveProperty<double> ViewWidth { get; }
        public ReactiveProperty<double> ViewHeight { get; }

        public ReactiveProperty<bool> IsTopBarOpen { get; }
        public ReactiveProperty<bool> IsTopBarFixed { get; }
        public ReactiveProperty<Visibility> SplitViewButtonVisibility { get; }

        public ReactiveProperty<bool> IsScrollRequested { get; }
        public ReactiveProperty<int> Orientation { get; }

        public Func<int> CheckHorizontalScrollRequestFunction { get; }
        public Func<int> CheckVerticalScrollRequestFunction { get; }

        public Action<object, MouseWheelEventArgs> TopBarWheelAction { get; }


        private readonly ClientWindowViewModel parent;
        private readonly Client client;
        private readonly Library library;

        private bool CursorKeyForMove => this.parent.Core.CursorKeyBind == 1;// { get; set; } = false;

        private bool topBarOpenedByPointer = false;


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

            // image
            //this.Record = new ReactiveProperty<Record>();//.AddTo(this.Disposables);

            this.Length = client.Length.ToReadOnlyReactiveProperty().AddTo(this.Disposables);

            this.ZoomFactor = new ReactiveProperty<double>().AddTo(this.Disposables);
            this.DesiredZoomFactor = new ReactiveProperty<double>(0.0).AddTo(this.Disposables);

            this.CurrentZoomFactorPercent = this.ZoomFactor
                .Select(x => x * 100.0)
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.DisplayZoomFactor = this.CurrentZoomFactorPercent.ToReactiveProperty().AddTo(this.Disposables);

            this.DisplayZoomFactor.Where(x => x != this.CurrentZoomFactorPercent.Value)
                .Subscribe(x => this.DesiredZoomFactor.Value = x / 100.0).AddTo(this.Disposables);

            this.IsImageChanging = new ReactiveProperty<bool>().AddTo(this.Disposables);

            //拡大率表示ポップアップ
            this.ZoomFactorVisibility = this.ZoomFactor.Select(x => true)
                .Merge(this.ZoomFactor.Throttle(TimeSpan.FromMilliseconds(zoomFactorDisplayTime)).Select(x => false))
                .Where(x => !this.IsImageChanging.Value)
                .Merge(this.IsImageChanging.Where(x => x).Select(x => !x))
                .Select(x => VisibilityHelper.Set(x))
                .ToReactiveProperty(Visibility.Collapsed)
                .AddTo(this.Disposables);


            //client.StateChanged
            //    .Do(_ =>
            //    {
            //        var index = client.ViewerIndex.Value;
            //        var result = client.GetRecords(index, 1);
            //    })
            //    .Delay(TimeSpan.FromMilliseconds(100))
            //    .ObserveOnUIDispatcher()
            //    .Subscribe(_ => this.SetRecord(client))//this.Record.Value = ImageLibrary.File.Record.Empty)
            //    .AddTo(this.Disposables);

            //client.StateChanged
            //    .Subscribe(_ =>
            //    {
            //        var index = client.ViewerIndex.Value;
            //        var result = client.GetRecords(index, 1);
            //    })
            //    .AddTo(this.Disposables);

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

            //this.MouseExButtonLeftCommand = new ReactiveCommand()
            //    .WithSubscribe(_ =>
            //    {
            //        if (parent.Core.UseExtendedMouseButtonsToSwitchImage)
            //        {
            //            this.MoveLeft();
            //        }
            //    }, this.Disposables);
            //
            //this.MouseExButtonRightCommand = new ReactiveCommand()
            //    .WithSubscribe(_ =>
            //    {
            //        if (parent.Core.UseExtendedMouseButtonsToSwitchImage)
            //        {
            //            this.MoveRight();
            //        }
            //    }, this.Disposables);


            this.ViewWidth = new ReactiveProperty<double>().AddTo(this.Disposables);
            this.ViewWidth.Subscribe(x => client.ViewWidth = x).AddTo(this.Disposables);
            this.ViewHeight = new ReactiveProperty<double>().AddTo(this.Disposables);
            this.ViewHeight.Subscribe(x => client.ViewHeight = x).AddTo(this.Disposables);

            //this.LimitedDesiredOffset = new ReactiveProperty<Point>().AddTo(this.Disposables);
            //this.DesiredOffset = new ReactiveProperty<Point>().AddTo(this.Disposables);
            this.IsInHorizontalMirror = new ReactiveProperty<bool>(false).AddTo(this.Disposables);
            this.IsInVerticalMirror = new ReactiveProperty<bool>(false).AddTo(this.Disposables);
            this.IsAutoScalingEnabled = new ReactiveProperty<bool>(false).AddTo(this.Disposables);
            this.Orientation = new ReactiveProperty<int>().AddTo(this.Disposables);

            this.IsScrollRequested = new ReactiveProperty<bool>().AddTo(this.Disposables);

            this.IsTopBarOpen = new ReactiveProperty<bool>(false).AddTo(this.Disposables);
            //this.IsTopBarFixed = new ReactiveProperty<bool>(false).AddTo(this.Disposables);
            this.IsTopBarFixed = parent.Core
                .ToReactivePropertyAsSynchronized(x => x.IsViewerPageTopBarFixed)
                .AddTo(this.Disposables);
            if (this.IsTopBarFixed.Value)
            {
                this.IsTopBarOpen.Value = true;
            }

            this.OpenPaneCommand = new ReactiveCommand()
                .WithSubscribe(_ => parent.TogglePane(OptionPaneType.ItemInfo), this.Disposables);

            this.TogglePaneCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.TogglePane(), this.Disposables);

            this.TapCommand = new ReactiveCommand().AddTo(this.Disposables);

            var tapped = this.TapCommand.OfType<ViewerTapEventArgs>().Publish().RefCount();

            tapped
                .Subscribe(e =>
                {
                    if (e.HolizontalRate < edgeTapThreshold)
                    {
                        this.MoveLeft();
                    }
                    else if (e.HolizontalRate > (1.0 - edgeTapThreshold))
                    {
                        this.MoveRight();
                    }

                })
                .AddTo(this.Disposables);

            tapped
                .Where(_ => parent.Core.IsOpenNavigationWithSingleTapEnabled)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(e =>
                {
                    if (e.Count == 1
                        && parent.Core.IsOpenNavigationWithSingleTapEnabled
                        && e.HolizontalRate >= edgeTapThreshold
                        && e.HolizontalRate <= (1.0 - edgeTapThreshold))
                    {
                        this.IsTopBarOpen.Toggle();
                    }
                })
                .AddTo(this.Disposables);


            this.PointerMoveCommand = new ReactiveCommand()
                .WithSubscribe(x =>
                {

                    var y = ((Point)x).Y;

                    if (y < 100)
                    {

                        //if (!this.IsTopBarOpen.Value)

                        this.IsTopBarOpen.Value = true;
                        this.topBarOpenedByPointer = true;

                    }
                    else if (y > 150)
                    {

                        if (this.topBarOpenedByPointer && !this.IsTopBarFixed.Value)
                        {
                            this.IsTopBarOpen.Value = false;
                        }
                        this.topBarOpenedByPointer = false;
                    }
                }, this.Disposables);


            this.BackCommand = client.BackHistoryCount
                .Select(x => x > 0)
                .ToReactiveCommand()
                .WithSubscribe(_ => client.Back(), this.Disposables);

            this.SplitViewButtonVisibility = parent.IsPaneFixed
                .Select(x => VisibilityHelper.Set(!x))
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.SlideshowCommand = new ReactiveCommand()
                .WithSubscribe(_ => parent.StartSlideshow(), this.Disposables);


            // Transform Dialog

            this.IsTransformDialogEnabled = new ReactiveProperty<bool>(true).AddTo(this.Disposables);

            this.VerticalMirrorCommand = new ReactiveCommand()
                .WithSubscribe(_ => { this.VerticalMirror(); this.HidePopup(); }, this.Disposables);
            this.HorizontalMirrorCommand = new ReactiveCommand()
                .WithSubscribe(_ => { this.HorizontalMirror(); this.HidePopup(); }, this.Disposables);
            this.RotateCwCommand = new ReactiveCommand()
                .WithSubscribe(_ => { this.Rotate(1); this.HidePopup(); }, this.Disposables);
            this.RotateCcwCommand = new ReactiveCommand()
                .WithSubscribe(_ => { this.Rotate(-1); this.HidePopup(); }, this.Disposables);



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



            this.RegisterKeyReceiver(parent);
        }

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


            ////CtrlかShiftが押されている
            //var ctrlOrShift = modifier == ModifierKeys.Control || modifier == ModifierKeys.Shift;
            //
            //return (this.CursorKeyForMove ^ ctrlOrShift);
        }

        private void HidePopup()
        {
            this.IsTransformDialogEnabled.Value = false;
        }

        private void StartSlideShow()
        {

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


        private void SetTagWithShortCut(string code)
        {
            var res = this.library.Tags.GetTag(code).Value;

            if (res != null)
            {
                ToggleTag(res);
            }
        }

        private void ToggleTag(TagInformation tag)
            => this.Record.Value?.TagSet.Toggle(tag);

        private void SetTag(TagInformation tag)
            => this.Record.Value?.TagSet.Add(tag);

        private void RemoveTag(TagInformation tag)
            => this.Record.Value?.TagSet.Remove(tag);


        private void ZoomImage(double coefficient)
        {
            this.DesiredZoomFactor.Value = this.ZoomFactor.Value * coefficient;
        }

        private void StartAutoScaling()
        {
            //if (this.IsAutoScalingEnabled.Value)
            //{
            //    this.IsAutoScalingEnabled.Value = false;
            //}
            this.IsAutoScalingEnabled.Toggle();
            //this.IsAutoScalingEnabled.Value = false;
        }
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
            if (this.client.ViewerIndex.Value > 0)
            {
                this.client.ViewerIndex.Value--;
            }
            else
            {
                this.client.ViewerIndex.Value = this.Length.Value - 1;
            }
        }

        /// <summary>
        /// 次の画像
        /// </summary>
        private void MoveNext()
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
                parent.TogglePane(OptionPaneType.ItemInfo);
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
                pageFilter, isPreview: true);
            keyReceiver.Register(Key.End, (t, key) => this.client.ViewerIndex.Value = this.Length.Value - 1,
                pageFilter, isPreview: true);


            keyReceiver.Register(Key.Escape, (t, key) =>
            {
                this.IsTopBarOpen.Value = false;
            }, cursorFilter);


            keyReceiver.Register(new[] { Key.Space, Key.Enter },
                (t, key) => this.StartAutoScaling(), buttonFilter);

            //keyReceiver.Register(new[] { Key.Space, Key.Enter },
            //    (t, key) => this.StartAutoScaling(), cursorFilter);
            //keyReceiver.Register(k => (int)k == 190, (t, key) => this.StartAutoScaling(), pageFilter);

            keyReceiver.Register(new[] { Key.Decimal, Key.OemPeriod },
                (t, key) => this.StartAutoScaling(), cursorFilter);


            keyReceiver.Register(new[] { Key.Space, Key.Enter },
                (t, key) => this.StartSlideShow(), buttonFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(new[] { Key.Decimal, Key.OemPeriod }, (t, key) => this.StartSlideShow(),
                cursorFilter, modifier: ModifierKeys.Control);
            //keyReceiver.Register(k => (int)k == 190, (t, key) => this.StartSlideShow(),
            //    pageFilter, false, ModifierKeys.Control);



            keyReceiver.Register(new[] { Key.Add, Key.OemPlus },
                (t, key) => this.SetRating(true), cursorFilter);
            keyReceiver.Register(new[] { Key.Subtract, Key.OemMinus },
                (t, key) => this.SetRating(false), cursorFilter);
            //keyReceiver.Register(k => (int)k == 187, (t, key) => this.SetRating(true), pageFilter);
            //keyReceiver.Register(k => (int)k == 189, (t, key) => this.SetRating(false), pageFilter);

            keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                (t, key) => this.SetTagWithShortCut(((char)(key - Key.A + 'a')).ToString()),
                cursorFilter);


            keyReceiver.Register(Key.H, (t, key) => this.HorizontalMirror(),
                cursorFilter, modifier: ModifierKeys.Control);
            keyReceiver.Register(Key.V, (t, key) => this.VerticalMirror(),
                cursorFilter, modifier: ModifierKeys.Control);
            keyReceiver.Register(Key.E, (t, key) => this.Rotate(-1),
                cursorFilter, modifier: ModifierKeys.Control);
            keyReceiver.Register(Key.R, (t, key) => this.Rotate(1),
                cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.T, (t, key) =>
           {
               //this.tagSelectorFlyout.IsOpen = true;
           }, cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.P, (t, key) =>
            {
                if (!parent.IsPaneFixed.Value)
                {
                    parent.TogglePane(OptionPaneType.ItemInfo);
                }
            }, cursorFilter, modifier: ModifierKeys.Control);




            keyReceiver.Register(Key.S, (t, key) =>
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

            keyReceiver.Register(Key.Divide, async (t, key) =>
            {
                var index = await this.client.FindIndexFromDatabaseAsync(this.Record.Value);
                MessageBox.Show((index + 1).ToString());
            }, cursorFilter);



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

