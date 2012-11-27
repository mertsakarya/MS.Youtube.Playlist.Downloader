using System;
using System.Collections.ObjectModel;
using System.Linq;
using Google.GData.Client;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;

namespace MS.Youtube.Downloader.Service
{
    public class YoutubeEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public MediaThumbnail Thumbnail { get; set; }
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
            foreach (var playlistEntry in items.Entries) 
                list.Add(playlistEntry);
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
            var queryItems = uri.Query.Split('&');
            foreach (var queryItem in queryItems)
            {
                var item = queryItem;
                if (item[0] == '?') item = item.Substring(1);
                if (item.Substring(0, 5).ToLowerInvariant() != "list=") continue;
                var id = item.Substring(5);
                if (id.Substring(0, 2).ToLowerInvariant() == "pl") id = id.Substring(2);
                var request = new YouTubeRequest(_settings);
                var playlist = request.Get<PlayListMember>(new Uri("http://gdata.youtube.com/feeds/api/playlists/" + id));
                return GetYoutubeEntries(playlist);
            }
            return new ObservableCollection<YoutubeEntry>();
        }

        private static ObservableCollection<YoutubeEntry> GetYoutubeEntries<T>(Feed<T> items) where T : Video, new()
        {
            var list = new ObservableCollection<YoutubeEntry>();
            
            foreach (var member in items.Entries.Where(member => member.WatchPage != null))
                list.Add(new YoutubeEntry { Title = member.Title, WatchPage = member.WatchPage, Description = member.Description, 
                    Thumbnail = member.Thumbnails.FirstOrDefault(t => t.Height == "90" && t.Width == "120")});
            return list;
        }

    }
}
