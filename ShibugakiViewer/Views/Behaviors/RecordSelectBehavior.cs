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
using System.Windows.Controls.Primitives;
using Boredbone.Utility.Tools;
using ImageLibrary.File;
using System.Collections.Specialized;
using ImageLibrary.Viewer;
using System.Windows.Media;

namespace ShibugakiViewer.Views.Behaviors
{
    class RecordSelectBehavior : Behavior<ToggleButton>, IDisposable
    {
        private Dictionary<string, IDisposable> Disposables { get; } = new Dictionary<string, IDisposable>();

        #region Manager

        public SelectionManager Manager
        {
            get { return (SelectionManager)GetValue(ManagerProperty); }
            set { SetValue(ManagerProperty, value); }
        }

        public static readonly DependencyProperty ManagerProperty =
            DependencyProperty.Register(nameof(Manager), typeof(SelectionManager), typeof(RecordSelectBehavior),
            new PropertyMetadata(null, new PropertyChangedCallback(OnManagerChanged)));

        private static void OnManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as RecordSelectBehavior;
            var manager = e.NewValue as SelectionManager;

            if (manager != null)
            {
                thisInstance.SubscribeEvents(manager);
            }

            thisInstance?.Refresh(null);
        }

        #endregion





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

            target.DataContextChanged += (o, e) => this.Refresh(e.NewValue as Record);
            target.Checked += (o, e) => this.Check(true);
            target.Unchecked += (o, e) => this.Check(false);


        }

        private void SubscribeEvents(SelectionManager manager)
        {
            manager.SubscribeCollectionChanged(
                () => this.AssociatedObject.IsChecked ?? false,
                x => this.AssociatedObject.IsChecked = x,
                () => (this.AssociatedObject.DataContext as Record)?.Id)
                .AddTo(this.Disposables, "Selection");
            /*
            manager.Added.Subscribe(x =>
            {
                if (this.AssociatedObject.IsChecked == true)
                {
                    return;
                }

                var key = (this.AssociatedObject.DataContext as Record)?.Id;
                if (key == null)
                {
                    return;
                }

                if (x.ContainsKey(key))
                {
                    this.AssociatedObject.IsChecked = true;
                }
            })
            .AddTo(this.Disposables, "Added");

            manager.Removed.Subscribe(x =>
            {
                if (this.AssociatedObject.IsChecked == false)
                {
                    return;
                }

                var key = (this.AssociatedObject.DataContext as Record)?.Id;
                if (key == null)
                {
                    return;
                }

                if (x.ContainsKey(key))
                {
                    this.AssociatedObject.IsChecked = false;
                }

            })
            .AddTo(this.Disposables, "Removed");

            manager.Cleared.Subscribe(x =>
            {

                var key = (this.AssociatedObject.DataContext as Record)?.Id;
                if (key == null)
                {
                    return;
                }

                this.AssociatedObject.IsChecked = this.Manager.Contains(key);
            })
            .AddTo(this.Disposables, "Cleared");*/
        }


        private void Refresh(Record record)
        {
            if (this.AssociatedObject == null)
            {
                return;
            }

            if (record == null)
            {
                record = this.AssociatedObject.DataContext as Record;
            }

            if (this.Manager == null || record == null)
            {
                this.AssociatedObject.IsChecked = false;
                return;
            }

            this.AssociatedObject.IsChecked = this.Manager.Contains(record);

        }


        private void Check(bool value)
        {
            if (this.AssociatedObject == null)
            {
                return;
            }

            var record = this.AssociatedObject.DataContext as Record;

            if (this.Manager == null || record == null)
            {
                this.AssociatedObject.IsChecked = false;
                return;
            }

            if (value)
            {
                this.Manager.AddOrReplace(record);
            }
            else
            {
                this.Manager.Remove(record);
            }
        }
        /*
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            var key = (this.AssociatedObject.DataContext as Record)?.Id;
            if (key == null)
            {
                return;
            }

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (this.AssociatedObject.IsChecked == false && args.NewItems.Contains(key))
                    {
                        this.AssociatedObject.IsChecked = true;
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (this.AssociatedObject.IsChecked == true && args.OldItems.Contains(key))
                    {
                        this.AssociatedObject.IsChecked = false;
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (this.AssociatedObject.IsChecked == false && args.NewItems.Contains(key))
                    {
                        this.AssociatedObject.IsChecked = true;
                    }
                    else if (this.AssociatedObject.IsChecked == true && args.OldItems.Contains(key))
                    {
                        this.AssociatedObject.IsChecked = false;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:

                    this.AssociatedObject.IsChecked = this.Manager.Contains(key);

                    break;
            }
        }*/

        public void Dispose()
        {
            this.Disposables.ForEach(y => y.Value.Dispose());
            this.Disposables.Clear();
        }
    }


    class RemoveFromGroupBehavior : Behavior<Panel>, IDisposable
    {
        private Dictionary<string, IDisposable> Disposables { get; } = new Dictionary<string, IDisposable>();

        #region Manager

        public SelectionManager Manager
        {
            get { return (SelectionManager)GetValue(ManagerProperty); }
            set { SetValue(ManagerProperty, value); }
        }

        public static readonly DependencyProperty ManagerProperty =
            DependencyProperty.Register(nameof(Manager), typeof(SelectionManager), typeof(RemoveFromGroupBehavior),
            new PropertyMetadata(null, new PropertyChangedCallback(OnManagerChanged)));

        private static void OnManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as RemoveFromGroupBehavior;
            var manager = e.NewValue as SelectionManager;

            if (manager != null)
            {
                thisInstance.SubscribeEvents(manager);
            }

            thisInstance?.Refresh(null);
        }

        #endregion

        #region IsSelected

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RemoveFromGroupBehavior),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as RemoveFromGroupBehavior;
            var value = e.NewValue as bool?;

            if (thisInstance != null || value.HasValue)
            {
                thisInstance.AssociatedObject.Background =
                    (value.Value) ? thisInstance.SelectedBrush : new SolidColorBrush(Colors.Transparent);
            }

        }

        #endregion

        #region SelectedBrush

        public Brush SelectedBrush
        {
            get { return (Brush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register(nameof(SelectedBrush), typeof(Brush),
                typeof(RemoveFromGroupBehavior), new PropertyMetadata(null));

        #endregion





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

            target.DataContextChanged += (o, e) => this.Refresh(e.NewValue as Record);

        }

        private void SubscribeEvents(SelectionManager manager)
        {
            manager.SubscribeCollectionChanged(
                () => this.IsSelected,
                x => this.IsSelected = x,
                () => (this.AssociatedObject.DataContext as Record)?.Id)
                .AddTo(this.Disposables, "Selection");
        }


        private void Refresh(Record record)
        {
            if (this.AssociatedObject == null)
            {
                return;
            }

            if (record == null)
            {
                record = this.AssociatedObject.DataContext as Record;
            }

            if (this.Manager == null || record == null)
            {
                this.IsSelected = false;
                return;
            }

            this.IsSelected = this.Manager.Contains(record);

        }

        
        public void Dispose()
        {
            this.Disposables.ForEach(y => y.Value.Dispose());
            this.Disposables.Clear();
        }
    }
}

