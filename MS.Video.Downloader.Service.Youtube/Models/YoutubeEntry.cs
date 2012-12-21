using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.Dowload;

namespace MS.Video.Downloader.Service.Youtube.Models
{

    public class YoutubeEntry : Entry
    {
        private readonly MSYoutubeSettings _settings;

        protected YoutubeUrl YoutubeUrl {
            get
            {
                var youtubeUrl = VideoUrl as YoutubeUrl;
                if (youtubeUrl == null) throw new Exception("Inavlid URL");
                return youtubeUrl;
            }
        }

        public YoutubeEntry(Entry parent = null) : base(parent)
        {
            _settings = new MSYoutubeSettings(
                "MS.Youtube.Downloader",
                "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg"
                ) {AutoPaging = true, PageSize = 50};
        }

        public override void GetEntries(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
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
            var entries = new List<Entry>();

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
            //if (Title == "Favorite Videos") {
            //    FillEntriesYoutubeFavorites(onEntriesReady, onYoutubeLoading);
            //    return;
            //}
            var youtubeUrl = YoutubeUrl;

            //https://gdata.youtube.com/feeds/api/users/{0}/uploads"
            //http://www.youtube.com/feed/UC2xskkQVFEpLcGFnNSLQY0A
            //https://gdata.youtube.com/feeds/api/users/UC2xskkQVFEpLcGFnNSLQY0A/uploads
            var url = "";
            if (!String.IsNullOrEmpty(YoutubeUrl.ChannelId)) {
                url = "https://gdata.youtube.com/feeds/api/playlists/" + youtubeUrl.ChannelId;
            } else if (!String.IsNullOrEmpty(YoutubeUrl.FeedId)) {
                url = String.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads", YoutubeUrl.FeedId);
            } 
            if (url.Length > 0) {
                var request = new MSYoutubeRequest(_settings);

                MSYoutubeEntry items;
                try {
                    items = await request.GetAsync(YoutubeUrl, new Uri(url), onYoutubeLoading);
                    if (items == null) {
                        if (onEntriesReady != null) onEntriesReady(new List<Entry>());
                        return;
                    }
                }
                catch {
                    if (onEntriesReady != null) onEntriesReady(new List<Entry>());
                    return;
                }
                var entries = GetMembers(items);
                if (onEntriesReady != null) onEntriesReady(entries);
            }
        }

        public override async Task DownloadAsync(MediaType mediaType, bool ignore)
        {
            await base.DownloadAsync(mediaType, ignore);
            var videoInfos = await DownloadUrlResolver.GetDownloadUrlsAsync(Uri);
            var videoInfo = videoInfos.FirstOrDefault(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            if (videoInfo == null) {
                Status.DownloadState = DownloadState.Error;
                Status.Percentage = 100.0;
                Status.UserData = "SKIPPING! No MP4 with 360 pixel resolution";
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                return;
            }
            Title = videoInfo.Title;
            VideoExtension = videoInfo.VideoExtension;
            Status.DownloadState = DownloadState.TitleChanged;
            var videoFile = GetLegalPath(Title) + VideoExtension;
            var fileExists = await FileExists(VideoFolder, videoFile);
            if (!(ignore && fileExists)) {
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                //await Logger.Log("SUCCESS: " + videoInfo.DownloadUrl+"\r\n");
                await DownloadToFileAsync(new Uri(videoInfo.DownloadUrl), VideoFolder, videoFile, OnYoutubeLoading);
            }

            Status.DownloadState = DownloadState.DownloadFinish;
            if (MediaType == MediaType.Audio) {
                Status.Percentage = 50.0;
                ConvertToMp3(ignore);
            }  else if (OnDownloadStatusChange != null) {
                Status.Percentage = 100.0;
                Status.DownloadState = DownloadState.Ready;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
            }
        }

        private void OnYoutubeLoading(object self, long count, long total)
        {
            var percentage = ((double)count / total) * ((MediaType == MediaType.Audio) ? 50 : 100);
            Status.DownloadState = DownloadState.DownloadProgressChanged;
            Status.Percentage = percentage;
            if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
        }

        private List<Entry> GetMembers(MSYoutubeEntry items)
        {
            var entries = new List<Entry>();
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
        public override Entry Clone()
        {
            var entry = new YoutubeEntry();
            CopyTo(entry);
            return entry;
        }
    }
}
