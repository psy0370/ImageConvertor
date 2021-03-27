using System;
using System.Globalization;
using System.Windows.Data;

namespace ImageConvertor
{
    public class InvartConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            else
            {
                throw new ArgumentException("Not boolean.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }
            else
            {
                throw new ArgumentException("Not boolean.");
            }
        }
    }
}
