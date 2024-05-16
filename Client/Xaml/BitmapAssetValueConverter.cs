using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HWM.Tools.Firebase.WPF.Xaml
{
    public class BitmapAssetValueConverter : IValueConverter
    {
        public static BitmapAssetValueConverter Instance = new();

        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is string rawUri && targetType == typeof(ImageSource))
            {
                if (rawUri.StartsWith("pack://") || File.Exists(rawUri))
                    return new BitmapImage(new Uri(rawUri));
            }

            throw new NotSupportedException();
        }

        public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
