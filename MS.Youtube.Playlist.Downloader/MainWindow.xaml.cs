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
            var s = @"<html><head></head><body><form action='https://www.paypal.com/cgi-bin/webscr' method='post' target='_blank'>
<input type='hidden' name='cmd' value='_s-xclick'>
<input type='hidden' name='hosted_button_id' value='C79LKUFBTF66N'>
<input type='image' src='https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
<img alt='' border='0' src='https://www.paypalobjects.com/en_US/i/scr/pixel.gif' width='1' height='1'>
</form><script type='text/javascript'>
var _gaq = _gaq || [];
_gaq.push(['_setAccount', 'UA-36629489-1']);
_gaq.push(['_setDomainName', 'mertsakarya.com']);
_gaq.push(['_trackPageview']);
(function() {
var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
})();
</script>
<!-- start Mixpanel --><script type='text/javascript'>(function(c,a){window.mixpanel=a;var b,d,h,e;b=c.createElement('script');b.type='text/javascript';b.async=!0;b.src=('https:'===c.location.protocol?'https:':'http:')+'//cdn.mxpnl.com/libs/mixpanel-2.1.min.js';d=c.getElementsByTagName('script')[0];d.parentNode.insertBefore(b,d);a._i=[];a.init=function(b,c,f){function d(a,b){var c=b.split('.');2==c.length&&(a=a[c[0]],b=c[1]);a[b]=function(){a.push([b].concat(Array.prototype.slice.call(arguments,0)))}}var g=a;'undefined'!==typeof f?
g=a[f]=[]:f='mixpanel';g.people=g.people||[];h='disable track track_pageview track_links track_forms register register_once unregister identify name_tag set_config people.identify people.set people.increment'.split(' ');for(e=0;e<h.length;e++)d(g,h[e]);a._i.push([b,c,f])};a.__SV=1.1})(document,window.mixpanel||[]);
mixpanel.init('57dbc5e73fac491d3412da1aa74b0295');</script><!-- end Mixpanel -->
<script type='text/javascript'>mixpanel.track('App run');
function track(prm) {mixpanel.track(prm);} 
function trackUser(prm) {mixpanel.track('GetUserPlaylists',{'username':prm});} 
function trackPlaylist(prm) {mixpanel.track('GetPlaylist',{'playlistName':prm});}
</script>
</body></html>
";
            Paypal.NavigateToString(s);
            Tabs.SelectedIndex = 2;

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
            MixpanelTrackUserName(username.Text);
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
            MixpanelTrack("DownloadList");
        }

        private void MixpanelTrack(string action) { Paypal.InvokeScript("track", action); }
        private void MixpanelTrackUserName(string name) { Paypal.InvokeScript("trackUser", name); }
        private void MixpanelTrackPlaylist(string name) { Paypal.InvokeScript("trackPlaylist", name); }

        private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playlist = listbox.SelectedItem as Google.YouTube.Playlist;
            if (_playlist != null)
            {
                var items = _service.GetPlaylist(_playlist);
                numFound.Content = items.Count;
                listbox2.ItemsSource = items;
                MixpanelTrackPlaylist(_playlist.Title);

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
