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
using Boredbone.Utility.Extensions;
using Boredbone.XamlTools.Extensions;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.Views.Controls;
using ShibugakiViewer.Views.Pages;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// ClientWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ClientWindow : Window, IDisposable, IPopupDialogOwner
    {
        public PopupDialog PopupDialog => this.popupDialog;
        private CompositeDisposable disposables = new CompositeDisposable();

        public ClientWindow()
        {
            InitializeComponent();

            ((App)Application.Current).WindowPlacement
                .Register(this, "ClientWindow");

            this.catalogPage.AddTo(this.disposables);
            this.viewerPage.AddTo(this.disposables);
            this.fileInformation.AddTo(this.disposables);
        }

        public void Dispose()
        {
            this.disposables.Dispose();
            (this.DataContext as IDisposable)?.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = e.NewValue as ClientWindowViewModel;
            if (vm != null)
            {
                vm.View = this;
                vm.PopupOwner = this;
            }
        }


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //起動時に戻るボタンが一瞬有効化されるので、ロード完了までは強制無効化
            if (!this.backButton.IsEnabled)
            {
                this.backButton.IsEnabled = true;
            }
        }
    }
}
