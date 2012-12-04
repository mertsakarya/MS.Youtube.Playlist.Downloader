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
            Dispatcher.Invoke(() => LogBoxLog(item, status.DownloadState));
            switch (status.DownloadState) {
                case DownloadState.AllFinished:
                    Dispatcher.Invoke(() => {
                        Log.Content = "DONE!";
                        progressBar.Value = 0;
                        LogBox.Text = String.Format("Finished {1} files.\r\n{0}", LogBox.Text, _downloadItems.Count);
                        foreach(var d in _downloadItems)
                            if (d.Status.DownloadState == DownloadState.Error) {
                                LogBox.Text = String.Format("Error [{2}]: {1}\r\n{0}", LogBox.Text, d.Uri, d.Status.UserData ?? "");
                            }

                        _downloadItems.Clear();
                    });
                    break;
                case DownloadState.DownloadProgressChanged:
                    Dispatcher.Invoke(() => { progressBar.Value = status.Percentage; });
                    break;
            }
            if (item != null) {
                Dispatcher.Invoke(() => {
                    var title = "";
                    if (item.VideoInfo != null && item.VideoInfo.Title != null)
                        title = item.VideoInfo.Title;
                    else if (item.VideoInfo != null && item.VideoInfo.DownloadUrl != null)
                        title = item.VideoInfo.DownloadUrl;
                    else if (item.Uri != null)
                        title = item.Uri.ToString();
                    else
                        title = item.Guid.ToString();
                    Log.Content = title;
                });
            }
        }

        private void LogBoxLog(DownloadItem item, DownloadState state)
        {
            if (item == null || state == DownloadState.DownloadProgressChanged) return;
            var title = "";
            if (item.VideoInfo != null && item.VideoInfo.Title != null)
                title = item.VideoInfo.Title;
            else if (item.VideoInfo != null && item.VideoInfo.DownloadUrl != null)
                title = item.VideoInfo.DownloadUrl;
            else if (item.Uri != null)
                title = item.Uri.ToString();
            else
                title = item.Guid.ToString();
            if(LogBox != null && LogBox.Text != null)
                LogBox.Text = String.Format("{1} [{0}]\r\n", title, state) + LogBox.Text;
        }

        private async void GetPlayLists_Click(object sender, RoutedEventArgs e)
        {
            var items = await _service.GetPlaylistsAsync(username.Text);
            //var coll = new ObservableCollection<Google.YouTube.Playlist>();
            //foreach(var item in items)
            //    coll.Add(item);
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
                if (String.IsNullOrEmpty(member.Url)) continue;
                var item = new DownloadItem(_playlist, new Uri(member.Url), (MediaType) Enum.Parse(typeof (MediaType), mediatype.Text), foldername.Text);
                _downloadItems.Add(item);
            }
            _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
            MixpanelTrack("Download List", new {_downloadItems.Count});
        }

        private async void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playlist = listbox.SelectedItem as Google.YouTube.Playlist;
            PlaylistsDownload.IsEnabled = false;
            if (_playlist == null) return;
            var items = await _service.GetPlaylistAsync(_playlist);
            numFound.Content = items.Count;
            PlaylistsDownload.IsEnabled = items.Count > 0;
            listbox2.ItemsSource = items;
            MixpanelTrack("Get Playlist", new {_playlist.Title, items.Count, Url = _playlist.PlaylistsEntry.AlternateUri});
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

        private async void GetPlayListItemsButton_Click(object sender, RoutedEventArgs e)
        {
            Uri uri;
            if (!Uri.TryCreate(playlistUrl.Text, UriKind.Absolute, out uri)) return;
            var items = await _service.GetPlaylistAsync(uri);
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
