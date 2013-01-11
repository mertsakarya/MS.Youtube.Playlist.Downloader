using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using ms.video.downloader.service;
using ms.video.downloader.service.Dowload;

namespace ms.video.downloader
{
    public class DatabindingEntryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var presenter = value as ContentPresenter;
            if (presenter == null) return null;
            var feed = presenter.Content as Feed;
            return feed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class DatabindingPauseButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //var executionStatus = value is ExecutionStatus ? (ExecutionStatus) value : ExecutionStatus.Normal;
            //return executionStatus == ExecutionStatus.Paused ? "Continue" : "Pause";
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class DatabindingProgressBarColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = value is DownloadState ? (DownloadState) value : DownloadState.Ready;
            switch (state) {
                case DownloadState.Ready: return "DarkGreen";
                case DownloadState.ConvertAudioStart: return "Chocolate"; 
                case DownloadState.Error: return "DarkRed";
                default: return "Navy";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
