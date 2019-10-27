using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using ImageLibrary.SearchProperty;
using ShibugakiViewer.ViewModels;
using WpfTools;

namespace ShibugakiViewer.Views.Converters
{

    public class PathContainer
    {
#if DEBUG
        private const string backSlash = "\u29F5";//"\u2216";//
        public string Name
        {
            get { return this.name.Replace("\\", backSlash); }
            set { this.name = value; }
        }
        private string name;
#else
        public string Name { get; set; }
#endif
        public string FullPath { get; set; }
    }

    public class FilePropertyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var p = value as FileProperty?;
            if (p == null)
            {
                return null;
            }

            return p.Value.GetPropertyLabel();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class BoolIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var p = value as bool?;
            if (p == null)
            {
                return 0;
            }

            return p.Value ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var p = value as int?;
            if (p == null)
            {
                return false;
            }

            return p.Value != 0;
        }
    }


    public class TagNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var key = value as int?;
            if (key == null)
            {
                return null;
            }
            var library = ((App)Application.Current).Core.Library;
            return library.Tags.GetTagValue(key.Value).Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var path = value as string;
            if (path == null)
            {
                return null;
            }
            var library = ((App)Application.Current).Core.Library;

            var list = new List<PathContainer>();

            string fullPath = "";

            var pathList = library.GetPathList(path, true, out var remain);

            foreach (var item in pathList)
            {
                var key = item.GetKey();
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                fullPath = fullPath + key;

                list.Add(new PathContainer() { Name = key, FullPath = fullPath });
            }
            if (!string.IsNullOrEmpty(remain))
            {
                fullPath = fullPath + remain;
                list.Add(new PathContainer() { Name = remain, FullPath = fullPath });
            }

            if (list.Count == 0)
            {
                list.Add(new PathContainer() { Name = path, FullPath = path });
            }

            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var path = value as string;
            if (path == null)
            {
                return null;
            }
            return System.IO.Path.GetFileName(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = MathExtensions.ToLong(value);
            if (num == null)
            {
                return null;
            }
            return FileSizeConverter.ConvertAuto(num.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RatingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as int?;
            if (num == null)
            {
                return null;
            }
            return RateConvertingHelper.ToRating(num.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as int?;

            if (num == null)
            {
                var dnum = value as double?;

                if (num == null)
                {
                    return null;
                }
                num = (int)dnum.Value;
            }
            return RateConvertingHelper.Reverse((int)num.Value);
        }
    }
    

    public class OptionPaneTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as OptionPaneType?;
            if (num == null)
            {
                return 0;
            }
            return (int)num.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as int?;
            if (num == null)
            {
                return OptionPaneType.None;
            }
            return (OptionPaneType)num.Value;
        }
    }

    public class SearchPageTabConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as TabMode?;
            if (num == null)
            {
                return 0;
            }
            return (int)num.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as int?;
            if (num == null)
            {
                return TabMode.History;
            }
            return (TabMode)num.Value;
        }
    }

    public class ComboBoxPlaceHolderVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value?.ToString();
            if (str.IsNullOrWhiteSpace())
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShortcutComboBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value?.ToString();
            if (!str.IsNullOrWhiteSpace() && str.Length == 1)
            {
                return str;
            }
            return " ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ComplexSearchModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var isOr = value as bool?;

            if (isOr.HasValue)
            {
                return FilePropertyManager.GetMatchLabel(isOr.Value);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ZeroToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as int?;
            if (num == null)
            {
                return Visibility.Collapsed;
            }
            return VisibilityHelper.Set(num.Value == 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RatingTextConverter : IValueConverter
    {

        private static Lazy<string[]> array = new Lazy<string[]>(() =>
        {
            var star= Application.Current.Resources["StarSymbol"].ToString();
            var border = Application.Current.Resources["StarBorderSymbol"].ToString();

            return Enumerable.Range(0, 6)
                .Select(c => string
                    .Join("", Enumerable.Range(0, RateConvertingHelper.Steps)
                    .Select(x => x < c ? star : border)))
                .ToArray();
        });

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as int?;
            if (num == null)
            {
                return null;
            }
            return array.Value.FromIndexOrDefault(RateConvertingHelper.ToRating(num.Value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var num = value as DateTimeOffset?;
            if (num == null)
            {
                return value;
            }
            return new DateTimeOffset(num.Value.Year, num.Value.Month, num.Value.Day,
                num.Value.Hour, num.Value.Minute, 0, num.Value.Offset);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
