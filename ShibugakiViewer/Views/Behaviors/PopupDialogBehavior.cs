using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Media.Imaging;
using Boredbone.XamlTools;
using ImageLibrary.File;
using ShibugakiViewer.Models;
using ShibugakiViewer.Models.ImageViewer;
using Boredbone.Utility.Extensions;
using System.Windows.Controls.Primitives;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.Views.Controls;

namespace ShibugakiViewer.Views.Behaviors
{
    class PopupDialogBehavior : Behavior<ButtonBase>
    {
        #region IsHorizontalOverlay

        public bool IsHorizontalOverlay
        {
            get { return (bool)GetValue(IsHorizontalOverlayProperty); }
            set { SetValue(IsHorizontalOverlayProperty, value); }
        }

        public static readonly DependencyProperty IsHorizontalOverlayProperty =
            DependencyProperty.Register(nameof(IsHorizontalOverlay), typeof(bool),
                typeof(PopupDialogBehavior), new PropertyMetadata(false));

        #endregion

        #region IsVerticalOverlay

        public bool IsVerticalOverlay
        {
            get { return (bool)GetValue(IsVerticalOverlayProperty); }
            set { SetValue(IsVerticalOverlayProperty, value); }
        }

        public static readonly DependencyProperty IsVerticalOverlayProperty =
            DependencyProperty.Register(nameof(IsVerticalOverlay), typeof(bool),
                typeof(PopupDialogBehavior), new PropertyMetadata(false));

        #endregion



        #region Content

        public FrameworkElement Content
        {
            get { return (FrameworkElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(FrameworkElement),
                typeof(PopupDialogBehavior), new PropertyMetadata(null));

        #endregion

        #region Position

        public Thickness Position
        {
            get { return (Thickness)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(nameof(Position), typeof(Thickness),
                typeof(PopupDialogBehavior), new PropertyMetadata(default(Thickness)));


        #endregion

        #region HorizontalContentAlignment

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.Register(nameof(HorizontalContentAlignment), typeof(HorizontalAlignment),
                typeof(PopupDialogBehavior), new PropertyMetadata(HorizontalAlignment.Center));

        #endregion

        #region VerticalContentAlignment

        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        public static readonly DependencyProperty VerticalContentAlignmentProperty =
            DependencyProperty.Register(nameof(VerticalContentAlignment), typeof(VerticalAlignment),
                typeof(PopupDialogBehavior), new PropertyMetadata(VerticalAlignment.Bottom));

        #endregion

        #region IsMaskVisible

        public bool IsMaskVisible
        {
            get { return (bool)GetValue(IsMaskVisibleProperty); }
            set { SetValue(IsMaskVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsMaskVisibleProperty =
            DependencyProperty.Register(nameof(IsMaskVisible), typeof(bool),
                typeof(PopupDialogBehavior), new PropertyMetadata(false));
        
        #endregion







        private IPopupDialogOwner parent;



        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Click += (o, e) => this.Open();
        }


        private void Open()
        {
            if (this.parent == null)
            {
                var window = Window.GetWindow(this.AssociatedObject) as IPopupDialogOwner;
                //var vm = window.DataContext as ClientWindowViewModel;
                this.parent = window;
            }

            if (this.parent == null || this.Content == null)
            {
                return;
            }

            var left = (this.HorizontalContentAlignment == HorizontalAlignment.Right && this.IsHorizontalOverlay)
                ? double.NaN : this.Position.Left;
            var right = (this.HorizontalContentAlignment == HorizontalAlignment.Left && this.IsHorizontalOverlay)
                ? double.NaN : this.Position.Right;


            var top = (this.VerticalContentAlignment == VerticalAlignment.Bottom && this.IsVerticalOverlay)
                ? double.NaN : this.Position.Top;
            var bottom = (this.VerticalContentAlignment == VerticalAlignment.Top && this.IsVerticalOverlay)
                ? double.NaN : this.Position.Bottom;


            var position = new Thickness(left, top, right, bottom);

            this.Content.DataContext = this.AssociatedObject.DataContext;
            this.Content.IsEnabled = true;

            this.parent.PopupDialog.Show(this.Content, position,
                this.HorizontalContentAlignment, this.VerticalContentAlignment,
                this.AssociatedObject, this.IsMaskVisible);
        }

    }
}
