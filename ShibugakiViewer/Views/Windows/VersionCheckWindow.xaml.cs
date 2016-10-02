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

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// VersionCheckWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class VersionCheckWindow : Window
    {
        public VersionCheckWindow()
        {
            InitializeComponent();

            var core = ((App)Application.Current).Core;

            this.DataContext = core;

            //var current = core.AppCurrentVersion;
            //var currentBuild = new Version(current.Major, current.Minor, current.Build);
            var last = core.LatestReleasedVersion;

            this.versionText.Text = last?.ToString() ?? "0.0.0";
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
