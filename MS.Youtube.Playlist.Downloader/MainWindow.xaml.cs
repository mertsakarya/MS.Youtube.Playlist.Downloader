using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Google.YouTube;
using MS.Youtube.Downloader.Service;

namespace MS.Youtube.Playlist.Downloader
{
    public partial class MainWindow : Window
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
        }

        private void OnDownloadStatusChange(DownloadItem item, DownloadStatus status)
        {
            switch (status.DownloadState)
            { 
                case DownloadState.AllFinished:
                    Log.Content = "DONE!";
                    break;
                case DownloadState.DownloadProgressChanged:
                    Dispatcher.Invoke(() => { progressBar.Value = status.Percentage; });
                    break;
            }
            if (item != null) {
                Dispatcher.Invoke(() => { Log.Content = item.VideoInfo.Title ?? ""; });
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e) { listbox.ItemsSource = _service.GetPlaylists(username.Text); }

        private void Button_Click_3(object sender, RoutedEventArgs e) { DownloadList((listbox2.SelectedItems.Count > 0) ? listbox2.SelectedItems: listbox2.Items); }

        private void DownloadList(IEnumerable list)
        {
            if (_playlist == null) return;
            foreach (YoutubeEntry member in list)
            {
                if (member.WatchPage != null)
                {
                    var item = new DownloadItem(_playlist, member.WatchPage,
                                                (MediaType) Enum.Parse(typeof (MediaType), mediatype.Text),
                                                foldername.Text);
                    _downloadItems.Add(item);
                }
            }
            _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
        }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playlist = listbox.SelectedItem as Google.YouTube.Playlist;
            if (_playlist != null)  listbox2.ItemsSource = _service.GetPlaylist(_playlist);
        }

        private void Window_Drop_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string dataString = (string) e.Data.GetData(DataFormats.StringFormat);
                Uri uri;
                if(Uri.TryCreate(dataString, UriKind.Absolute, out uri))
                {
                    if (uri.Host == "www.youtube.com")
                    {

                        var arr = uri.AbsolutePath.Substring(1).Split('/');
                        if (arr[0].ToLowerInvariant() == "watch")
                        {
                                Tabs.SelectedIndex = 1;
                                YoutubeVideoTextbox.Text = uri.ToString();
                                //Button_Click_2(null, null);
                        } else if (arr.Length >= 2)
                        {
                            if (arr[0].ToLowerInvariant() == "user")
                            {
                                Tabs.SelectedIndex = 0;
                                username.Text = arr[1];
                                Button_Click_1(null, null);
                            }
                        }
                    }
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _downloadItems.Clear();
            Uri uri;
            if (Uri.TryCreate(YoutubeVideoTextbox.Text, UriKind.Absolute, out uri))
            {
                var item = new DownloadItem(uri, (MediaType)Enum.Parse(typeof(MediaType), mediatype.Text), foldername.Text);
                _downloadItems.Add(item);
                _downloadItems.Download(ignoreDownloaded.IsChecked ?? false);
            }
        }
    }
}
