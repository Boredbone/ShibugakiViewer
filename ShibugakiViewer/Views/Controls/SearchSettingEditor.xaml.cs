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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ShibugakiViewer.ViewModels.Controls;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// SearchSettingEditor.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchSettingEditor : UserControl
    {
        public SearchSettingEditor()
        {
            InitializeComponent();
        }

        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var sv = ((FrameworkElement)sender).Parent as ScrollViewer;
            if (sv == null)
            {
                return;
            }
            if (sv.ScrollableHeight > 0)
            {
                sv.ScrollToBottom();
                //sv.ChangeView(null, sv.ScrollableHeight, null);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
        }
    }
}
