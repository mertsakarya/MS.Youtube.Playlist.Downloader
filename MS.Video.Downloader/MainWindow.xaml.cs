using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Newtonsoft.Json;
using ms.video.downloader.service;
using ms.video.downloader.service.Dowload;

namespace ms.video.downloader
{
    public partial class MainWindow : Window
    {
        public readonly DownloadLists Lists;
        private YoutubeUrl _youtubeUrl;
        private readonly LocalService _settings;

        public MainWindow()
        {
            InitializeComponent();
            _settings = new LocalService();
            Title = "MS.Video.Downloader ver. " + _settings.Version;
            Lists = new DownloadLists(_settings, OnDownloadStatusChange);
            Loading(false);
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            DownloadStatusGrid.DataContext = Lists;
            DownloadStatusGrid.ItemsSource = Lists.Entries;
            var firstTimeString = (_settings.FirstTime ? "mixpanel.track('Installed', {Version:'" + _settings.Version + "'});" : "");
            var paypalHtml = Properties.Resources.TrackerHtml.Replace("|0|", _settings.Guid.ToString()).Replace("|1|", firstTimeString).Replace("|2|", _settings.Version);
            Paypal.NavigateToString(paypalHtml);
            Navigate(new Uri("http://www.youtube.com/"));
        }

        private void Loading(bool show = true)
        {
            GetList.Visibility = Visibility.Collapsed;
            GetVideo.Visibility = Visibility.Collapsed;
            ConvertMp3.Visibility = Visibility.Collapsed;
            if (!show) return;
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

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            Url.Text = e.Uri.ToString();
            MixpanelTrack("Navigated", new {Url = e.Uri.ToString(), _settings.Guid});
            _youtubeUrl = YoutubeUrl.Create(e.Uri);
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
        
        private void GetList_Click(object sender, RoutedEventArgs e) { YoutubeEntry.Create(_youtubeUrl.Uri).GetEntries(DownloadList); }
        
        private void DownloadList(IList list)
        {
            if (list.Count == 0) return;
            var mediaType = (!ConvertMp3.IsChecked.HasValue) ? MediaType.Video : (ConvertMp3.IsChecked.Value) ? MediaType.Audio : MediaType.Video;
            Lists.Add(list, mediaType);
            MixpanelTrack("Download", new { _settings.Guid });
        }        
        
        private void OnDownloadStatusChange(Feed downloadItems, Feed entry, DownloadState downloadState, double percentage)
        { try { Dispatcher.Invoke(() => UpdateStatus(downloadItems, entry, downloadState, percentage)); } catch {} }

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
                    case DownloadState.DownloadProgressChanged:
                        ProgressBar.Value = percentage;
                        break;
                    case DownloadState.TitleChanged:
                        MixpanelTrack("Download", new {entry.Title, _settings.Guid});
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

    }
}
