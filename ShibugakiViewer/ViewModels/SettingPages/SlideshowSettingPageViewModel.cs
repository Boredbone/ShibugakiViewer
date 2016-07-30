using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using ImageLibrary.Core;
using ImageLibrary.File;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    public class SlideshowSettingPageViewModel : DisposableBase
    {
        public ReactiveProperty<bool> IsResizingAlways { get; }
        public ReactiveProperty<bool> IsResizeToFill { get; }
        public ReactiveProperty<bool> IsFullScreen { get; }

        public ReactiveProperty<double> AnimationTimeSec { get; }
        public ReactiveProperty<int> FlipTimeSec { get; }

        public SlideshowSettingPageViewModel()
        {

            var core = ((App)Application.Current).Core;
            var library = core.Library;

            this.IsResizeToFill = core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowResizeToFill)
                .AddTo(this.Disposables);

            this.IsResizingAlways = core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowResizingAlways)
                .AddTo(this.Disposables);


            this.IsFullScreen = core
                .ToReactivePropertyAsSynchronized(x => x.IsSlideshowFullScreen)
                .AddTo(this.Disposables);

            this.AnimationTimeSec = core.ToReactivePropertyAsSynchronized
                (x => x.SlideshowAnimationTimeMillisec, x => x / 1000.0, x => (int)(x * 1000.0))
                .AddTo(this.Disposables);


            this.FlipTimeSec = core.ToReactivePropertyAsSynchronized
                (x => x.SlideshowFlipTimeMillisec, x => x / 1000, x => x * 1000).AddTo(this.Disposables);
        }
    }
}
