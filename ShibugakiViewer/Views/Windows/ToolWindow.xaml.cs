using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Reactive.Disposables;
using ShibugakiViewer.Models;
using WpfTools;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// ToolWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ToolWindow : Window, IDisposable
    {
        private readonly App application;
        public ApplicationCore Core { get; }
        private CompositeDisposable disposables = new CompositeDisposable();

        private bool conpatButtonLoaded = false;

        public ToolWindow()
        {
            this.application = (App)Application.Current;
            this.Core = this.application.Core;

            InitializeComponent();

            this.application.WindowPlacement.Register(this, "ToolWindow");



            this.libraryCreation.AddTo(this.disposables);
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

        private void FlatButton_Click_3(object sender, RoutedEventArgs e)
        {
            this.application.ImportOrExportLibrary(false, true);
        }

        private void FlatButton_Click_4(object sender, RoutedEventArgs e)
        {
            this.application.ImportOrExportLibrary(true, true);
        }

        private void mikanImportButton_Click(object sender, RoutedEventArgs e)
        {
            this.application.ConvertOldLibrary();
        }


        private async void mikanImportButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (!conpatButtonLoaded)
            {
                conpatButtonLoaded = true;
                var convertable = await this.Core.IsOldConvertableAsync();
                this.mikanImportButton.Visibility = VisibilityHelper.Set(convertable);
            }
        }

    }
}
