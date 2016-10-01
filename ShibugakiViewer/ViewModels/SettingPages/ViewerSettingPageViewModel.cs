using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.Views.Windows;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class ViewerSettingPageViewModel : DisposableBase
    {

        public ReactiveProperty<int> CursorKeyBind { get; }
        [Range(64, 512)]
        public ReactiveProperty<int> ThumbnailSize { get; }
        public ReactiveProperty<bool> IsAutoInformationPaneEnabled { get; }

        public ReactiveProperty<bool> IsDarkTheme { get; }

        public ReactiveProperty<bool> IsGroupingEnabled { get; }
        public ReactiveProperty<bool> RefreshLibraryCompletely { get; }

        public ReactiveProperty<bool> IsFlipReversed { get; }
        public ReactiveProperty<bool> IsAnimatedGifEnabled { get; }
        public ReactiveProperty<bool> IsOpenNavigationWithSingleTapEnabled { get; }
        public ReactiveProperty<bool> UseExtendedMouseButtonsToSwitchImage { get; }
        public ReactiveProperty<bool> RefreshLibraryOnLaunched { get; }
        public ReactiveProperty<bool> IsLibraryRefreshStatusVisible { get; }
        public ReactiveProperty<bool> IsFolderUpdatedNotificationVisible { get; }
        public ReactiveProperty<bool> IsViewerMoveButtonDisabled { get; }

        public ReactiveProperty<bool> IsFill { get; }
        public ReactiveProperty<bool> IsResizingAlways { get; }
        public ReactiveProperty<bool> IsSlideshowFullScreen { get; }
        public ReactiveProperty<bool> UseLogicalPixel { get; }

        public ReactiveProperty<double> AnimationTimeSec { get; }
        public ReactiveProperty<int> FlipTimeSec { get; }

        public ReactiveProperty<Color> BackColor { get; }

        public ReactiveProperty<bool> IsVersionCheckEnabled { get; }
        public ReactiveProperty<int> ScalingMode { get; }


        public ViewerSettingPageViewModel()
        {
            var core = ((App)Application.Current).Core;
            var library = core.Library;

            //this.Text = new ReactiveProperty<string>().AddTo(this.Disposables);

            this.ThumbnailSize = core
                .ToReactivePropertyAsSynchronized(x => x.ThumbNailSize, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => this.ThumbnailSize)
                .AddTo(this.Disposables);

            this.IsAutoInformationPaneEnabled = core
                .ToReactivePropertyAsSynchronized(x => x.IsAutoInformationPaneDisabled, x => !x, x => !x)
                .AddTo(this.Disposables);

            this.IsDarkTheme = core
                .ToReactivePropertyAsSynchronized(x => x.IsDarkTheme)
                .AddTo(this.Disposables);

            this.BackColor = core
                .ToReactivePropertyAsSynchronized(x => x.BackgroundColor)
                .AddTo(this.Disposables);


            this.IsGroupingEnabled = library
                .ToReactivePropertyAsSynchronized(x => x.IsGroupingEnabled)
                .AddTo(this.Disposables);

            this.RefreshLibraryCompletely = library
                .ToReactivePropertyAsSynchronized(x => x.RefreshLibraryCompletely)
                .AddTo(this.Disposables);

            this.IsFlipReversed = core
                .ToReactivePropertyAsSynchronized(x => x.IsFlipReversed)
                .AddTo(this.Disposables);
            this.IsAnimatedGifEnabled = core
                .ToReactivePropertyAsSynchronized(x => x.IsAnimatedGifEnabled)
                .AddTo(this.Disposables);
            this.IsOpenNavigationWithSingleTapEnabled = core
                .ToReactivePropertyAsSynchronized(x => x.IsOpenNavigationWithSingleTapEnabled)
                .AddTo(this.Disposables);
            this.UseExtendedMouseButtonsToSwitchImage = core
                .ToReactivePropertyAsSynchronized(x => x.UseExtendedMouseButtonsToSwitchImage)
                .AddTo(this.Disposables);
            this.RefreshLibraryOnLaunched = core
                .ToReactivePropertyAsSynchronized(x => x.RefreshLibraryOnLaunched)
                .AddTo(this.Disposables);
            this.IsLibraryRefreshStatusVisible = core
                .ToReactivePropertyAsSynchronized(x => x.IsLibraryRefreshStatusVisible)
                .AddTo(this.Disposables);
            this.IsFolderUpdatedNotificationVisible = core
                .ToReactivePropertyAsSynchronized(x => x.IsFolderUpdatedNotificationVisible)
                .AddTo(this.Disposables);

            this.IsViewerMoveButtonDisabled = core
                .ToReactivePropertyAsSynchronized(x => x.IsViewerMoveButtonDisabled)
                .AddTo(this.Disposables);

            this.IsFill = core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowResizeToFill)
                .AddTo(this.Disposables);

            this.IsResizingAlways = core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowResizingAlways)
                .AddTo(this.Disposables);

            this.IsSlideshowFullScreen = core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowFullScreen)
                .AddTo(this.Disposables);

            this.UseLogicalPixel = core
                .ToReactivePropertyAsSynchronized(x => x.UseLogicalPixel)
                .AddTo(this.Disposables);


            this.AnimationTimeSec = core.ToReactivePropertyAsSynchronized
                (x => x.SlideshowAnimationTimeMillisec, x => x / 1000.0, x => (int)(x * 1000.0))
                .AddTo(this.Disposables);


            this.FlipTimeSec = core.ToReactivePropertyAsSynchronized
                (x => x.SlideshowFlipTimeMillisec, x => x / 1000, x => x * 1000).AddTo(this.Disposables);

            this.CursorKeyBind = core
                .ToReactivePropertyAsSynchronized(x => x.CursorKeyBind).AddTo(this.Disposables);

            this.IsVersionCheckEnabled = core.ToReactivePropertyAsSynchronized
                (x => x.SkipVersionCheck, x => !x, x => !x).AddTo(this.Disposables);

            this.ScalingMode = core
                .ToReactivePropertyAsSynchronized(x => x.ScalingMode)
                .AddTo(this.Disposables);

            //this.GenerateNewClientCommand = new ReactiveCommand()
            //    .WithSubscribe(_ => core.ShowNewClient(null), this.Disposables);
            //
            //this.ConvertCommand = new ReactiveCommand()
            //    .WithSubscribe(_ => this.Convert(core).FireAndForget(), this.Disposables);
        }

    }
}
