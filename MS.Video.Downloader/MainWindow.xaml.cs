using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using Newtonsoft.Json;
using ms.video.downloader.service;
using ms.video.downloader.service.Download;
using ms.video.downloader.service.S3;
using mshtml;

namespace ms.video.downloader
{
    public partial class MainWindow : Window
    {
        private readonly DownloadLists Lists;
        private YoutubeUrl _youtubeUrl;
        private YoutubeEntry _youtubeEntry;
        private readonly Settings _settings;
        private readonly CacheManager _cacheManager;

        public MainWindow()
        {
            InitializeComponent();
            _settings = Settings.Instance;
            _cacheManager = CacheManager.Instance;
            Title = "MS.Video.Downloader ver. " + _settings.Version;
            Lists = new DownloadLists(OnDownloadStatusChange);
            Loading(false);
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            //DownloadStatusGrid.DataContext = Lists;
            DownloadStatusGrid.ItemsSource = Lists.Entries;
            if (!_settings.IsDevelopment) {
                var firstTimeString = (_settings.FirstTime ? "mixpanel.track('Installed', {Version:'" + _settings.Version + "'});" : "");
                var paypalHtml = Properties.Resources.TrackerHtml.Replace("|0|", _settings.ApplicationConfiguration.Guid.ToString()).Replace("|1|", firstTimeString).Replace("|2|", _settings.Version);
                Paypal.NavigateToString(paypalHtml);
            }
            var uri = new Uri(Url.Text);
            Navigate(uri);
            //Task.Factory.StartNew(() => {
            //    while (true) {
            //        IHTMLDocument3 doc = null;
            //        string url = null;
            //        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
            //            url = Url.Text;
            //            doc = Browser.Document as IHTMLDocument3;
            //        }));
            //        if (doc == null) return;
            //        var html = doc.documentElement.outerHTML;
            //        var prevCount = (_youtubeEntry == null) ? 0 : _youtubeEntry.Entries.Count;
            //        var youtubeEntry = YoutubeEntry.Create(new Uri(url), html);
            //        if (youtubeEntry != null && youtubeEntry.Entries.Count != prevCount) {
            //            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { 
            //                _youtubeEntry = youtubeEntry;
            //                Loading();
            //            }));
            //        }
            //        Thread.Sleep(500);
            //    }
            //});

        }

        private void Loading(bool show = true)
        {
            GetList.IsEnabled = false;
            GetVideo.IsEnabled = false;
            ConvertMp3.IsEnabled = false;
            GetPage.IsEnabled = false;
            GetPage.Content = String.Format(" Download Page ");
            if (!show) return;
            if (!(_youtubeEntry == null || _youtubeEntry.Entries.Count == 0)) {
                GetPage.IsEnabled = true;
                ConvertMp3.IsEnabled = true;
                GetPage.Content = String.Format(" Download Page ({0}) ", _youtubeEntry.Entries.Count);
            }
            if (_youtubeUrl == null) return;
            switch (_youtubeUrl.Type) {
                case VideoUrlType.Channel:
                    GetList.IsEnabled = true;
                    ConvertMp3.IsEnabled = true;
                    break;
                case VideoUrlType.Video:
                    if (_youtubeUrl.ChannelId != "")
                        GetList.IsEnabled = true;
                    GetVideo.IsEnabled = true;
                    ConvertMp3.IsEnabled = true;
                    break;
            }
        }

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            Url.Text = e.Uri.ToString();
            MixpanelTrack("Navigated", new {Url = e.Uri.ToString(), Guid = _settings.ApplicationConfiguration.Guid});
            _youtubeUrl = YoutubeUrl.Create(e.Uri);
            var doc = Browser.Document as IHTMLDocument3; ;
            if (doc == null) return;
            var html = doc.documentElement.outerHTML;
            _youtubeEntry = YoutubeEntry.Create(e.Uri, html);
            Loading();
        }

        private void Navigate(Uri uri)
        {
            Loading(false);
            Browser.Navigate(uri);
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack) Browser.GoBack();
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoForward) Browser.GoForward();
        } // { Browser.InvokeScript("eval", new[] { "history.go(1)" }); }

        private void Url_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return) return;
            Uri uri;
            if(Uri.TryCreate(Url.Text, UriKind.Absolute, out uri)) Navigate(uri);
        }

        private void GetVideo_Click(object sender, RoutedEventArgs e) { DownloadList(new List<YoutubeEntry>(1) { YoutubeEntry.Create(_youtubeUrl.Uri) }); }

        private void GetPage_Click(object sender, RoutedEventArgs e)
        {
            if (_youtubeEntry.Entries.Count <= 0) return;
            var list = new List<Feed>(_youtubeEntry.Entries.Count);
            list.AddRange(_youtubeEntry.Entries);
            DownloadList(list);
        }

        private void GetList_Click(object sender, RoutedEventArgs e) { YoutubeEntry.Create(_youtubeUrl.Uri).GetEntries(DownloadList); }
        
        private void DownloadList(IList list)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                if (list.Count == 0) return;
                var mediaType = (!ConvertMp3.IsChecked.HasValue) ? MediaType.Video : (ConvertMp3.IsChecked.Value) ? MediaType.Audio : MediaType.Video;
                Lists.Add(list, mediaType);
                MixpanelTrack("Download", new {Guid = _settings.ApplicationConfiguration.Guid});
            }));
        }        
        
        private void OnDownloadStatusChange(Feed downloadItems, Feed entry, DownloadState downloadState, double percentage)
        {
            try {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => UpdateStatus(downloadItems, entry, downloadState, percentage)));
            } catch {}
        }

        private void UpdateStatus(Feed downloadItems, Feed entry, DownloadState downloadState, double percentage)
        {
            try {
                switch (downloadState) {
                    case DownloadState.AllStart:
                        ProgressBar.Value = 0;
                        break;
                    case DownloadState.AllFinished:
                        Log.Text = "DONE!";
                        ProgressBar.Value = 0;
                        downloadItems.Entries.Clear();
                        return;
                    case DownloadState.UpdateCache:
                        _cacheManager.Save();
                        return;
                    case DownloadState.DownloadProgressChanged:
                        ProgressBar.Value = percentage;
                        break;
                    case DownloadState.TitleChanged:
                        MixpanelTrack("Download", new {entry.Title, Guid = _settings.ApplicationConfiguration.Guid});
                        break;
                }
                Log.Text = (entry != null) ? entry.ToString() : "";
            }
            catch {}
        }

        private void UpdatePanes(object sender, RoutedEventArgs e)
        {
            DownloadStatusGrid.Visibility = (DownloadStatusGrid.Visibility != Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
            WebViewGrid.Visibility = (DownloadStatusGrid.Visibility != Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
        }
        
        public void MixpanelTrack(string action, object obj = null)
        {
            if (_settings.IsDevelopment) return;
            var objText = (obj == null) ? "" : ", " + JsonConvert.SerializeObject(obj);
            var cmd = "mixpanel.track('" + action + "'" + objText + ");";
            Paypal.InvokeScript("trackEval", cmd);
        }
        
        #region Hide script errors

        private void Browser_Navigated(object sender, NavigationEventArgs e)
        {
            SetSilent(Browser, true);
        }

        public static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null) return;

            var sp = browser.Document as IOleServiceProvider;
            if (sp == null) return;
            var iidIWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
            var iidIWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

            object webBrowser;
            sp.QueryService(ref iidIWebBrowserApp, ref iidIWebBrowser2, out webBrowser);
            if (webBrowser != null) {
                webBrowser.GetType()
                          .InvokeMember("Silent",
                                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null,
                                        webBrowser, new object[] {silent});
            }
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid,
                             [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }

        #endregion

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Lists.UpdatePlaylists();
        }

        private void StopAllDownloads_Click(object sender, RoutedEventArgs e)
        {
            Lists.Delete();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _settings.SaveDownloadLists(Lists);
        }

        private void Browser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            Loading();
        }

    }
}
