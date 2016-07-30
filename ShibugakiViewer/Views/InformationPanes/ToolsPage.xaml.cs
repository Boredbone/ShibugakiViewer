﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
using Boredbone.Utility.Extensions;
using Boredbone.XamlTools.Extensions;
using Reactive.Bindings;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels;
using ShibugakiViewer.Views.Windows;

namespace ShibugakiViewer.Views.InformationPanes
{
    /// <summary>
    /// ToolsPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ToolsPage : UserControl
    {
        private readonly ApplicationCore core;
        private readonly CompositeDisposable disposables;

        public ToolsPage()
        {
            InitializeComponent();

            this.core = ((App)Application.Current).Core;
            var library = core.Library;

            this.disposables = new CompositeDisposable();

            this.refreshButton.Command= library.IsCreating
                .Select(x => !x)
                .ToReactiveCommand()
                .WithSubscribe(_ =>
                {
                    library.StartRefreshLibrary();
                }, this.disposables);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowSettingWindow();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new VersionWindow()
            {
                Owner = Window.GetWindow(this),
            }.Show();
        }

        private void FlatButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ClientWindowViewModel;
            if (vm != null)
            {
                vm.SelectedInformationPage.Value = OptionPaneType.KeyBind;
            }
        }

        private void FlatButton_Click_1(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ExitAll();
        }

        private void FlatButton_Click_2(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowLibraryUpdateStatusWindow();
            //new LibraryUpdateStatusWindow()
            //{
            //    Owner = Window.GetWindow(this),
            //}.Show();
        }
    }
}
