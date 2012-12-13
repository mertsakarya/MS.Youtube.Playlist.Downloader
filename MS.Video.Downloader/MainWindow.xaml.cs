using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MS.Video.Downloader.Service;
using MS.Video.Downloader.Service.Models;
using MS.Video.Downloader.Service.Youtube;
using Newtonsoft.Json;

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

        private void OnDownloadStatusChange(DownloadItems downloadItems, Entry entry, DownloadStatus status)
        {
            Dispatcher.Invoke(() => LogBoxLog(entry, status.DownloadState));
            switch (status.DownloadState) {
                case DownloadState.AllStart:
                    Dispatcher.Invoke(() => {
                        Log.Content = "START";
                        progressBar.Value = 0;
                        AddLog(String.Format("STARTED DOWNLOAD [{0}] WITH [{1}] FILES.", downloadItems.Guid, downloadItems.Count));
                    });
                    break;
                case DownloadState.AllFinished:
                    Dispatcher.Invoke(() => {
                        Log.Content = "DONE!";
                        progressBar.Value = 0;
                        AddLog(String.Format("FINISHED DOWNLOAD [{0}] WITH [{1}] FILES.", downloadItems.Guid, downloadItems.Count));
                        foreach (var e in downloadItems)
                            if (e.Status.DownloadState == DownloadState.Error) {
                                AddLog(String.Format("ERROR {2}[{1}]: {0}", e.Url, e.Status.UserData ?? "", (entry == null) ? "" :entry.Title ?? ""));
                            }
                        downloadItems.Clear();
                    });
                    break;
                case DownloadState.DownloadProgressChanged:
                    Dispatcher.Invoke(() => { progressBar.Value = status.Percentage; });
                    break;
            }
            if (entry != null) {
                Dispatcher.Invoke(() => {
                    string title;
                    if (entry.Title != null)
                        title = entry.Title;
                    else if (entry.Url != null)
                        title = entry.Url;
                    else
                        title = entry.Guid.ToString();
                    Log.Content = title;
                });
            }
        }

        private void LogBoxLog(Entry item, DownloadState state)
        {
            if (item == null || state == DownloadState.DownloadProgressChanged) return;
            string title;
            if (item.Title != null)
                title = item.Title;
            else if (item.Url != null)
                title = item.Url;
            else
                title = item.Guid.ToString();
            AddLog(String.Format("{1} [{0}]", title, state));
        }

        private void AddLog(string text)
        {
            if (LogBox != null)
                LogBox.Items.Insert(0, text);
        }

        private void GetPlayLists_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistsProvider.SelectedItem == null) return;
            var youtubeUserText = "http://www.youtube.com/user/" + username.Text;
            //var vimeoUserText = "http://vimeo.com/user" + username.Text;
            var entryText = //PlaylistsProvider.SelectedItem.ToString() == ContentProviderType.Vimeo.ToString()
                            //    ? vimeoUserText :
                                youtubeUserText;
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
        private void PlaylistDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadList((listbox3.SelectedItems.Count > 0) ? listbox3.SelectedItems : listbox3.Items);
        }

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
            _playlist = Entry.Create(playlistUrl.Text);
            _playlist.ParseChannelInfoFromHtml(_playlist.VideoUrl);
            _playlist.GetEntries(items => Dispatcher.Invoke(() => {
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
        private void GoToUrl_Click(object sender, RoutedEventArgs e) { Navigate(VideoTextbox.Text); }
        private void Navigate(string text) { WebBrowser.Navigate(new Uri(text)); }

        private void WebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e) { VideoTextbox.Text = e.Uri.ToString(); }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            var dataString = (string)e.Data.GetData(DataFormats.StringFormat);
            Uri uri;
            if(Uri.TryCreate(dataString, UriKind.Absolute, out uri))
                Navigate(uri.ToString());
        }

        private void PrepareUrl(string url)
        {
            var u = VideoUrl.Create(url);
            switch (u.Type) {
                case VideoUrlType.User:
                    //Tabs.SelectedIndex = 0;
                    username.Text = u.Id;
                    GetPlayLists_Click(null, null);
                    break;
                case VideoUrlType.Channel:
                    //Tabs.SelectedIndex = 1;
                    playlistUrl.Text = u.ToString();
                    GetPlayListItemsButton_Click(null, null);
                    break;
                case VideoUrlType.Video:
                    //Tabs.SelectedIndex = 2;
                    VideoTextbox.Text = u.ToString();
                    if (u is YoutubeUrl && (u as YoutubeUrl).ChannelId != "") {
                        playlistUrl.Text = u.ToString();
                        GetPlayListItemsButton_Click(null, null);
                    }
                    break;
            }
            //return u;
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
            if(UrlDownload != null && url != null)
                UrlDownload.IsEnabled = (url.Type == VideoUrlType.Video);
        }

        private void WebBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e) { SetSilent(sender as WebBrowser, true); }
        private void WebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e) { PrepareUrl(e.Uri.ToString()); }
        private void Back_Click(object sender, RoutedEventArgs e) { if (WebBrowser.CanGoBack) WebBrowser.GoBack(); }

        private static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null) return;

            // get an IWebBrowser2 from the document
            var sp = browser.Document as IOleServiceProvider;
            if (sp != null) {
                var iidIWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                var iidIWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref iidIWebBrowserApp, ref iidIWebBrowser2, out webBrowser);
                if (webBrowser != null) {
                    webBrowser.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, new object[] { silent });
                }
            }
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }

    }
}
