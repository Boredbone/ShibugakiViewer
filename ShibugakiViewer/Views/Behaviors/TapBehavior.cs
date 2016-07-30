using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Boredbone.Utility.Extensions;
using Boredbone.XamlTools.Extensions;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;
using System.Reactive.Subjects;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Views.Behaviors
{
    public class TapBehavior : Behavior<Control>, IDisposable
    {
        private Dictionary<string, IDisposable> Disposables { get; } = new Dictionary<string, IDisposable>();
        
        private Vector startPosition;
        private DateTime startTime;

        private bool moved = false;

        public event Action<object, PointerTapEventArgs> PointerTapped;
        //public event Action<object, PointerTapEventArgs> PointerDoubleTapped;
        //public event Action<object, PointerTapEventArgs> PointerLongTapped;

        private Subject<PointerTapEventArgs> TapSubject { get; set; }

        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();


            var target = this.AssociatedObject;
            if (target == null)
            {
                return;
            }



            // マウスダウン、マウスアップ、マウスムーブのIObservable
            var mouseDown = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => target.PreviewMouseLeftButtonDown += h,
                h => target.PreviewMouseLeftButtonDown -= h);

            var mouseMove = Observable.FromEvent<MouseEventHandler, MouseEventArgs>(
                h => (s, e) => h(e),
                h => target.MouseMove += h,
                h => target.MouseMove -= h);

            var mouseUp = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => target.PreviewMouseLeftButtonUp += h,
                h => target.PreviewMouseLeftButtonUp -= h);


            var doubleTap = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => target.MouseDoubleClick += h,
                h => target.MouseDoubleClick -= h)
                .Select(x => new PointerTapEventArgs()
                {
                    SenderHeight = this.AssociatedObject.ActualHeight,
                    SenderWidth = this.AssociatedObject.ActualWidth,
                    InnerArgs = x,
                    IsDoubleTap = true,
                });


            mouseMove
                .SkipUntil(mouseDown.Do(e => this.OnDown(e, target)))
                .TakeUntil(mouseUp.Do(e => this.OnUp(e, target)))
                .Repeat()
                .TimeInterval()
                .Subscribe(e =>
                {
                    this.OnMove(e.Value, target);
                })
                .AddTo(this.Disposables, "Drag");
            
            this.TapSubject = new Subject<PointerTapEventArgs>().AddTo(this.Disposables, "TapSubject");


            this.TapSubject
                .TimeInterval()
                .Subscribe(x =>
                {
                    x.Value.Interval = x.Interval;
                    this.PointerTapped?.Invoke(this, x.Value);
                })
                .AddTo(this.Disposables, "RawTap");


            /*
            var shortTap = this.TapSubject
                .Where(x => x.Span < TimeSpan.FromMilliseconds(300))
                .TimeInterval()
                .Select(x=>
                {
                    x.Value.Interval = x.Interval;
                    return x.Value;
                })
                .Publish().RefCount();


            var doubleClickTime = 300;

            var trigger = shortTap.Throttle(TimeSpan.FromMilliseconds(doubleClickTime))
                .Merge(shortTap.Where(x => x.Interval < TimeSpan.FromMilliseconds(doubleClickTime - 10)));

            var taps = shortTap.Buffer(trigger)
                .ObserveOnUIDispatcher()
                .Publish().RefCount();

            taps.Where(x => x.Count == 1)
                .Subscribe(x => this.PointerTapped?.Invoke(this, x.First()))
                .AddTo(this.Disposables, "SingleTap");

            taps.Where(x => x.Count > 1)
                .Subscribe(x => this.PointerDoubleTapped?.Invoke(this, x.Last()))
                .AddTo(this.Disposables, "DoubleTap");

            this.TapSubject.Where(x => x.Span > TimeSpan.FromMilliseconds(1000))
                .Subscribe(x => this.PointerLongTapped?.Invoke(this, x))
                .AddTo(this.Disposables, "LongTap");*/

        }

        /// <summary>
        /// ダウン時動作
        /// </summary>
        /// <param name="e"></param>
        private void OnDown(MouseButtonEventArgs e, UIElement target)
        {

            var position = (Vector)e.GetPosition(this.AssociatedObject);

            this.startPosition = position;
            this.startTime = DateTime.Now;
            this.moved = false;
            
        }

        /// <summary>
        /// 移動中動作
        /// </summary>
        /// <param name="e"></param>
        private void OnMove(MouseEventArgs e, UIElement target)
        {

            var point = (Vector)e.GetPosition(this.AssociatedObject);
            

            if ((point - this.startPosition).LengthSquared > 800)
            {
                this.moved = true;
            }
            
        }

        /// <summary>
        /// アップ時動作
        /// </summary>
        /// <param name="e"></param>
        private void OnUp(MouseButtonEventArgs e, UIElement target)
        {

            var timeSpan = DateTime.Now - this.startTime;

            if (!this.moved)
            {
                this.TapSubject.OnNext(new PointerTapEventArgs()
                {
                    Span = timeSpan,
                    StartPosition = this.startPosition,
                    EndPosition = (Vector)e.GetPosition(this.AssociatedObject),
                    InnerArgs = e,
                    SenderHeight = this.AssociatedObject.ActualHeight,
                    SenderWidth = this.AssociatedObject.ActualWidth,
                });
            }
            
        }
        

        public void Dispose()
        {
            this.Disposables.ForEach(y => y.Value.Dispose());
            this.Disposables.Clear();
        }
    }

    public class PointerTapEventArgs : EventArgs
    {
        public TimeSpan Span { get; set; }
        public TimeSpan Interval { get; set; }
        public Vector StartPosition { get; set; }
        public Vector EndPosition { get; set; }
        public MouseButtonEventArgs InnerArgs { get; set; }
        public double SenderWidth { get; set; }
        public double SenderHeight { get; set; }
        public bool IsDoubleTap { get; set; }
    }
    
}
