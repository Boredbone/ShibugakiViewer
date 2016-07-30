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
using ImageLibrary.Core;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.Views.Windows
{
    /// <summary>
    /// NotifyWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NotifyWindow : Window
    {
        public static readonly ICommand OpenCommand = new RoutedCommand(nameof(OpenCommand), typeof(NotifyWindow));

        private readonly App app;

        public NotifyWindow()
        {
            InitializeComponent();

            this.app = (App)Application.Current;

            this.app.Core.SystemNotification.Subscribe(x =>
            {
                this.notifyIcon.ShowBalloonTip(this.app.Core.AppName, x,
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            });
        }

        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void TerminateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.app.ExitAll();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.app.ShowClientWindow(null);
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.app.ShowClientWindow(null);
        }

        private void notifyIcon_TrayBalloonTipClicked(object sender, RoutedEventArgs e)
        {
            this.app.ShowLibraryUpdateStatusWindow();
        }
        
    }
}
