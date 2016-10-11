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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Views.Pages
{
    /// <summary>
    /// ViewerPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ViewerPage : UserControl, IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public ViewerPage()
        {
            InitializeComponent();
            this.scrollImageViewer.AddTo(this.disposables);
        }

        public void Dispose()
        {
            this.disposables.Dispose();
        }
    }
}
