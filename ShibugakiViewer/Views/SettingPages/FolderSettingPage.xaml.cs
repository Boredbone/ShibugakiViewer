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
    }

    public class CheckModeConverter : IValueConverter
    {
        //private static Lazy<Dictionary<FolderCheckMode, string>> dictionary
        //    = new Lazy<Dictionary<FolderCheckMode, string>>(() =>
        //    {
        //        var core = ((App)Application.Current).Core;
        //        return new Dictionary<FolderCheckMode, string>
        //        {
        //            [FolderCheckMode.None] = core.GetResourceString("RefreshModeDefault"),
        //            [FolderCheckMode.Light] = core.GetResourceString("RefreshModeFast"),
        //            [FolderCheckMode.Detail] = core.GetResourceString("RefreshModeSlow"),
        //        };
        //    });

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var key = value as FolderCheckMode?;
            if (key == null)
            {
                return null;
            }
            //string str;
            //if (dictionary.Value.TryGetValue(key.Value, out str))
            //{
            //    return str;
            //}

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
