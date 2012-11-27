using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Google.YouTube;

namespace MS.Youtube.Downloader.Service
{
    public class YoutubeEntry
    {
        public string Title { get; set; }
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
            list.Add(new Playlist() {Title = "Favorites", Content = user});
            var playListFeed = request.GetPlaylistsFeed(user);
            foreach (var playlistEntry in playListFeed.Entries) 
                list.Add(playlistEntry);
            return list;
        }

        public ObservableCollection<YoutubeEntry> GetPlaylist(Playlist playlist)
        {
            var request = new YouTubeRequest(_settings);
            if (playlist.Title == "Favorites")
            {
                var list = new ObservableCollection<YoutubeEntry>();
                var items = request.GetFavoriteFeed(playlist.Content);
                foreach (var member in items.Entries)
                    if(member.WatchPage != null)
                        list.Add(new YoutubeEntry() { Title = member.Title, WatchPage = member.WatchPage });
                return list;
            }
            else
            {
                var items = request.GetPlaylist(playlist);
                var list = new ObservableCollection<YoutubeEntry>();
                foreach (var member in items.Entries)
                    if (member.WatchPage != null)
                        list.Add(new YoutubeEntry() { Title = member.Title, WatchPage = member.WatchPage });
                return list;
            }
        }
    }
}
