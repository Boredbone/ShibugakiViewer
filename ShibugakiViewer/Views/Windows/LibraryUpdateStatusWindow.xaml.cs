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

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// LibraryUpdateStatusWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LibraryUpdateStatusWindow : Window, IDisposable
    {
        private CompositeDisposable disposables = new CompositeDisposable();

        public LibraryUpdateStatusWindow()
        {
            InitializeComponent();

            ((App)Application.Current).WindowPlacement
                .Register(this, "LibraryUpdateStatusWindow");

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
    }
}
