using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.GData.Client;
using Google.YouTube;
using MS.Video.Downloader.Service.Youtube;
using Vimeo.API;

namespace MS.Video.Downloader.Service.Models
{
    public delegate void EntriesReady(IList<Entry> entries);

    public class Entry
    {
        private string _url;
        private readonly YouTubeRequestSettings _settings;
        private readonly VimeoClient _vimeoClient;

        private Entry(Entry parent = null)
        {
            Parent = parent;
            _settings = new YouTubeRequestSettings(
                "MS.Youtube.Downloader",
                "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg" //key
                ) {AutoPaging = true, PageSize = 50};
            _vimeoClient = new VimeoClient("0511c1f34b14b62200a3b6fc3db5488d1ffaa1d3", "5ef0dc0e1e161b7185b8de6e980a3921d539e17e");
        }

        public static Entry Create(string url, Entry parent = null)
        {
            var entry = new Entry(parent) {Url = url};
            return entry;
        }

        public Entry Parent { get; private set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Content { get; set; }

        public string Url
        {
            get { return _url; }
            set { _url = value;
                VideoUrl = VideoUrl.Create(_url);
            }
        }

        public string MediaUrl { get; set; }

        public VideoUrl VideoUrl { get; private set; }

        public override string ToString()
        {
            return Title;
        }

        public void GetEntries(EntriesReady onEntriesReady)
        {
            switch (VideoUrl.Type) {
                case VideoUrlType.Channel:
                    FillEntriesChannel(onEntriesReady);
                    break;
                case VideoUrlType.User:
                    FillEntriesUser(onEntriesReady);
                    break;
            }
        }

        private void FillEntriesUser(EntriesReady onEntriesReady)
        {
            switch (VideoUrl.Provider) {
                case ContentProviderType.Vimeo:
                    FillEntriesVimeoUser(onEntriesReady);
                    break;
                case ContentProviderType.Youtube:
                    FillEntriesYoutubeUser(onEntriesReady);
                    break;
            }
        }

        private void FillEntriesChannel(EntriesReady onEntriesReady)
        {
            switch (VideoUrl.Provider) {
                case ContentProviderType.Vimeo:
                    FillEntriesVimeoChannel(onEntriesReady);
                    break;
                case ContentProviderType.Youtube:
                    FillEntriesYoutubeChannel(onEntriesReady);
                    break;
            }
        }

        private void FillEntriesVimeoUser(EntriesReady onEntriesReady) { }

        private void FillEntriesVimeoChannel(EntriesReady onEntriesReady) { }

        private async void FillEntriesYoutubeUser(EntriesReady onEntriesReady)
        {
            await Task.Factory.StartNew(() => {
                var request = new YouTubeRequest(_settings);
                var items = request.Get<Playlist>(new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/playlists?v=2", VideoUrl.Id)));
                if (items == null) return;
                var entries = new List<Entry>();
                entries.Add(new Entry(this) { Title = "Favorites", Content = VideoUrl.Id, VideoUrl = VideoUrl.Create(VideoUrl.Id, VideoUrl.Provider, VideoUrlType.Channel)});
                foreach (var member in items.Entries) {
                    entries.Add(new Entry(this) {
                        Title = member.Title,
                        Url = member.PlaylistsEntry.AlternateUri.ToString(),
                        Description = member.Summary,
                    });
                }
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }

        private async void FillEntriesYoutubeChannel(EntriesReady onEntriesReady)
        {
            if (Title == "Favorites") {
                FillEntriesYoutubeFavorites(onEntriesReady);
                return;
            }
            await Task.Factory.StartNew(() => {
                var request = new YouTubeRequest(_settings);
                var items = request.Get<PlayListMember>(new Uri("http://gdata.youtube.com/feeds/api/playlists/" + VideoUrl.Id));
                if (items == null) return;
                var entries = new List<Entry>();
                foreach (var member in items.Entries.Where(member => member.WatchPage != null)) {
                    var firstOrDefault = member.Thumbnails.FirstOrDefault(t => t.Height == "90" && t.Width == "120");
                    entries.Add(new Entry(this) {
                        Title = member.Title,
                        Url = member.WatchPage.ToString(),
                        Description = member.Description,
                        ThumbnailUrl = (firstOrDefault != null) ? firstOrDefault.Url : "",
                        Content = member.Content
                    });
                }
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }

        private async void FillEntriesYoutubeFavorites(EntriesReady onEntriesReady)
        {
            await Task.Factory.StartNew(() => {
                var request = new YouTubeRequest(_settings);
                var items = request.Get<PlayListMember>(new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/favorites", Content)));
                if (items == null) return;
                var entries = new List<Entry>();
                foreach (var member in items.Entries.Where(member => member.WatchPage != null)) {
                    var firstOrDefault = member.Thumbnails.FirstOrDefault(t => t.Height == "90" && t.Width == "120");
                    entries.Add(new Entry(this) {
                        Title = member.Title,
                        Url = member.WatchPage.ToString(),
                        Description = member.Description,
                        ThumbnailUrl = (firstOrDefault != null) ? firstOrDefault.Url : "",
                        Content = member.Content
                    });
                }
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }
    }
}