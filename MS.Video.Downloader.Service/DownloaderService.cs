using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Google.GData.Client;
using Google.YouTube;
using MS.Video.Downloader.Service.Youtube;

namespace MS.Video.Downloader.Service
{
    public class DownloaderService
    {
        private readonly YouTubeRequestSettings _settings;

        public DownloaderService()
        {
            _settings = new YouTubeRequestSettings(
                "MS.Youtube.Downloader",
                "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg" //key
            ) {AutoPaging = true, PageSize = 50};
        }

        public async Task<ObservableCollection<Playlist>> GetPlaylistsAsync(string userName, int startIndex = 1)
        {
            var list = new ObservableCollection<Playlist>();
            list.Add(new Playlist { Title = "Favorites", Content = userName});

            await Task.Factory.StartNew(() => {
                var request = new YouTubeRequest(_settings);
                var items = request.GetPlaylistsFeed(userName);
                foreach(var item in items.Entries) list.Add(item);
            }).ConfigureAwait(false);
            return list;
        }

        public async Task<ObservableCollection<VideoEntry>> GetPlaylistAsync(Playlist playlist)
        {
            var request = new YouTubeRequest(_settings);
            if (playlist.Title == "Favorites") {
                Feed<Google.YouTube.Video> items = null;
                await Task.Factory.StartNew(() => {
                    items = request.GetFavoriteFeed(playlist.Content);
                }).ConfigureAwait(false); 
                return GetYoutubeEntries(items);
            } else {
                Feed<PlayListMember> items = null;
                await Task.Factory.StartNew(() => {
                    items = request.GetPlaylist(playlist);
                }).ConfigureAwait(false);
                return GetYoutubeEntries(items);
            }
        }

        public async Task<ObservableCollection<VideoEntry>> GetPlaylistAsync(Uri uri)
        {
            var videoUrl = VideoUrl.Create(uri);
            var id = videoUrl.Id;
            if(String.IsNullOrEmpty(id)) return new ObservableCollection<VideoEntry>();
            var request = new YouTubeRequest(_settings);
            Feed<PlayListMember> items = null;
            await Task.Factory.StartNew(() => {
                items = request.Get<PlayListMember>(new Uri("http://gdata.youtube.com/feeds/api/playlists/" + id));
            }).ConfigureAwait(false);
            return GetYoutubeEntries(items);
        }

        private static ObservableCollection<VideoEntry> GetYoutubeEntries<T>(Feed<T> items) where T : Google.YouTube.Video, new()
        {
            var list = new ObservableCollection<VideoEntry>();
            if (items == null) return list;
            try {
                foreach (var member in items.Entries.Where(member => member.WatchPage != null)) {
                    var firstOrDefault = member.Thumbnails.FirstOrDefault(t => t.Height == "90" && t.Width == "120");
                    list.Add(new VideoEntry {
                        Title = member.Title,
                        Url = member.WatchPage.ToString(),
                        Description = member.Description,
                        ThumbnailUrl = (firstOrDefault != null) ? firstOrDefault.Url : ""
                    });
                }
            }
            catch {
                //
            }
            return list;
        }

    }
}
