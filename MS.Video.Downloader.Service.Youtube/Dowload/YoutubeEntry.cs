using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.MSYoutube;
using Windows.Foundation;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{

    public delegate void EntriesReady(IList<YoutubeEntry> entries);

    public delegate void EntryDownloadStatusEventHandler(YoutubeEntry entry, DownloadStatus status);

    public class YoutubeEntry
    {
        private readonly MSYoutubeSettings _settings;
        private Uri _uri;

        public YoutubeEntry Parent { get; private set; }
        public string Title { get; set; }
        public string VideoExtension { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string[] ThumbnailUrls { get; set; }
        public YoutubeUrl YoutubeUrl { get; protected set; }
        public StorageFolder BaseFolder { get; set; }
        public StorageFolder ProviderFolder { get; set; }
        public StorageFolder VideoFolder { get; set; }
        public StorageFolder DownloadFolder { get; set; }
        public MediaType MediaType { get; set; }
        public Guid Guid { get; private set; }
        public DownloadStatus Status { get; set; }
        public string ChannelName { get { return Parent == null ? "" : Parent.Title; } }
        public EntryDownloadStatusEventHandler OnEntryDownloadStatusChange;
        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; YoutubeUrl = YoutubeUrl.Create(_uri); }
        }

        private YoutubeEntry(YoutubeEntry parent = null)
        {
            Parent = parent;
            Guid = Guid.NewGuid();
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
            _settings = new MSYoutubeSettings( "MS.Youtube.Downloader", "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg" ) {AutoPaging = true, PageSize = 50};
        }

        #region Convert to MP3

        private async void ConvertToMp3(bool ignore = false)
        {
            if (Status.DownloadState != DownloadState.DownloadFinish) return;
            var title = DownloadHelper.GetLegalPath(Title);
            var audioFileName = title + ".mp3";
            var videoFileName = title + VideoExtension;
            var fileExists = await DownloadHelper.FileExists(VideoFolder, videoFileName);
            if (!fileExists) return;
            fileExists = await DownloadHelper.FileExists(DownloadFolder, audioFileName);
            if (ignore && fileExists) {
                UpdateStatus(DownloadState.Ready);
            } else {
                if (fileExists) {
                    var file = await DownloadFolder.GetFileAsync(audioFileName);
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                var videoFile = await VideoFolder.GetFileAsync(videoFileName);
                var audioFile = await DownloadFolder.CreateFileAsync(audioFileName, CreationCollisionOption.ReplaceExisting);
                try {
                    TranscodeFile(videoFile, audioFile, 
                        (info, asyncStatus) => UpdateStatus(DownloadState.Ready), 
                        (info, progressInfo) => UpdateStatus(DownloadState.DownloadProgressChanged, 50 + (progressInfo/2))
                    );
                } catch (Exception) {
                    UpdateStatus(DownloadState.Error);
                }
            }
        }

        private void UpdateStatus(DownloadState state, double percentage = 100.0)
        {
            Status.DownloadState = state;
            Status.Percentage = percentage;
            if (OnEntryDownloadStatusChange != null) OnEntryDownloadStatusChange(this, Status);

        }

        private static async void TranscodeFile(StorageFile srcFile, StorageFile destFile, AsyncActionWithProgressCompletedHandler<double> action, AsyncActionProgressHandler<double> progress )
        {
            var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
            var transcoder = new MediaTranscoder();
            var prepareOp = await transcoder.PrepareFileTranscodeAsync(srcFile, destFile, profile);
            if (!prepareOp.CanTranscode) return;
            var transcodeOp = prepareOp.TranscodeAsync();
            transcodeOp.Progress += progress;
            transcodeOp.Completed += action;
        }

        #endregion

        #region GetEntries
        public void GetEntries(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            if(YoutubeUrl.Type == VideoUrlType.Channel || YoutubeUrl.ChannelId != "" || YoutubeUrl.FeedId != "")
                FillEntriesChannel(onEntriesReady, onYoutubeLoading);
            if(YoutubeUrl.Type == VideoUrlType.User)
                FillEntriesUser(onEntriesReady, onYoutubeLoading);
        }

        private async void FillEntriesUser(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            var youtubeUrl = YoutubeUrl;
            var request = new MSYoutubeRequest(_settings);
            var items = await request.GetAsync(YoutubeUrl, new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/playlists?v=2", youtubeUrl.UserId)), onYoutubeLoading);
            if (items == null) return;
            var entries = new List<YoutubeEntry>();

            try {
                if (!String.IsNullOrWhiteSpace(items.AuthorId)) {
                    var favoritesEntry = new YoutubeEntry(this) {
                        Title = "Favorite Videos",
                        Uri = new Uri("http://www.youtube.com/playlist?list=FL" + items.AuthorId),
                    };
                    entries.Add(favoritesEntry);
                }
                foreach (var member in items.Entries) {
                    var entry = new YoutubeEntry(this) {
                        Title = member.Title,
                        Uri = member.Uri,
                        Description = member.Description
                    };
                    entries.Add(entry);
                }
            }
            catch {
                entries.Clear();
            }
            if (onEntriesReady != null) onEntriesReady(entries);
        }

        private async void FillEntriesChannel(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            var url = "";
            if (!String.IsNullOrEmpty(YoutubeUrl.ChannelId)) {
                url = "https://gdata.youtube.com/feeds/api/playlists/" + YoutubeUrl.ChannelId;
            } else if (!String.IsNullOrEmpty(YoutubeUrl.FeedId)) {
                url = String.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads", YoutubeUrl.FeedId);
            }
            if (url.Length <= 0) return;

            MSYoutubeEntry items;
            try {
                var request = new MSYoutubeRequest(_settings);
                items = await request.GetAsync(YoutubeUrl, new Uri(url), onYoutubeLoading);
                if (items == null) {
                    if (onEntriesReady != null) onEntriesReady(new List<YoutubeEntry>());
                    return;
                }
                if (String.IsNullOrEmpty(Title)) 
                    Title = items.Title;
            }
            catch {
                if (onEntriesReady != null) onEntriesReady(new List<YoutubeEntry>());
                return;
            }
            var entries = GetMembers(items);
            if (onEntriesReady != null) onEntriesReady(entries);
        }

        private List<YoutubeEntry> GetMembers(MSYoutubeEntry items)
        {
            var entries = new List<YoutubeEntry>();
            MSYoutubeEntry[] members;
            try {
                members = items.Entries.Where(member => member.Uri != null).ToArray();
            } catch {
                return entries;
            }
            foreach (var member in members) {
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

        public async Task DownloadAsync(MediaType mediaType, bool ignore)
        {
            UpdateStatus(DownloadState.DownloadStart, 0.0);
            MediaType = mediaType;
            BaseFolder = KnownFolders.VideosLibrary;
            ProviderFolder = await DownloadHelper.GetFolder(BaseFolder, Enum.GetName(typeof(ContentProviderType), YoutubeUrl.Provider));
            VideoFolder = await DownloadHelper.GetFolder(ProviderFolder, DownloadHelper.GetLegalPath(ChannelName));

            if (MediaType == MediaType.Audio) {
                var audioFolder = KnownFolders.MusicLibrary;
                ProviderFolder = await DownloadHelper.GetFolder(audioFolder, Enum.GetName(typeof(ContentProviderType), YoutubeUrl.Provider));
                DownloadFolder = await DownloadHelper.GetFolder(ProviderFolder, DownloadHelper.GetLegalPath(ChannelName));
            }
            var videoInfos = await DownloadHelper.GetDownloadUrlsAsync(Uri);
            var videoInfo = videoInfos.FirstOrDefault(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            if (videoInfo == null) { UpdateStatus(DownloadState.Error); return; }
            Title = videoInfo.Title;
            VideoExtension = videoInfo.VideoExtension;
            Status.DownloadState = DownloadState.TitleChanged;
            var videoFile = DownloadHelper.GetLegalPath(Title) + VideoExtension;
            var fileExists = await DownloadHelper.FileExists(VideoFolder, videoFile);
            if (!(ignore && fileExists)) {
                if (OnEntryDownloadStatusChange != null) OnEntryDownloadStatusChange(this, Status);
                await DownloadHelper.DownloadToFileAsync(videoInfo.DownloadUri, VideoFolder, videoFile,
                    (count, total) => UpdateStatus(DownloadState.DownloadProgressChanged, ((double) count/total) * ((MediaType == MediaType.Audio) ? 50 : 100)));
            }

            Status.DownloadState = DownloadState.DownloadFinish;
            if (MediaType == MediaType.Audio) {
                Status.Percentage = 50.0;
                ConvertToMp3(ignore);
            }  else if (OnEntryDownloadStatusChange != null) 
                UpdateStatus(DownloadState.Ready);
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
                Guid = new Guid(),
                ProviderFolder = ProviderFolder,
                MediaType = MediaType,
                Status = new DownloadStatus {
                    DownloadState = DownloadState.Initialized,
                    Percentage = 0.0,
                },
                ThumbnailUrl = ThumbnailUrl,
                Uri = Uri,
                VideoExtension = VideoExtension,
                VideoFolder = VideoFolder
            };
            if (ThumbnailUrls != null && ThumbnailUrls.Length > 0) {
                entry.ThumbnailUrls = new string[ThumbnailUrls.Length];
                for (var i = 0; i < ThumbnailUrls.Length; i++)
                    entry.ThumbnailUrls[i] = ThumbnailUrls[i];
            }
            return entry;
        }

        public static YoutubeEntry Create(Uri uri, YoutubeEntry parent = null)
        {
            var entry =  new YoutubeEntry(parent) { Uri = uri };
            return entry;
        }
    }
}
