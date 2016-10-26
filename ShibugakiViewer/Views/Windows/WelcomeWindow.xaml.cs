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
using Reactive.Bindings.Extensions;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.ViewModels.SettingPages;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// WelcomeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WelcomeWindow : Window, IDisposable
    {
        private readonly SerialDisposable subscription = new SerialDisposable();
        private readonly CompositeDisposable disposables;

        private bool exitAll = true;
        private bool closing = false;

        public WelcomeWindow()
        {
            InitializeComponent();

            this.disposables = new CompositeDisposable();

            this.subscription.AddTo(this.disposables);
            this.folderSetting.AddTo(this.disposables);
            this.libraryCreation.AddTo(this.disposables);

            var fvm = this.folderSetting.DataContext as FolderSettingPageViewModel;
            if (fvm != null)
            {
                fvm.IsInitializeMode.Value = true;
            }
        }

        public void Dispose()
        {
            this.disposables.Dispose();
            (this.DataContext as IDisposable)?.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!this.closing)
            {
                this.closing = true;

                if (this.exitAll)
                {
                    this.exitAll = false;
                    ((App)Application.Current).ExitAll();
                }
                this.Dispose();
            }
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = e.NewValue as WelcomeWindowViewModel;
            if (vm != null)
            {
                this.subscription.Disposable = vm.Exit.ObserveOnUIDispatcher().Subscribe(x =>
                {
                    if (x)
                    {
                        this.exitAll = false;
                        this.Close();
                    }
                });
            }
        }
    }
}
