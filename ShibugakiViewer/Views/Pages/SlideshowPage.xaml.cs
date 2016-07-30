using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.ViewModels;

namespace ShibugakiViewer.Views.Pages
{
    /// <summary>
    /// SlideshowPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SlideshowPage : UserControl, IDisposable
    {
        private CompositeDisposable disposables;
        private CompositeDisposable bindingDisposables = new CompositeDisposable();



        public SlideshowPage()
        {
            InitializeComponent();

            this.disposables = new CompositeDisposable();

            Disposable.Create(() => this.bindingDisposables.Clear()).AddTo(this.disposables);
        }

        public void Dispose()
        {
            this.disposables.Dispose();
            (this.DataContext as IDisposable)?.Dispose();
        }

        private void imageGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void pageRoot_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            var vm = e.NewValue as SlideshowPageViewModel;

            this.bindingDisposables.Clear();

            if (vm == null)
            {
                return;
            }

            this.inAnimation.KeyTime = vm.Interval;
            this.outAnimation.KeyTime = vm.Interval;

            Observable.FromEvent<EventHandler, EventArgs>
                 (h => (s, ea) => h(ea),
                 h => this.animationStoryBoard.Completed += h,
                 h => this.animationStoryBoard.Completed -= h)
                 .Subscribe(_ => vm.ChangeImage())
                 .AddTo(this.bindingDisposables);

            vm.ObserveProperty(x => x.Interval)
                .Subscribe(x =>
                {
                    this.inAnimation.KeyTime = x;
                    this.outAnimation.KeyTime = x;
                })
                .AddTo(this.bindingDisposables);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
    }
}
