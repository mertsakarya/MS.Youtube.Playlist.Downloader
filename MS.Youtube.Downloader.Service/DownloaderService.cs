using System;
using System.Collections.ObjectModel;
using System.Linq;
using Google.GData.Client;
using Google.YouTube;

namespace MS.Youtube.Downloader.Service
{
    public class YoutubeUrl
    {
        public Uri Uri { get; set; }
        public UrlType Type { get; set; }
        public string Id { get; set; }
        public override string ToString()
        {
            return Uri.ToString();
        }
    }

    public enum UrlType
    {
        Video,
        Playlist,
        User,
        Unknown
    }

    public class YoutubeEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public Uri WatchPage { get; set; }
        public override string ToString()
        {
            return Title;
        }
    }

    public class DownloaderService
    {
        private readonly YouTubeRequestSettings _settings;

        public DownloaderService()
        {
            _settings = new YouTubeRequestSettings(
                "MS.Youtube.Downloader",
                "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg" //key
            );
            _settings.AutoPaging = true;
            _settings.PageSize = 50;
        }

        public ObservableCollection<Playlist> GetPlaylists(string user, int startIndex = 1)
        {
            var request = new YouTubeRequest(_settings);
            var list = new ObservableCollection<Playlist>();
            list.Add(new Playlist {Title = "Favorites", Content = user});
            var items = request.GetPlaylistsFeed(user);
            foreach (var playlist in items.Entries)
            {
                list.Add(playlist);

            }
            return list;
        }

        public ObservableCollection<YoutubeEntry> GetPlaylist(Playlist playlist)
        {
            var request = new YouTubeRequest(_settings);
            if (playlist.Title == "Favorites") {
                var items = request.GetFavoriteFeed(playlist.Content);
                return GetYoutubeEntries(items);
            } else {
                var items = request.GetPlaylist(playlist);
                return GetYoutubeEntries(items);
            }
        }

        public ObservableCollection<YoutubeEntry> GetPlaylist(Uri uri)
        {
            var id = GetPlaylistId(uri);
            if(String.IsNullOrEmpty(id))return new ObservableCollection<YoutubeEntry>();
            var request = new YouTubeRequest(_settings);
            var playlist = request.Get<PlayListMember>(new Uri("http://gdata.youtube.com/feeds/api/playlists/" + id));
            return GetYoutubeEntries(playlist);
        }

        private static ObservableCollection<YoutubeEntry> GetYoutubeEntries<T>(Feed<T> items) where T : Video, new()
        {
            var list = new ObservableCollection<YoutubeEntry>();
            try {
                foreach (var member in items.Entries.Where(member => member.WatchPage != null)) {
                    var firstOrDefault = member.Thumbnails.FirstOrDefault(t => t.Height == "90" && t.Width == "120");
                    list.Add(new YoutubeEntry {
                        Title = member.Title,
                        WatchPage = member.WatchPage,
                        Description = member.Description,
                        ThumbnailUrl = (firstOrDefault != null) ? firstOrDefault.Url : ""
                    });
                }
            } catch {}
            return list;
        }

        public YoutubeUrl GetUrlType(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return new YoutubeUrl {Type = UrlType.Unknown};
            return GetUrlType(uri);
        }

        public YoutubeUrl GetUrlType(Uri uri)
        {
            var url = new YoutubeUrl {Uri = uri, Type = UrlType.Unknown};
            if (uri.Host != "www.youtube.com") return url;
            var arr = uri.AbsolutePath.Substring(1).Split('/');
            if (arr[0].ToLowerInvariant() == "user") {
                url.Id = arr[1];
                url.Type = UrlType.User;
                return url;
            }
            if (arr[0].ToLowerInvariant() == "watch") {
                url.Id = GetVideoId(uri);
                url.Type = UrlType.Video;
                return url;
            }
            url.Id = GetPlaylistId(uri);
            if (!String.IsNullOrEmpty(url.Id)) url.Type = UrlType.Playlist;
            return url;
        }

        private string GetVideoId(Uri uri)
        {
            var queryItems = uri.Query.Split('&');
            string id = "";
            if (queryItems.Length > 0 && !String.IsNullOrEmpty(queryItems[0])) {
                foreach (var queryItem in queryItems) {
                    var item = queryItem;
                    if (item[0] == '?') item = item.Substring(1);
                    if (item.Substring(0, 2).ToLowerInvariant() == "v=") {
                        id = item.Substring(2);
                        break;
                    }
                }
            }
            return id;
        }

        private string GetPlaylistId(Uri uri)
        {
            var queryItems = uri.Query.Split('&');
            string id = "";
            if (queryItems.Length > 0 && !String.IsNullOrEmpty(queryItems[0])) {
                foreach (var queryItem in queryItems) {
                    var item = queryItem;
                    if (item[0] == '?') item = item.Substring(1);
                    if (item.Substring(0, 5).ToLowerInvariant() == "list=") {
                        id = item.Substring(5);
                        if (id.Substring(0, 2).ToLowerInvariant() == "pl") id = id.Substring(2);
                        break;
                    }
                }
            }
            return id;
        }

        //public ObservableCollection<YoutubeEntry> TryGetFeed(Uri uri)
        //{
        //    var client = new WebClient();
        //    var html = client.DownloadString(uri);
        //    var doc = new HtmlDocument();
        //    doc.LoadHtml(html);
        //    var nodes = doc.DocumentNode.SelectNodes("//link[@type='application/rss+xml']");
        //    if (nodes.Count > 0)
        //    {
        //        var node = nodes[0];
        //        var link = node.GetAttributeValue("href", "");
        //        if (link != "")
        //        {
        //            var request = new YouTubeRequest(_settings);
        //            var playlist = request.Get<Google.GData.YouTube>(new Uri(link));
        //            return GetYoutubeEntries(playlist);
        //        }
        //    }
        //    return new ObservableCollection<YoutubeEntry>();
        //}
    }
}
