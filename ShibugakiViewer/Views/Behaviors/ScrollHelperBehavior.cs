using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Boredbone.Utility.Extensions;
using Boredbone.XamlTools.Extensions;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Views.Behaviors
{
    class ScrollHelperBehavior : Behavior<UIElement>
    {
        #region ScrollSpeed

        public double ScrollSpeed
        {
            get { return (double)GetValue(ScrollSpeedProperty); }
            set { SetValue(ScrollSpeedProperty, value); }
        }

        public static readonly DependencyProperty ScrollSpeedProperty =
            DependencyProperty.Register("ScrollSpeed", typeof(double),
                typeof(ScrollHelperBehavior), new PropertyMetadata(6.0));
        

        #endregion


        #region IsAnimationEnabled

        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAnimationEnabledProperty =
            DependencyProperty.Register("IsAnimationEnabled", typeof(bool),
                typeof(ScrollHelperBehavior), new PropertyMetadata(false));

        #endregion


        #region AnimationTime

        public double AnimationTime
        {
            get { return (double)GetValue(AnimationTimeProperty); }
            set { SetValue(AnimationTimeProperty, value); }
        }

        public static readonly DependencyProperty AnimationTimeProperty =
            DependencyProperty.Register("AnimationTime", typeof(double),
                typeof(ScrollHelperBehavior), new PropertyMetadata(100.0));


        #endregion

        #region RequestScrollAction

        public Action<Vector> RequestScrollAction
        {
            get { return (Action<Vector>)GetValue(RequestScrollActionProperty); }
            set { SetValue(RequestScrollActionProperty, value); }
        }

        public static readonly DependencyProperty RequestScrollActionProperty =
            DependencyProperty.Register(nameof(RequestScrollAction), typeof(Action<Vector>),
                typeof(ScrollHelperBehavior), new PropertyMetadata(null));

        #endregion


        

        




        private ScrollViewer childView = null;

        private Dictionary<int, SingleValueAnimator<double>> animatingObjects
            = new Dictionary<int, SingleValueAnimator<double>>();

        private Queue<ScrollRequestContainer> request = new Queue<ScrollRequestContainer>();

        private int idCount = 0;
        private int[] prevActedIds = null;


        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheelScrolled;

            this.RequestScrollAction = this.OnScrollRequested;
        }

        public ScrollViewer GetScrollViewer()
        {
            if (this.childView == null)
            {
                this.childView = (this.AssociatedObject as ScrollViewer)
                    ?? this.AssociatedObject.Descendants<ScrollViewer>().FirstOrDefault();
            }
            return this.childView;
        }

        private void OnScrollRequested(Vector vector)
        {
            if (vector.Y != 0.0)
            {
                var length = vector.Y;
                var sv = this.GetScrollViewer();

                if (sv != null)
                {
                    var height = sv.ActualHeight;

                    var delta
                        = double.IsNegativeInfinity(length) ? (-sv.VerticalOffset)
                        : double.IsPositiveInfinity(length) ? (sv.ScrollableHeight - sv.VerticalOffset)
                        : height * length;

                    var animation = this.IsAnimationEnabled && Math.Abs(delta) < height * 2;

                    this.ScrollToVerticalOffset(delta, animation);


                }
            }
        }

        private void OnPreviewMouseWheelScrolled(object sender, MouseWheelEventArgs e)
        {
            var offset = -(e.Delta * this.ScrollSpeed / 6.0);
            ScrollToVerticalOffset(offset, this.IsAnimationEnabled);
            e.Handled = true;

            //Debug.WriteLine($"{DateTime.Now.Ticks}");
        }


        private void ScrollToVerticalOffset(double delta, bool isAnimationEnabled)
        {
            
            var scrollViewer = this.GetScrollViewer();

            if (scrollViewer == null)
            {
                return;
            }

            var time = this.AnimationTime;

            if (time <= 0 || !isAnimationEnabled)
            {
                // no animation
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);
                return;
            }

            var animation = new DoubleAnimation()
            {
                From = 0,
                To = delta,
                //EasingFunction = new CircleEase() { EasingMode = EasingMode.EaseOut },
                Duration = new Duration(TimeSpan.FromMilliseconds(time)),
            };

            var target = new SingleValueAnimator<double>();

            var id = this.idCount++;

            target.ValueChanged += (o, e) => this.Animated(scrollViewer, id, target, e);

            //var startTime = DateTime.Now.Ticks;

            animation.Completed += (o, e) =>
            {
                //Debug.WriteLine(DateTime.Now.Ticks - startTime);
                this.animatingObjects.Remove(id);
            };

            //Debug.WriteLine($"{DateTime.Now.Ticks}, {this.animatingObjects.Count}");

            target.BeginAnimation(SingleValueAnimator<double>.ValueProperty, animation);
        }

        /// <summary>
        /// アニメーション中
        /// </summary>
        /// <param name="scrollViewer"></param>
        /// <param name="id"></param>
        /// <param name="e"></param>
        private void Animated
            (ScrollViewer scrollViewer, int id, SingleValueAnimator<double> target,
            DependencyPropertyChangedEventArgs e)
        {
            

            this.request.Enqueue(new ScrollRequestContainer()
            {
                OldValue = (double)e.OldValue,
                NewValue = (double)e.NewValue,
                Id = id,
            });

            this.animatingObjects[id] = target;

            //リクエストが溜まっていない間はスクロールしない
            if (request.Count < this.animatingObjects.Count)
            {
                //Debug.WriteLine($"request:{request.Count},animating:{this.animatingObjects.Count}");
                return;
            }

            var items = this.request.ToArray();
            this.request.Clear();

            var oldValue = 0.0;
            var newValue = 0.0;

            foreach(var item in items)
            {
                oldValue += item.OldValue;
                newValue += item.NewValue;
            }
            //var oldValue = items.Sum(x => x.OldValue);
            //var newValue = items.Sum(x => x.NewValue);

            var ids = items.Select(x => x.Id).Distinct().ToArray();

            if (this.prevActedIds != null)
            {
                //動いていないアニメーションが検出された

                var actedIds = this.prevActedIds.Concat(ids).Distinct().ToArray();
                

                this.animatingObjects
                    .Where(x => !actedIds.Contains(x.Key))
                    .ToArray()
                    .ForEach(x =>
                    {
                        this.animatingObjects.Remove(x.Key);
                        Debug.WriteLine($"aborted animation is detected:{x.Key}");
                    });

                this.prevActedIds = null;
            }
            else
            {
                if (ids.Length != items.Length)
                {
                    //Id被り=動いていないアニメーションがある
                    this.prevActedIds = ids;
                    Debug.WriteLine("animation id is repeated");
                }
                else
                {
                    this.prevActedIds = null;
                }
            }


            var b = scrollViewer.VerticalOffset - oldValue;
            scrollViewer.ScrollToVerticalOffset(b + newValue);
            //Debug.WriteLine($"{DateTime.Now.Ticks}, {b + newValue}, {items.Length}");
        }

        private class ScrollRequestContainer
        {
            public double OldValue { get; set; }
            public double NewValue { get; set; }
            public int Id { get; set; }
        }
    }

    class SingleValueAnimator<T> : Animatable
    {
        #region Value

        public T Value
        {
            get => (T)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(T), typeof(SingleValueAnimator<T>),
                new PropertyMetadata(default(T), new PropertyChangedCallback(OnValueChanged)));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SingleValueAnimator<T> thisInstance)
            {
                thisInstance.ValueChanged?.Invoke(d, e);
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new SingleValueAnimator<T>();
        }

        #endregion

        public event PropertyChangedCallback ValueChanged;

    }
}
