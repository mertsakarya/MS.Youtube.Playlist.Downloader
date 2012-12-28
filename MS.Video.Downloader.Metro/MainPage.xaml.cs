using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Video.Downloader.Service.Youtube;
using MS.Video.Downloader.Service.Youtube.Dowload;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        private YoutubeEntry _playlist;
        private readonly WebViewWrapper _webView;
        private readonly Feed _lists;
        public MainPage() { 
            InitializeComponent();
            _lists = new DownloadLists(OnDownloadStatusChange);
            _webView = new WebViewWrapper(WebBrowser);
            _webView.Navigating += (sender, args) => Loading();
            DownloadStatusGrid.DataContext = _lists;
            DownloadStatusGrid.ItemsSource = _lists.Entries;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) { }

        private void PrepareUrl(Uri uri)
        {
            List.ItemsSource = null;

            _youtubeUrl = YoutubeUrl.Create(uri);
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
            var entry = YoutubeEntry.Create(_youtubeUrl.Uri);
            entry.GetEntries(OnEntriesReady, OnYoutubeLoading);
        }

        private void GetPlaylistItems()
        {
            List.SelectionMode = ListViewSelectionMode.Multiple;
            _playlist = YoutubeEntry.Create(_youtubeUrl.Uri);
            _playlist.GetEntries(OnEntriesReady, OnYoutubeLoading);
        }

        private void OnYoutubeLoading(long count, long total)
        {
            if (total > 0) {
                LoadingProgressBar.Visibility = Visibility.Visible;
                LoadingProgressBar.Value = ((double) count/total)*100;
            }
        }

        private void OnEntriesReady(ObservableCollection<Feed> entries)
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
            if (_youtubeUrl != null) {
                switch (_youtubeUrl.Type) {
                    case VideoUrlType.Channel:
                        GetList.Visibility = Visibility.Visible;
                        ConvertMp3.Visibility = Visibility.Visible;
                        break;
                    case VideoUrlType.Video:
                        if (_youtubeUrl.ChannelId != "")
                            GetList.Visibility = Visibility.Visible;
                        GetVideo.Visibility = Visibility.Visible;
                        ConvertMp3.Visibility = Visibility.Visible;
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
            Url.Text = e.Uri.ToString();
            PrepareUrl(e.Uri);
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_youtubeUrl.Type == VideoUrlType.User) {
                var entry = List.SelectedItem as YoutubeEntry;
                if (entry == null) return;
                Navigate(entry.Uri);
            }
            else {
                GetList.Content = (List.SelectedItems.Count > 0) ? "Download Selected" : "Download Playlist";
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e) { WebBrowser.InvokeScript("eval", new[] { "history.go(-1)" });  }
        private void GoForward_Click(object sender, RoutedEventArgs e) { WebBrowser.InvokeScript("eval", new[] { "history.go(1)" }); }

        private void GetVideo_Click(object sender, RoutedEventArgs e) { DownloadList(new List<YoutubeEntry>(1) { YoutubeEntry.Create(_youtubeUrl.Uri) }); }
        private void GetList_Click(object sender, RoutedEventArgs e) { DownloadList(((List.SelectedItems.Count > 0) ? List.SelectedItems : List.Items)); }
        private void DownloadList(IEnumerable list)
        {
            var mediaType = (!ConvertMp3.IsChecked.HasValue) ? MediaType.Video : (ConvertMp3.IsChecked.Value) ? MediaType.Audio : MediaType.Video;
            var downloadLists = _lists as DownloadLists;
            if(downloadLists != null)
                downloadLists.Add(list, mediaType, false);
            //MixpanelTrack("Download List", new { downloadItems.Count });
        }

        private void OnDownloadStatusChange(Feed downloadItems, Feed entry, DownloadState downloadState, double percentage)
        {
            switch (downloadState) {
                case DownloadState.AllStart:
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        Log.Text = "START";
                        ProgressBar.Value = 0;
                        DownloadProcessRing.IsActive = true;
                    });
                    break;
                case DownloadState.AllFinished:
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        Log.Text = "DONE!";
                        DownloadProcessRing.IsActive = false;
                        ProgressBar.Value = 0;
                    });
                    downloadItems.Entries.Clear();
                    return;
                case DownloadState.DownloadProgressChanged:
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        ProgressBar.Value = percentage;
                    });
                    break;
            }
            if (entry != null) 
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { Log.Text = entry.ToString(); });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DownloadStatusGrid.Visibility = (DownloadStatusGrid.Visibility == Visibility.Collapsed)
                                            ? Visibility.Visible
                                            : Visibility.Collapsed;
            WebViewGrid.Visibility = (DownloadStatusGrid.Visibility == Visibility.Collapsed)
                                            ? Visibility.Visible
                                            : Visibility.Collapsed;

        }

        private void Url_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if(e.Key == VirtualKey.Enter) WebBrowser.Navigate(new Uri(Url.Text));
        }

    }
}
