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
using WpfTools;

namespace ShibugakiViewer.Views.InformationPanes
{
    /// <summary>
    /// HelpPage.xaml の相互作用ロジック
    /// </summary>
    public partial class HelpPage : UserControl
    {
        private readonly App application;
        private readonly ApplicationCore core;

        public HelpPage()
        {
            InitializeComponent();

            this.application = (App)Application.Current;
            this.core = this.application.Core;
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
        
        
    }
}
