using System;
using System.Collections;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MS.Youtube.Downloader.Service;

namespace MS.Youtube.Playlist.Downloader
{
    public partial class MainWindow
    {
        private readonly DownloaderService _service;
        private Google.YouTube.Playlist _playlist;
        private readonly DownloadItems _downloadItems;

        public MainWindow()
        {
            InitializeComponent();
            _service = new DownloaderService();
            _downloadItems = new DownloadItems();
            _downloadItems.OnDownloadStatusChange += OnDownloadStatusChange;
            mediatype.SelectedIndex = 1;
            foldername.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\YoutubeDownloads";
            WebBrowser.Navigate(YoutubeVideoTextbox.Text);
        }

        private void OnDownloadStatusChange(DownloadItem item, DownloadStatus status)
        {
            switch (status.DownloadState)
            { 
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

        private void Button_Click_1(object sender, RoutedEventArgs e) { 
            var items = _service.GetPlaylists(username.Text);
            numFound.Content = items.Count;
            listbox.ItemsSource = items;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) { DownloadList((listbox2.SelectedItems.Count > 0) ? listbox2.SelectedItems: listbox2.Items); }

        private void Button_Click_4(object sender, RoutedEventArgs e) { DownloadList((listbox3.SelectedItems.Count > 0) ? listbox3.SelectedItems : listbox3.Items); }

        private void DownloadList(IEnumerable list)
        {
            if (_playlist == null) return;
            foreach (YoutubeEntry member in list) {
                if (member.WatchPage == null) continue;
                var item = new DownloadItem(_playlist, member.WatchPage, (MediaType) Enum.Parse(typeof (MediaType), mediatype.Text), foldername.Text);
                _downloadItems.Add(item);
            }
            _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
        }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playlist = listbox.SelectedItem as Google.YouTube.Playlist;
            if (_playlist != null)
            {
                var items = _service.GetPlaylist(_playlist);
                numFound.Content = items.Count;
                listbox2.ItemsSource = items;
            }
        }

        private void Window_Drop_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var dataString = (string) e.Data.GetData(DataFormats.StringFormat);
                Uri uri;
                if(Uri.TryCreate(dataString, UriKind.Absolute, out uri)) {
                    if (uri.Host == "www.youtube.com") {
                        var arr = uri.AbsolutePath.Substring(1).Split('/');
                        if (arr.Length >= 2 && arr[0].ToLowerInvariant() == "user") {
                            username.Text = arr[1];
                            Button_Click_1(null, null);
                        }
                    }
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _downloadItems.Clear();
            Uri uri;
            if (!Uri.TryCreate(YoutubeVideoTextbox.Text, UriKind.Absolute, out uri)) return;
            var item = new DownloadItem(uri, (MediaType)Enum.Parse(typeof(MediaType), mediatype.Text), foldername.Text);
            _downloadItems.Add(item);
            _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
        }

        private void GetPlayListItemsButton_Click(object sender, RoutedEventArgs e)
        {
            Uri uri;
            if (!Uri.TryCreate(playlistUrl.Text, UriKind.Absolute, out uri)) return;
            listbox3.ItemsSource = _service.GetPlaylist(uri); ;
        }

        private void YoutubeVideoTextbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Navigate(YoutubeVideoTextbox.Text);
            }
        }

        private void Navigate(string text)
        {
            WebBrowser.Navigate(new Uri(text));
        }

        private void WebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (YoutubeVideoTextbox.Text != e.Uri.ToString())
            {
                YoutubeVideoTextbox.Text = e.Uri.ToString();
                if (e.Uri.Host == "www.youtube.com")
                {
                    var id = _service.GetPlaylistId(e.Uri);
                    if (!String.IsNullOrEmpty(id))
                    {
                        playlistUrl.Text = e.Uri.ToString();
                        GetPlayListItemsButton_Click(null, null);
                    } else  {
                        var arr = e.Uri.AbsolutePath.Substring(1).Split('/');
                        if (arr.Length >= 2 && arr[0].ToLowerInvariant() == "user")
                        {
                            username.Text = arr[1];
                            Button_Click_1(null, null);
                        }
                    }
                }
            }
        }
    }
}
