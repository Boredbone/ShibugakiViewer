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

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window, IDisposable
    {
        private CompositeDisposable disposables = new CompositeDisposable();

        public SettingWindow()
        {
            InitializeComponent();

            ((App)Application.Current).WindowPlacement
                .Register(this, "SettingWindow");

            this.viewerSetting.AddTo(this.disposables);
            //this.libraryCreation.AddTo(this.disposables);
            this.folderSetting.AddTo(this.disposables);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
