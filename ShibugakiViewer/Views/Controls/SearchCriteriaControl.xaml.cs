using System;
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
using Database.Search;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.Views.Controls
{
    /// <summary>
    /// SearchCriteriaControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchCriteriaControl : UserControl, IDisposable
    {
        #region ItemClickCommand

        public ICommand ItemClickCommand
        {
            get { return (ICommand)GetValue(ItemClickCommandProperty); }
            set { SetValue(ItemClickCommandProperty, value); }
        }

        public static readonly DependencyProperty ItemClickCommandProperty =
            DependencyProperty.Register(nameof(ItemClickCommand), typeof(ICommand),
                typeof(SearchCriteriaControl), new PropertyMetadata(null));

        #endregion

        #region BackGroundColor

        public Brush BackGroundColor
        {
            get { return (Brush)GetValue(BackGroundColorProperty); }
            set { SetValue(BackGroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackGroundColorProperty =
            DependencyProperty.Register(nameof(BackGroundColor), typeof(Brush),
                typeof(SearchCriteriaControl), new PropertyMetadata(new SolidColorBrush(Colors.Gray)));

        #endregion

        #region IsComplex

        public bool IsComplex
        {
            get { return (bool)GetValue(IsComplexProperty); }
            set { SetValue(IsComplexProperty, value); }
        }

        public static readonly DependencyProperty IsComplexProperty =
            DependencyProperty.Register("IsComplex", typeof(bool),
                typeof(SearchCriteriaControl), new PropertyMetadata(false));


        #endregion

        #region IsSVO

        public bool IsSVO
        {
            get { return (bool)GetValue(IsSVOProperty); }
            set { SetValue(IsSVOProperty, value); }
        }

        public static readonly DependencyProperty IsSVOProperty =
            DependencyProperty.Register(nameof(IsSVO), typeof(bool),
                typeof(SearchCriteriaControl), new PropertyMetadata(false));


        #endregion


        #region Mode

        public string Mode
        {
            get { return (string)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(string),
                typeof(SearchCriteriaControl), new PropertyMetadata(null));

        #endregion

        #region Property
        public string Property
        {
            get { return (string)GetValue(PropertyProperty); }
            set { SetValue(PropertyProperty, value); }
        }

        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.Register(nameof(Property), typeof(string),
                typeof(SearchCriteriaControl), new PropertyMetadata(null));

        #endregion





        private CompositeDisposable disposables = new CompositeDisposable();

        public SearchCriteriaControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var item = this.DataContext as ComplexSearch;
            if (item != null)
            {
                item.IsOr = !item.IsOr;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var item = this.DataContext as ISqlSearch;
            if (item != null)
            {
                item.RemoveSelf();
            }
        }

        private void searchCriteriaItemRoot_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var item = this.DataContext as ISqlSearch;

            var core = ((App)Application.Current).Core;
            this.IsSVO = core.IsSVOLanguage;

            this.disposables.Clear();

            if (item == null)
            {
                this.BackGroundColor = new SolidColorBrush(Colors.Gray);
                this.IsComplex = false;
                this.Mode = null;
                this.Property = null;
            }
            else if (item.IsUnit)
            {
                this.BackGroundColor = (Brush)Application.Current.Resources["UnitSearchItemColor"];
                this.IsComplex = false;

                var unit = (UnitSearch)item;

                var isVString = core.GetResourceString("IsV");

                unit.ObserveProperty(x => x.Property)
                    .Select(x => x.GetPropertyLabel() + isVString)
                    .Subscribe(x => this.Property = x)
                    .AddTo(this.disposables);


                unit.ObserveProperty(x => x.Mode)
                    .CombineLatest(unit.ObserveProperty(x => x.Property), (m, p) => p.GetCompareLabel(m))
                    .Subscribe(x => this.Mode = x)
                    .AddTo(this.disposables);


            }
            else
            {
                this.BackGroundColor = (Brush)Application.Current.Resources["ComplexSearchItemColor"];
                this.IsComplex = true;

                this.Property = null;

                var pack = (ComplexSearch)item;

                pack.ObserveProperty(x => x.IsOr)
                    .Select(x => FilePropertyManager.GetMatchLabel(x))
                    .Subscribe(x => this.Mode = x)
                    .AddTo(this.disposables);

            }



            //this.rootGrid.DataContext = null;
            //this.rootGrid.DataContext = item;
        }

        public void Dispose()
        {
            this.disposables.Dispose();
        }

        private void searchCriteriaItemRoot_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Dispose();
        }

        private void treeItemPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {

            var item = this.DataContext as ComplexSearch;

            if (item != null)
            {
                item.IsOr = !item.IsOr;
            }
            else
            {
                this.ItemClickCommand.Execute(this.DataContext);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.ItemClickCommand.Execute(this.DataContext);
        }
    }

}
