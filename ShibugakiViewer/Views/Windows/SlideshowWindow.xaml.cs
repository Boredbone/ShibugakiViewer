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


        //public void OpenPopup
        //    (FrameworkElement content, Thickness position,
        //    HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
        //    FrameworkElement dock = null, bool isMaskVisible = true)
        //{
        //    this.popupDialog.IsMasked = isMaskVisible;
        //    this.popupDialog.DockControl = dock;
        //    this.popupDialog.HorizontalDialogAlignment = horizontalAlignment;
        //    this.popupDialog.VerticalDialogAlignment = verticalAlignment;
        //    this.popupDialog.Position = position;
        //    this.popupDialog.DialogContent = content;
        //    this.popupDialog.IsOpen = true;
        //}
        //
        //public void ClosePopup()
        //{
        //    this.popupDialog.IsOpen = false;
        //}
    }
}
