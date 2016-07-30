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
        //private readonly ApplicationCore core;

        public NotifyWindow()
        {
            InitializeComponent();

            this.app = (App)Application.Current;
            //this.core = this.app.Core;

            //core.Library.Loaded.Subscribe(this.ShowLibraryResult);

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
            //foreach(var window in Application.Current.Windows.OfType<Window>())
            //{
            //    window.Close();
            //}
            //Environment.Exit(0);
            //this.Close();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.app.ShowClientWindow(null);
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //this.ShowBalloonTip();
            this.app.ShowClientWindow(null);
        }

        //private void ShowBalloonTip()
        //{
        //    this.notifyIcon.ShowBalloonTip("Title", "Message", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        //}

        private void notifyIcon_TrayBalloonTipClicked(object sender, RoutedEventArgs e)
        {
            this.app.ShowLibraryUpdateStatusWindow();
            //this.app.ShowClientWindow(null);
        }

        //private void ShowLibraryResult(LibraryLoadResult result)
        //{
        //
        //    if (!this.core.IsLibraryRefreshStatusVisible)
        //    {
        //        return;
        //    }
        //    if (result.Action == LibraryLoadAction.Activation
        //        || ((result.Action == LibraryLoadAction.Startup || result.Action == LibraryLoadAction.FolderChanged)
        //        && result.AddedFiles.Count == 0
        //        && result.RemovedFiles.Count == 0))
        //    {
        //        return;
        //    }
        //
        //    if(result.Action == LibraryLoadAction.FolderChanged && !this.core.IsFolderUpdatedNotificationVisible)
        //    {
        //        return;
        //    }
        //
        //    var refreshedText = core.GetResourceString("RefreshedText");
        //    var addedText = core.GetResourceString("AddedText");
        //    var removedText = core.GetResourceString("RemovedText");
        //    var unitText = core.GetResourceString("UnitText");
        //    var updatedText = core.GetResourceString("UpdatedText");
        //
        //    var resultText
        //        = $"{refreshedText}\n{addedText} : {result.AddedFiles.Count} {unitText}, "
        //        + $"{removedText} : {result.RemovedFiles.Count} {unitText}, "
        //        + $"{updatedText} : {result.UpdatedFiles.Count} {unitText}";
        //
        //
        //    this.notifyIcon.ShowBalloonTip(this.core.AppName, resultText,
        //        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        //
        //}
    }
}
