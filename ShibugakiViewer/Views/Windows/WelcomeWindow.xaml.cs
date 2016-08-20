using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ShibugakiViewer.ViewModels;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// WelcomeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WelcomeWindow : Window, IDisposable
    {
        private readonly SerialDisposable subscription = new SerialDisposable();


        public WelcomeWindow()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            (this.DataContext as IDisposable)?.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = e.NewValue as WelcomeWindowViewModel;
            if (vm != null)
            {
                this.subscription.Disposable = vm.Exit.Subscribe(x =>
                {
                    if (x)
                    {
                        this.Close();
                    }
                });
            }
        }
    }
}
