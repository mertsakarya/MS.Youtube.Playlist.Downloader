using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MS.Youtube.Downloader.Service;
using MS.Youtube.Downloader.Service.Youtube;
using Newtonsoft.Json;

namespace MS.Youtube.Playlist.Downloader
{
    public partial class MainWindow
    {
        private readonly DownloaderService _service;
        private Google.YouTube.Playlist _playlist;
        private readonly DownloadItems _downloadItems;
        private readonly LocalService _settings;

        public MainWindow()
        {
            InitializeComponent();
            _settings = new LocalService(GetType().Assembly);
            _service = new DownloaderService();
            _downloadItems = new DownloadItems();
            _downloadItems.OnDownloadStatusChange += OnDownloadStatusChange;
            mediatype.SelectedIndex = 1;
            foldername.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\YoutubeDownloads";
            WebBrowser.Navigate(YoutubeVideoTextbox.Text);
            var firstTimeString = (_settings.FirstTime
                                       ? "mixpanel.track('Installed', {Version:'" + _settings.Version + "'});"
                                       : "");
            var paypalHtml = Properties.Resources.TrackerHtml
                                       .Replace("|0|", _settings.Guid.ToString())
                                       .Replace("|1|", firstTimeString)
                                       .Replace("|2|", _settings.Version);
            Paypal.NavigateToString(paypalHtml);
            Tabs.SelectedIndex = 2;
            PlaylistDownload.IsEnabled = false;
            UrlDownload.IsEnabled = false;
            PlaylistsDownload.IsEnabled = false;
        }

        private void OnDownloadStatusChange(DownloadItem item, DownloadStatus status)
        {
            switch (status.DownloadState) {
                case DownloadState.AllFinished:
                    Dispatcher.Invoke(() => { Log.Content = "DONE!"; });
                    break;
                case DownloadState.DownloadProgressChanged:
                    Dispatcher.Invoke(() => { progressBar.Value = status.Percentage; });
                    break;
            }
            if (item != null) {
                Dispatcher.Invoke(() => { Log.Content = item.VideoInfo.Title ?? ""; });
            }
        }

        private void GetPlayLists_Click(object sender, RoutedEventArgs e)
        {
            var items = _service.GetPlaylists(username.Text);
            numFound.Content = items.Count;
            listbox.ItemsSource = items;
            PlaylistsDownload.IsEnabled = false;
            MixpanelTrack("Get Playlist", new { Username = username.Text, items.Count });
        }

        private void PlaylistsDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadList((listbox2.SelectedItems.Count > 0) ? listbox2.SelectedItems : listbox2.Items);
        }

        private void PlaylistDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadList((listbox3.SelectedItems.Count > 0) ? listbox3.SelectedItems : listbox3.Items);
        }

        private void DownloadList(IEnumerable list)
        {
            if (_playlist == null) return;
            foreach (YoutubeEntry member in list) {
                if (member.WatchPage == null) continue;
                var item = new DownloadItem(_playlist, member.WatchPage, (MediaType) Enum.Parse(typeof (MediaType), mediatype.Text), foldername.Text);
                _downloadItems.Add(item);
            }
            _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
            MixpanelTrack("Download List", new {_downloadItems.Count});
        }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playlist = listbox.SelectedItem as Google.YouTube.Playlist;
            PlaylistsDownload.IsEnabled = (_playlist != null);
            if (_playlist != null) {
                var items = _service.GetPlaylist(_playlist);
                numFound.Content = items.Count;
                listbox2.ItemsSource = items;
                MixpanelTrack("Get Playlist", new {_playlist.Title, items.Count, Url = _playlist.PlaylistsEntry.AlternateUri});
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            var dataString = (string) e.Data.GetData(DataFormats.StringFormat);
            var url = _service.GetUrlType(dataString);
            switch (url.Type) {
                case YoutubeUrlType.User:
                    Tabs.SelectedIndex = 0;
                    username.Text = url.Id;
                    GetPlayLists_Click(null, null);
                    break;
                case YoutubeUrlType.Playlist:
                    Tabs.SelectedIndex = 1;
                    playlistUrl.Text = url.ToString();
                    GetPlayListItemsButton_Click(null, null);
                    break;
                case YoutubeUrlType.Video:
                    Tabs.SelectedIndex = 2;
                    YoutubeVideoTextbox.Text = url.ToString();
                    Navigate(YoutubeVideoTextbox.Text);
                    break;
            }
        }

        private void UrlDownload_Click(object sender, RoutedEventArgs e)
        {
            _downloadItems.Clear();
            Uri uri;
            if (!Uri.TryCreate(YoutubeVideoTextbox.Text, UriKind.Absolute, out uri)) return;
            var item = new DownloadItem(null, uri, (MediaType) Enum.Parse(typeof (MediaType), mediatype.Text), foldername.Text);
            _downloadItems.Add(item);
            _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
        }

        private void GetPlayListItemsButton_Click(object sender, RoutedEventArgs e)
        {
            Uri uri;
            if (!Uri.TryCreate(playlistUrl.Text, UriKind.Absolute, out uri)) return;
            var items = _service.GetPlaylist(uri);
            listbox3.ItemsSource = items;
            PlaylistDownload.IsEnabled = (items.Count > 0);
            MixpanelTrack("Get Playlist", new {Url = playlistUrl.Text, items.Count});
        }

        private void YoutubeVideoTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Navigate(YoutubeVideoTextbox.Text);
        }

        private void GoToUrl_Click(object sender, RoutedEventArgs e)
        {
            Navigate(YoutubeVideoTextbox.Text);
        }

        private void Navigate(string text)
        {
            WebBrowser.Navigate(new Uri(text));
        }

        private void WebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            var url = _service.GetUrlType(e.Uri);
            YoutubeVideoTextbox.Text = url.ToString();
            switch (url.Type) {
                case YoutubeUrlType.User:
                    username.Text = url.Id;
                    GetPlayLists_Click(null, null);
                    break;
                case YoutubeUrlType.Playlist:
                    playlistUrl.Text = url.ToString();
                    GetPlayListItemsButton_Click(null, null);
                    break;
            }
        }

        public void MixpanelTrack(string action, object obj = null)
        {
            var objText = (obj == null) ? "" : ", " + JsonConvert.SerializeObject(obj);
            var cmd = "mixpanel.track('" + action + "'" + objText + ");";
            Paypal.InvokeScript("trackEval", cmd);
        }

        private void playlistUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_service == null) return;
            var url = _service.GetUrlType(playlistUrl.Text);
            PlaylistDownload.IsEnabled = (url.Type == YoutubeUrlType.Playlist);
        }

        private void YoutubeVideoTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_service == null) return;
            var url = _service.GetUrlType(YoutubeVideoTextbox.Text);
            UrlDownload.IsEnabled = (url.Type == YoutubeUrlType.Video);
        }

    }
}
