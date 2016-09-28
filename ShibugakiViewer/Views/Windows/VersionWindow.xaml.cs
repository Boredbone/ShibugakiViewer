using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using ShibugakiViewer.Models;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// VersionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class VersionWindow : Window
    {
        public VersionWindow()
        {
            InitializeComponent();

            var core = ((App)Application.Current).Core;

            var appName = core.AppName;
            this.Title = $"About {appName}";
            
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            this.versionText.Text = ver.ToString();

            this.projectHomeLink.NavigateUri = new Uri(core.ProjectHomeUrl);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
