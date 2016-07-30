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

namespace ShibugakiViewer.Views.SettingPages
{
    /// <summary>
    /// ViewerSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ViewerSettingPage : UserControl, IDisposable
    {
        public ViewerSettingPage()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            (this.DataContext as IDisposable)?.Dispose();
        }
    }
}
