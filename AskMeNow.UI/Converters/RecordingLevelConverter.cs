using System.Globalization;
using System.Windows.Data;

namespace AskMeNow.UI.Converters
{
    public class RecordingLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float level)
            {
                var percentage = Math.Max(0, Math.Min(1, level));
                return percentage * 200;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
