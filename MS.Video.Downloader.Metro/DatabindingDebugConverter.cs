using System;
using System.Diagnostics;
using MS.Video.Downloader.Service.Youtube.Dowload;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace MS.Video.Downloader.Metro
{
    /// <summary>
    /// This converter does nothing except breaking the
    /// debugger into the convert method
    /// </summary>
    public class DatabindingDebugConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var feed = value as Feed;
            return feed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
           
        }
    }

    public class DatabindingImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
           
        }
    }
}
