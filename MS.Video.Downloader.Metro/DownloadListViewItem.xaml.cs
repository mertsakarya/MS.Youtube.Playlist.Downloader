using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MS.Video.Downloader.Service.Youtube.Dowload;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MS.Video.Downloader.Metro
{
    public sealed partial class DownloadListViewItem : UserControl
    {
        //public static DependencyProperty FeedProperty = DependencyProperty.Register("Feed", typeof(Feed), typeof(DownloadListViewItem), null); //new PropertyMetadata(null, OnItemsSourcePropertyChanged));

        //private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{

        //}

        //public Feed Feed
        //{
        //    get { return (Feed)GetValue(FeedProperty); }
        //    set { SetValue(FeedProperty, value); }
        //}

        public DownloadListViewItem()
        {
            this.InitializeComponent();
        }
    }
}
