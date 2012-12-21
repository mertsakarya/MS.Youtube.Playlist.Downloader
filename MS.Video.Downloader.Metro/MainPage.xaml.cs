using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MS.Video.Downloader.Service.Youtube;
using MS.Video.Downloader.Service.Youtube.Dowload;
using MS.Video.Downloader.Service.Youtube.Models;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MS.Video.Downloader.Metro
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private YoutubeUrl _youtubeUrl;
        private Entry _playlist;
        private readonly WebViewWrapper _webView;
        private DownloadLists lists;
        public MainPage() { 
            InitializeComponent();
            lists = new DownloadLists();
            lists.OnStatusChanged += OnDownloadStatusChange;
            _webView = new WebViewWrapper(WebBrowser);
            _webView.Navigating += (sender, args) => Loading();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) { }

        private void PrepareUrl(Uri uri)
        {
            List.ItemsSource = null;

            _youtubeUrl = VideoUrl.Create(uri) as YoutubeUrl;
            if (_youtubeUrl == null) {
                Loading(false);  
                return; 
            }
            switch (_youtubeUrl.Type) {
                case VideoUrlType.User:
                    List.SelectionMode = ListViewSelectionMode.Single;
                    GetPlaylists();
                    break;
                case VideoUrlType.Channel:
                    GetPlaylistItems();
                    break;
                case VideoUrlType.Video:
                    if (_youtubeUrl.ChannelId != "") {
                        GetPlaylistItems();
                    } else
                        Loading(false);
                    break;
                case VideoUrlType.Unknown:
                    Loading(false);
                    break;
            }
            //return u;
        }

        private void GetPlaylists()
        {
            var entry = Entry.Create(_youtubeUrl.Uri);
            entry.GetEntries(OnEntriesReady, OnYoutubeLoading);
        }

        private void GetPlaylistItems()
        {
            List.SelectionMode = ListViewSelectionMode.Multiple;
            _playlist = Entry.Create(_youtubeUrl.Uri);
            _playlist.GetEntries(OnEntriesReady, OnYoutubeLoading);
        }

        private void OnYoutubeLoading(object self, long count, long total)
        {
            if (total > 0) {
                LoadingProgressBar.Visibility = Visibility.Visible;
                LoadingProgressBar.Value = ((double) count/total)*100;
            }
        }

        private void OnEntriesReady(IList<Entry> entries)
        {
            if (entries.Count > 0) {
                List.ItemsSource = entries;
                Count.Text = entries.Count.ToString();
                Count.Visibility = Visibility.Visible;
                ItemsText.Visibility = Visibility.Visible;
            }
            else {
                Count.Visibility = Visibility.Collapsed;
                ItemsText.Visibility = Visibility.Collapsed;
            }
            Loading(false);
        }

        private void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            WebBrowser.Width = WebViewGrid.ActualWidth;
            WebBrowser.Height = WebViewGrid.ActualHeight;
            Navigate(new Uri("http://www.youtube.com/"));
        }

        private void Navigate(Uri uri)
        {
            Loading();
            WebBrowser.Navigate(uri);
        }

        private void Loading(bool value = true)
        {
            LoadingPane.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed;
            LoadingImage.IsActive = value;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            InteractionPane.Visibility = (!value) ? Visibility.Visible : Visibility.Collapsed;
            if (value) {
                Count.Visibility = Visibility.Collapsed;
                ItemsText.Visibility = Visibility.Collapsed;
                return;
            }
            GetList.Visibility = Visibility.Collapsed;
            GetVideo.Visibility = Visibility.Collapsed;
            ConvertMp3.Visibility = Visibility.Collapsed;
            IgnoreDownloaded.Visibility = Visibility.Collapsed;
            if (_youtubeUrl != null) {
                switch (_youtubeUrl.Type) {
                    case VideoUrlType.Channel:
                        GetList.Visibility = Visibility.Visible;
                        ConvertMp3.Visibility = Visibility.Visible;
                        IgnoreDownloaded.Visibility = Visibility.Visible;
                        break;
                    case VideoUrlType.Video:
                        if (_youtubeUrl.ChannelId != "")
                            GetList.Visibility = Visibility.Visible;
                        GetVideo.Visibility = Visibility.Visible;
                        ConvertMp3.Visibility = Visibility.Visible;
                        IgnoreDownloaded.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void WebViewGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WebBrowser.Width = e.NewSize.Width;
            WebBrowser.Height = e.NewSize.Height;
        }

        private void WebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            PrepareUrl(e.Uri);
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_youtubeUrl.Type == VideoUrlType.User) {
                var entry = List.SelectedItem as Entry;
                if (entry == null) return;
                Navigate(entry.Uri);
            }
            else {
                GetList.Content = (List.SelectedItems.Count > 0) ? "Download Selected" : "Download Playlist";
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e) { WebBrowser.InvokeScript("eval", new[] { "history.go(-1)" });  }
        private void GoForward_Click(object sender, RoutedEventArgs e) { WebBrowser.InvokeScript("eval", new[] { "history.go(1)" }); }

        private void GetVideo_Click(object sender, RoutedEventArgs e) { DownloadList(new List<Entry>(1) { Entry.Create(_youtubeUrl.Uri) }); }
        private void GetList_Click(object sender, RoutedEventArgs e) { DownloadList(((List.SelectedItems.Count > 0) ? List.SelectedItems : List.Items)); }
        private void DownloadList(IEnumerable list)
        {
            var mediaType = (!ConvertMp3.IsChecked.HasValue) ? MediaType.Video : (ConvertMp3.IsChecked.Value) ? MediaType.Audio : MediaType.Video;
            lists.Add(list, mediaType, IgnoreDownloaded.IsChecked ?? false);
            //MixpanelTrack("Download List", new { downloadItems.Count });
        }

        private void OnDownloadStatusChange(DownloadItems downloadItems, Entry entry, DownloadStatus status)
        {
            //LogBoxLog(entry, status.DownloadState);
            switch (status.DownloadState) {
                case DownloadState.AllStart:
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        Log.Text = "START";
                        ProgressBar.Value = 0;
                        DownloadProcessRing.IsActive = true;
                        //AddLog(String.Format("STARTED DOWNLOAD [{0}] WITH [{1}] FILES.", downloadItems.Guid, downloadItems.Count));
                    });
                    break;
                case DownloadState.AllFinished:
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        Log.Text = "DONE!";
                        DownloadProcessRing.IsActive = false;
                        ProgressBar.Value = 0;
                    });
                    downloadItems.Clear();
                    return;
                case DownloadState.DownloadProgressChanged:
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        ProgressBar.Value = status.Percentage;
                    });
                    break;
            }
            if (entry == null) return;
            string title;
            if (entry.Title != null)
                title = entry.Title;
            else if (entry.Uri != null)
                title = entry.Uri.ToString();
            else
                title = entry.Guid.ToString();
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { Log.Text = title; });
        }

    }
}
