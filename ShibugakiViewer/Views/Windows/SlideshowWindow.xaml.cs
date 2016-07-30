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
using ShibugakiViewer.Views.Controls;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// SlideshowWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SlideshowWindow : Window, IDisposable, IPopupDialogOwner
    {
        public PopupDialog PopupDialog => this.popupDialog;

        public SlideshowWindow()
        {
            InitializeComponent();

            ((App)Application.Current).WindowPlacement
                .Register(this, "SlideshowWindow");
        }

        public void Dispose()
        {
            this.slideshowPage.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
