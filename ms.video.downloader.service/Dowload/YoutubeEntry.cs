using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ms.video.downloader.service.MSYoutube;

namespace ms.video.downloader.service.Dowload
{

    public delegate void EntriesReady(ObservableCollection<Feed> entries);

    public delegate void EntryDownloadStatusEventHandler(Feed feed, DownloadState downloadState, double percentage);

    public class YoutubeEntry : Feed
    {
        private readonly MSYoutubeSettings _settings;
        private Uri _uri;

        [JsonIgnore]
        public YoutubeEntry Parent { get; private set; }
        [JsonIgnore]
        public string VideoExtension { get; set; }
        public string[] ThumbnailUrls { get; set; }
        [JsonIgnore]
        public YoutubeUrl YoutubeUrl { get; protected set; }
        [JsonIgnore]
        public StorageFolder BaseFolder { get; set; }
        [JsonIgnore]
        public StorageFolder ProviderFolder { get; set; }
        [JsonIgnore]
        public StorageFolder VideoFolder { get; set; }
        [JsonIgnore]
        public StorageFolder DownloadFolder { get; set; }
        [JsonIgnore]
        public MediaType MediaType { get; set; }
        [JsonIgnore]
        public string ChannelName { get { return Parent == null ? "" : Parent.Title; } }
        [JsonIgnore]
        public EntryDownloadStatusEventHandler OnEntryDownloadStatusChange;
        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; if (value != null) YoutubeUrl = YoutubeUrl.Create(_uri); }
        }

        private YoutubeEntry(YoutubeEntry parent = null)
        {
            Parent = parent;
            _settings = new MSYoutubeSettings( "MS.Youtube.Downloader", "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg" ) {AutoPaging = true, PageSize = 50};
        }

        #region Convert to MP3


        #endregion

        #region GetEntries

        public void GetEntries(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading = null)
        {
            Task.Factory.StartNew(() => {
                if (YoutubeUrl.Type == VideoUrlType.Channel || YoutubeUrl.ChannelId != "" || YoutubeUrl.FeedId != "")
                    FillEntriesChannel(onEntriesReady, onYoutubeLoading);
                else if (YoutubeUrl.Type == VideoUrlType.User)
                    FillEntriesUser(onEntriesReady, onYoutubeLoading);
            });
        }

        private void FillEntriesUser(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            var youtubeUrl = YoutubeUrl;
            var request = new MSYoutubeRequest(_settings);
            var uri = new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/playlists?v=2", youtubeUrl.UserId));
            var items = request.GetAsync(YoutubeUrl, uri, onYoutubeLoading);
            if (items == null) return;
            Entries = new ObservableCollection<Feed>();
            try {
                if (!String.IsNullOrEmpty(items.AuthorId)) {
                    var favoritesEntry = new YoutubeEntry(this) {
                        Title = "Favorite Videos",
                        Uri = new Uri("http://www.youtube.com/playlist?list=FL" + items.AuthorId),
                    };
                    Entries.Add(favoritesEntry);
                }
                foreach (var member in items.Entries) {
                    var entry = new YoutubeEntry(this) {
                        Title = member.Title,
                        Uri = member.Uri,
                        Description = member.Description
                    };
                    Entries.Add(entry);
                }
            } catch {
                Entries.Clear();
            }
            if (onEntriesReady != null) onEntriesReady(Entries);
        }

        private void FillEntriesChannel(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            var url = "";
            if (!String.IsNullOrEmpty(YoutubeUrl.ChannelId)) 
                url = "https://gdata.youtube.com/feeds/api/playlists/" + YoutubeUrl.ChannelId;
            else if (!String.IsNullOrEmpty(YoutubeUrl.FeedId))
                url = String.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads", YoutubeUrl.FeedId);
            if (url.Length <= 0) return;

            try {
                var request = new MSYoutubeRequest(_settings);
                var items = request.GetAsync(YoutubeUrl, new Uri(url), onYoutubeLoading);
                if (items == null)
                    Entries = new ObservableCollection<Feed>();
                else {
                    if (String.IsNullOrEmpty(Title)) Title = items.Title;
                    Entries = GetMembers(items);
                }
            } catch {
                Entries = new ObservableCollection<Feed>();
            }
            if (onEntriesReady != null) onEntriesReady(Entries);
        }

        private ObservableCollection<Feed> GetMembers(MSYoutubeEntry items)
        {
            var entries = new ObservableCollection<Feed>();
            foreach (var member in items.Entries) {
                if (member.Uri == null) continue;
                var thumbnailUrl = "";
                var thumbnailUrls = new List<string>(member.Thumbnails.Count);
                foreach (var tn in member.Thumbnails) {
                    thumbnailUrls.Add(tn.Url);
                    if (tn.Height == "90" && tn.Width == "120")
                        thumbnailUrl = tn.Url;
                }
                entries.Add(new YoutubeEntry(this) {
                    Title = member.Title,
                    Uri = member.Uri,
                    Description = member.Description,
                    ThumbnailUrl = thumbnailUrl
                });
            }
            return entries;
        }

        #endregion

        public void DownloadAsync(MediaType mediaType)
        {
            if (ExecutionStatus == ExecutionStatus.Deleted) { Delete(); return; }
            // Now Download Async calls this
            // UpdateStatus(DownloadState.DownloadStart, 0.0);
            MediaType = mediaType;
            BaseFolder = KnownFolders.VideosLibrary;
            ProviderFolder = DownloadHelper.GetFolder(BaseFolder, Enum.GetName(typeof(ContentProviderType), YoutubeUrl.Provider));
            VideoFolder = DownloadHelper.GetFolder(ProviderFolder, DownloadHelper.GetLegalPath(ChannelName));

            if (MediaType == MediaType.Audio) {
                var audioFolder = KnownFolders.MusicLibrary;
                ProviderFolder = DownloadHelper.GetFolder(audioFolder, Enum.GetName(typeof(ContentProviderType), YoutubeUrl.Provider));
                DownloadFolder = DownloadHelper.GetFolder(ProviderFolder, DownloadHelper.GetLegalPath(ChannelName));
            }

            var videoInfos = DownloadHelper.GetDownloadUrlsAsync(Uri);
            VideoInfo videoInfo = null;
            foreach (VideoInfo info in videoInfos) if (info.VideoType == VideoType.Mp4 && info.Resolution == 360) { videoInfo = info; break; }
            if (videoInfo == null) { UpdateStatus(DownloadState.Error); return; }
            Title = videoInfo.Title;
            VideoExtension = videoInfo.VideoExtension;
            var videoFile = DownloadHelper.GetLegalPath(Title) + VideoExtension;
            UpdateStatus(DownloadState.TitleChanged, Percentage);
            DownloadHelper.DownloadToFileAsync(this, videoInfo.DownloadUri, VideoFolder, videoFile,OnYoutubeLoading);
            DownloadState = DownloadState.DownloadFinish;
            if (MediaType == MediaType.Audio) {
                Percentage = 50.0;
                var converter = new AudioConverter(this, OnAudioConversionStatusChange);
                converter.ConvertToMp3();
            } else if (OnEntryDownloadStatusChange != null)
                UpdateStatus(DownloadState.Ready);
        }

        private void OnYoutubeLoading(long count, long total)
        {
            UpdateStatus(DownloadState.DownloadProgressChanged, ((double) count/total)*((MediaType == MediaType.Audio) ? 50 : 100));
        }

        private void OnAudioConversionStatusChange(Feed feed, DownloadState downloadState, double percentage)
        {
            UpdateStatus(downloadState, percentage);
        }

        internal void UpdateStatus(DownloadState state, double percentage = 100.0)
        {
            DownloadState = state;
            Percentage = percentage;
            if (OnEntryDownloadStatusChange != null)
                OnEntryDownloadStatusChange(this, DownloadState, Percentage);
        }

        public override string ToString()
        {
            if (Title != null) return Title;
            if (Uri != null) return Uri.ToString();
            return Guid.ToString();
        }

        public YoutubeEntry Clone()
        {
            var entry = new YoutubeEntry {
                Title = Title,
                BaseFolder = BaseFolder,
                Parent = Parent,
                Description = Description,
                DownloadFolder = DownloadFolder,
                ProviderFolder = ProviderFolder,
                MediaType = MediaType,
                ThumbnailUrl = ThumbnailUrl,
                Uri = Uri,
                VideoExtension = VideoExtension,
                VideoFolder = VideoFolder,
                ExecutionStatus = ExecutionStatus
            };
            if (entry.ExecutionStatus == ExecutionStatus.Deleted) entry.DownloadState = DownloadState.Deleted;
            if (ThumbnailUrls != null && ThumbnailUrls.Length > 0) {
                entry.ThumbnailUrls = new string[ThumbnailUrls.Length];
                for (var i = 0; i < ThumbnailUrls.Length; i++)
                    entry.ThumbnailUrls[i] = ThumbnailUrls[i];
            }
            return entry;
        }

        public static YoutubeEntry Create(Uri uri, YoutubeEntry parent = null)
        {
            var entry = new YoutubeEntry(parent);
            if (uri != null) 
                entry.Uri = uri;
            return entry;
        }

        public override void Delete()
        {
            if (DownloadState == DownloadState.Error || DownloadState == DownloadState.Ready) return;
            try {
                var title = DownloadHelper.GetLegalPath(Title);
                var videoFile = title + VideoExtension;
                if (DownloadHelper.FileExists(VideoFolder, videoFile)) DownloadHelper.GetFile(VideoFolder, videoFile).DeleteAsync();
                if (MediaType == MediaType.Audio) {
                    var audioFile = title + ".mp3";
                    if (DownloadHelper.FileExists(DownloadFolder, audioFile)) DownloadHelper.GetFile(DownloadFolder, audioFile).DeleteAsync();
                }
            } catch { }
            base.Delete();
            UpdateStatus(DownloadState.Deleted);
        }
    }
}
