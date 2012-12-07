using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MS.Video.Downloader.Service;
using MS.Video.Downloader.Service.Models;
using MS.Video.Downloader.Service.Youtube;
using Newtonsoft.Json;
using Vimeo.API;

namespace MS.Video.Downloader
{
    public partial class MainWindow
    {
        private Entry _playlist;
        private readonly LocalService _settings;

        public MainWindow()
        {
            InitializeComponent();
            _settings = new LocalService(GetType().Assembly);
            mediatype.SelectedIndex = 1;
            foldername.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\MS.Video.Downloader";
            WebBrowser.Navigate(VideoTextbox.Text);
            var firstTimeString = (_settings.FirstTime
                                       ? "mixpanel.track('Installed', {Version:'" + _settings.Version + "'});"
                                       : "");
            var paypalHtml = Properties.Resources.TrackerHtml
                                       .Replace("|0|", _settings.Guid.ToString())
                                       .Replace("|1|", firstTimeString)
                                       .Replace("|2|", _settings.Version);
            var contentProviders = Enum.GetNames(typeof(ContentProviderType));
            PlaylistsProvider.ItemsSource = contentProviders;
            PlaylistsProvider.Text = ContentProviderType.Youtube.ToString();

            Paypal.NavigateToString(paypalHtml);
            Tabs.SelectedIndex = 2;
            PlaylistDownload.IsEnabled = false;
            UrlDownload.IsEnabled = false;
            PlaylistsDownload.IsEnabled = false;
        }

        private void OnDownloadStatusChange(DownloadItems downloadItems, DownloadItem item, DownloadStatus status)
        {
            Dispatcher.Invoke(() => LogBoxLog(item, status.DownloadState));
            switch (status.DownloadState) {
                case DownloadState.AllFinished:
                    Dispatcher.Invoke(() => {
                        Log.Content = "DONE!";
                        progressBar.Value = 0;
                        LogBox.Text = String.Format("Finished {1} files.\r\n{0}", LogBox.Text, downloadItems.Count);
                        foreach(var d in downloadItems)
                            if (d.Status.DownloadState == DownloadState.Error) {
                                LogBox.Text = String.Format("Error [{2}]: {1}\r\n{0}", LogBox.Text, d.Uri, d.Status.UserData ?? "");
                            }

                        downloadItems.Clear();
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

        private void GetPlayLists_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistsProvider.SelectedItem == null) return;
            var youtubeUserText = "http://www.youtube.com/user/" + username.Text;
            var vimeoUserText = "http://vimeo.com/" + username.Text;
            var entryText = PlaylistsProvider.SelectedItem.ToString() == ContentProviderType.Vimeo.ToString()
                                ? vimeoUserText
                                : youtubeUserText;
            var entry = Entry.Create(entryText);
            entry.GetEntries(items => Dispatcher.Invoke(() => {
                numFound.Content = items.Count;
                if (items.Count <= 0) return;
                listbox.ItemsSource = items;
                PlaylistsDownload.IsEnabled = false;
                MixpanelTrack("Get Playlist", new {Username = username.Text, items.Count});
            }));
        }

        private void PlaylistsDownload_Click(object sender, RoutedEventArgs e) { DownloadList((listbox2.SelectedItems.Count > 0) ? listbox2.SelectedItems : listbox2.Items); }
        private void PlaylistDownload_Click(object sender, RoutedEventArgs e) { DownloadList((listbox3.SelectedItems.Count > 0) ? listbox3.SelectedItems : listbox3.Items); }

        private void DownloadList(IList list)
        {
            if (_playlist == null) return;
            var downloadItems = new DownloadItems((MediaType) Enum.Parse(typeof (MediaType), mediatype.Text), foldername.Text, OnDownloadStatusChange);
            foreach (var member in list.Cast<Entry>().Where(member => !String.IsNullOrEmpty(member.Url))) 
                downloadItems.Add(member);
            downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
            MixpanelTrack("Download List", new {downloadItems.Count});
        }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playlist = listbox.SelectedItem as Entry;
            PlaylistsDownload.IsEnabled = false;
            if (_playlist == null) return;
            _playlist.GetEntries(items => Dispatcher.Invoke(() => {
                numFound.Content = items.Count;
                if (items.Count <= 0) return;
                PlaylistsDownload.IsEnabled = true;
                listbox2.ItemsSource = items;
                MixpanelTrack("Get Playlist", new { _playlist.Title, items.Count, _playlist.Url });
            }));
        }

        private void UrlDownload_Click(object sender, RoutedEventArgs e)
        {
            var downloadItems = new DownloadItems((MediaType)Enum.Parse(typeof(MediaType), mediatype.Text), foldername.Text, OnDownloadStatusChange) { Entry.Create(VideoTextbox.Text) };
            downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
        }

        private void GetPlayListItemsButton_Click(object sender, RoutedEventArgs e)
        {
            Entry.Create(playlistUrl.Text).GetEntries(items => Dispatcher.Invoke(() => {
                listbox3.ItemsSource = items;
                PlaylistDownload.IsEnabled = (items.Count > 0);
                MixpanelTrack("Get Playlist", new {Url = playlistUrl.Text, items.Count});
            }));
        }

        private void VideoTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Navigate(VideoTextbox.Text);
        }

        private void GoToUrl_Click(object sender, RoutedEventArgs e)
        {
            Navigate(VideoTextbox.Text);
        }

        private void Navigate(string text)
        {
            WebBrowser.Navigate(new Uri(text));
        }

        private void WebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            PrepareUrl(e.Uri.ToString());
            Tabs.SelectedIndex = 2;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            var dataString = (string)e.Data.GetData(DataFormats.StringFormat);
            var videoUrl = PrepareUrl(dataString);
            if(videoUrl.Type == VideoUrlType.Video)
                Navigate(videoUrl.Uri.ToString());
        }

        private VideoUrl PrepareUrl(string url)
        {
            var u = VideoUrl.Create(url);
            switch (u.Type) {
                case VideoUrlType.User:
                    Tabs.SelectedIndex = 0;
                    username.Text = u.Id;
                    GetPlayLists_Click(null, null);
                    break;
                case VideoUrlType.Channel:
                    Tabs.SelectedIndex = 1;
                    playlistUrl.Text = u.ToString();
                    GetPlayListItemsButton_Click(null, null);
                    break;
                case VideoUrlType.Video:
                    Tabs.SelectedIndex = 2;
                    VideoTextbox.Text = u.ToString();
                    break;
            }
            return u;
        }

        public void MixpanelTrack(string action, object obj = null)
        {
            var objText = (obj == null) ? "" : ", " + JsonConvert.SerializeObject(obj);
            var cmd = "mixpanel.track('" + action + "'" + objText + ");";
            Paypal.InvokeScript("trackEval", cmd);
        }

        private void playlistUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            var url = VideoUrl.Create(playlistUrl.Text);
            if(PlaylistDownload != null)
                PlaylistDownload.IsEnabled = (url.Type == VideoUrlType.Channel);
        }

        private void VideoTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var url = VideoUrl.Create(VideoTextbox.Text);
            if(UrlDownload != null)
                UrlDownload.IsEnabled = (url.Type == VideoUrlType.Video);
        }

    }
}
