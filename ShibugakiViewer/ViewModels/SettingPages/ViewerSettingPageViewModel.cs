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

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class ViewerSettingPageViewModel : DisposableBase
    {

        public ReactivePropertySlim<int> CursorKeyBind { get; }
        [Range(64, 512)]
        public ReactiveProperty<int> ThumbnailSize { get; }
        public ReactivePropertySlim<bool> IsAutoInformationPaneEnabled { get; }

        public ReactivePropertySlim<bool> IsDarkTheme { get; }

        public ReactivePropertySlim<bool> IsGroupingEnabled { get; }
        public ReactivePropertySlim<bool> RefreshLibraryCompletely { get; }

        public ReactivePropertySlim<bool> IsFlipReversed { get; }
        public ReactivePropertySlim<bool> IsAnimatedGifEnabled { get; }
        public ReactivePropertySlim<bool> IsExifOrientationDisabled { get; }
        public ReactivePropertySlim<bool> IsOpenNavigationWithSingleTapEnabled { get; }
        public ReactivePropertySlim<bool> UseExtendedMouseButtonsToSwitchImage { get; }
        public ReactivePropertySlim<bool> RefreshLibraryOnLaunched { get; }
        public ReactivePropertySlim<bool> IsLibraryRefreshStatusVisible { get; }
        public ReactivePropertySlim<bool> IsFolderUpdatedNotificationVisible { get; }
        public ReactivePropertySlim<bool> IsViewerMoveButtonDisabled { get; }
        public ReactivePropertySlim<bool> IsCheckFileShellInformation { get; }

        public ReactivePropertySlim<bool> IsFill { get; }
        public ReactivePropertySlim<bool> IsResizingAlways { get; }
        public ReactivePropertySlim<bool> IsSlideshowFullScreen { get; }
        public ReactivePropertySlim<bool> UseLogicalPixel { get; }

        public ReactivePropertySlim<double> AnimationTimeSec { get; }
        public ReactivePropertySlim<int> FlipTimeSec { get; }

        public ReactivePropertySlim<Color> BackColor { get; }

        public ReactivePropertySlim<bool> IsVersionCheckEnabled { get; }
        public ReactivePropertySlim<int> ScalingMode { get; }


        public ViewerSettingPageViewModel()
        {
            var core = ((App)Application.Current).Core;
            var library = core.Library;
            

            this.ThumbnailSize = core
                .ToReactivePropertyAsSynchronized(x => x.ThumbNailSize, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => this.ThumbnailSize)
                .AddTo(this.Disposables);

            this.IsAutoInformationPaneEnabled = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsAutoInformationPaneDisabled, x => !x, x => !x)
                .AddTo(this.Disposables);

            this.IsDarkTheme = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsDarkTheme)
                .AddTo(this.Disposables);

            this.BackColor = core
                .ToReactivePropertySlimAsSynchronized(x => x.BackgroundColor)
                .AddTo(this.Disposables);


            this.IsGroupingEnabled = library
                .ToReactivePropertySlimAsSynchronized(x => x.IsGroupingEnabled)
                .AddTo(this.Disposables);

            this.RefreshLibraryCompletely = library
                .ToReactivePropertySlimAsSynchronized(x => x.RefreshLibraryCompletely)
                .AddTo(this.Disposables);

            this.IsFlipReversed = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsFlipReversed)
                .AddTo(this.Disposables);
            this.IsAnimatedGifEnabled = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsAnimatedGifEnabled)
                .AddTo(this.Disposables);
            this.IsExifOrientationDisabled = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsExifOrientationDisabled)
                .AddTo(this.Disposables);
            this.IsOpenNavigationWithSingleTapEnabled = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsOpenNavigationWithSingleTapEnabled)
                .AddTo(this.Disposables);
            this.UseExtendedMouseButtonsToSwitchImage = core
                .ToReactivePropertySlimAsSynchronized(x => x.UseExtendedMouseButtonsToSwitchImage)
                .AddTo(this.Disposables);
            this.RefreshLibraryOnLaunched = core
                .ToReactivePropertySlimAsSynchronized(x => x.RefreshLibraryOnLaunched)
                .AddTo(this.Disposables);
            this.IsLibraryRefreshStatusVisible = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsLibraryRefreshStatusVisible)
                .AddTo(this.Disposables);
            this.IsFolderUpdatedNotificationVisible = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsFolderUpdatedNotificationVisible)
                .AddTo(this.Disposables);

            this.IsViewerMoveButtonDisabled = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsViewerMoveButtonDisabled)
                .AddTo(this.Disposables);

            this.IsFill = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsSlideshowResizeToFill)
                .AddTo(this.Disposables);

            this.IsResizingAlways = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsSlideshowResizingAlways)
                .AddTo(this.Disposables);

            this.IsSlideshowFullScreen = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsSlideshowFullScreen)
                .AddTo(this.Disposables);

            this.UseLogicalPixel = core
                .ToReactivePropertySlimAsSynchronized(x => x.UseLogicalPixel)
                .AddTo(this.Disposables);

            this.IsCheckFileShellInformation = library
                .ToReactivePropertySlimAsSynchronized(x => x.CheckFileShellInformation)
                .AddTo(this.Disposables);


            this.AnimationTimeSec = core.ToReactivePropertySlimAsSynchronized
                (x => x.SlideshowAnimationTimeMillisec, x => x / 1000.0, x => (int)(x * 1000.0))
                .AddTo(this.Disposables);


            this.FlipTimeSec = core.ToReactivePropertySlimAsSynchronized
                (x => x.SlideshowFlipTimeMillisec, x => x / 1000, x => x * 1000).AddTo(this.Disposables);

            this.CursorKeyBind = core
                .ToReactivePropertySlimAsSynchronized(x => x.CursorKeyBind).AddTo(this.Disposables);

            this.IsVersionCheckEnabled = core.ToReactivePropertySlimAsSynchronized
                (x => x.SkipVersionCheck, x => !x, x => !x).AddTo(this.Disposables);

            this.ScalingMode = core
                .ToReactivePropertySlimAsSynchronized(x => x.ScalingMode)
                .AddTo(this.Disposables);
            
        }
    }
}
