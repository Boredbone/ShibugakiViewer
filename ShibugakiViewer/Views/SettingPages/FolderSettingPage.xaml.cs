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
using ImageLibrary.Creation;

namespace ShibugakiViewer.Views.SettingPages
{
    /// <summary>
    /// FolderSettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderSettingPage : UserControl, IDisposable
    {
        public FolderSettingPage()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            (this.DataContext as IDisposable)?.Dispose();
        }
        

        private void itemGridView_Unloaded(object sender, RoutedEventArgs e)
        {

        }
    }

    public class CheckModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var key = value as FolderCheckMode?;
            if (key == null)
            {
                return null;
            }
            var core = ((App)Application.Current).Core;

            switch (key)
            {
                case FolderCheckMode.None:
                    return core.GetResourceString("RefreshModeDefault");
                case FolderCheckMode.Light:
                    return core.GetResourceString("RefreshModeFast");
                case FolderCheckMode.Detail:
                    return core.GetResourceString("RefreshModeSlow");
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
