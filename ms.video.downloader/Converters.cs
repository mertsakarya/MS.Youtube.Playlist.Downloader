using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using ms.video.downloader.service.Dowload;

namespace ms.video.downloader
{
    public class DatabindingEntryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var presenter = value as ContentPresenter;
            var feed = presenter.Content as Feed;
            return feed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
