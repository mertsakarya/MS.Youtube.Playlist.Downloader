using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ms.video.downloader.service;
using ms.video.downloader.service.Dowload;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        private YoutubeUrl _youtubeUrl;
        private YoutubeEntry _playlist;
        private readonly Feed _lists;

        public MainWindow()
        {
            InitializeComponent();
            _lists = new DownloadLists(OnDownloadStatusChange);
            //DownloadStatusGrid.DataContext = _lists;
            //DownloadStatusGrid.ItemsSource = _lists.Entries;
            Navigate(new Uri("http://www.youtube.com/"));

        }

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
                    GetItems(SelectionMode.Single);
                    break;
                case VideoUrlType.Channel:
                    GetItems();
                    break;
                case VideoUrlType.Video:
                    if (_youtubeUrl.ChannelId != "") {
                        GetItems();
                    } else
                        Loading(false);
                    break;
                case VideoUrlType.Unknown:
                    Loading(false);
                    break;
            }
        }

        private void GetItems(SelectionMode mode = SelectionMode.Multiple)
        {
            _playlist = YoutubeEntry.Create(_youtubeUrl.Uri);
            _playlist.GetEntries(OnEntriesReady, OnYoutubeLoading);
        }

        private void OnYoutubeLoading(long count, long total)
        {
            if (total <= 0) return;
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.Value = ((double)count / total) * 100;
        }

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            Url.Text = e.Uri.ToString();
            PrepareUrl(e.Uri);
        }

        private void OnEntriesReady(ObservableCollection<Feed> entries)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                if (entries.Count > 0) {
                    List.ItemsSource = entries;
                    Count.Text = entries.Count.ToString(CultureInfo.InvariantCulture);
                    Count.Visibility = Visibility.Visible;
                    ItemsText.Visibility = Visibility.Visible;
                } else {
                    Count.Visibility = Visibility.Collapsed;
                    ItemsText.Visibility = Visibility.Collapsed;
                }
                Loading(false);
            }));
        }

        private void Navigate(Uri uri)
        {
            Loading();
            Browser.Navigate(uri);
        }

        private void Loading(bool value = true)
        {
            LoadingPane.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed;
            //LoadingImage.IsActive = value;
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

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_youtubeUrl.Type == VideoUrlType.User) {
                var entry = List.SelectedItem as YoutubeEntry;
                if (entry == null) return;
                Navigate(entry.Uri);
            } else {
                GetList.Content = (List.SelectedItems.Count > 0) ? "Download Selected" : "Download Playlist";
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e) { if (Browser.CanGoBack) Browser.GoBack(); }
        private void GoForward_Click(object sender, RoutedEventArgs e) { if (Browser.CanGoForward) Browser.GoForward(); } // { Browser.InvokeScript("eval", new[] { "history.go(1)" }); }

        private void GetVideo_Click(object sender, RoutedEventArgs e) { DownloadList(new List<YoutubeEntry>(1) { YoutubeEntry.Create(_youtubeUrl.Uri) }); }
        private void GetList_Click(object sender, RoutedEventArgs e) { DownloadList(((List.SelectedItems.Count > 0) ? List.SelectedItems : List.Items)); }
        private void DownloadList(IEnumerable list)
        {
            var mediaType = (!ConvertMp3.IsChecked.HasValue) ? MediaType.Video : (ConvertMp3.IsChecked.Value) ? MediaType.Audio : MediaType.Video;
            var downloadLists = _lists as DownloadLists;
            if (downloadLists != null)
                downloadLists.Add(list, mediaType, false);
            //MixpanelTrack("Download List", new { downloadItems.Count });
        }

        private void OnDownloadStatusChange(Feed downloadItems, Feed entry, DownloadState downloadState, double percentage)
        {
            Dispatcher.Invoke(new Action(() => {
                switch (downloadState) {
                    case DownloadState.AllStart:
                        Log.Text = "START";
                        ProgressBar.Value = 0;
                        break;
                    case DownloadState.AllFinished:
                        Log.Text = "DONE!";
                        ProgressBar.Value = 0;
                        downloadItems.Entries.Clear();
                        return;
                    case DownloadState.DownloadProgressChanged:
                        ProgressBar.Value = percentage;
                        break;
                }
                if (entry != null)
                    Log.Text = entry.ToString();
            }));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //DownloadStatusGrid.Visibility = (DownloadStatusGrid.Visibility == Visibility.Collapsed)
            //                                ? Visibility.Visible
            //                                : Visibility.Collapsed;
            //WebViewGrid.Visibility = (DownloadStatusGrid.Visibility == Visibility.Collapsed)
            //                                ? Visibility.Visible
            //                                : Visibility.Collapsed;

        }

    }
}
