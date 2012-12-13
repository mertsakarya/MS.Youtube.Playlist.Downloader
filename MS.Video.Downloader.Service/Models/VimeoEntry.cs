using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube;
using TagLib.Id3v2;
using Vimeo.API;
using File = System.IO.File;

namespace MS.Video.Downloader.Service.Models
{
    public class VimeoEntry : Entry
    {
        private readonly VimeoClient _vimeoClient;
        
        public VimeoEntry(Entry parent = null)
            : base(parent)
        {
            _vimeoClient = new VimeoClient("0511c1f34b14b62200a3b6fc3db5488d1ffaa1d3", "5ef0dc0e1e161b7185b8de6e980a3921d539e17e");
        }

        public override async void GetEntries(EntriesReady onEntriesReady)
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

        protected async void FillEntriesUser(EntriesReady onEntriesReady)
        {
            await Task.Factory.StartNew(() => {
                var entries = new List<Entry>();
                var videoUrl = VideoUrl as VimeoUrl;
                if (videoUrl == null) return;
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }

        protected async void FillEntriesChannel(EntriesReady onEntriesReady)
        {
            await Task.Factory.StartNew(() => {
                var entries = new List<Entry>();
                var vimeoUrl = VideoUrl as VimeoUrl;
                if (vimeoUrl == null) return;
                var id = vimeoUrl.Id;
                Videos videos;
                switch (vimeoUrl.Command) {
                    case "videos":
                        videos = _vimeoClient.vimeo_videos_getAll(id, true, VimeoClient.VideosSortMethod.Newest, 1, 50);
                        ToEntries(entries, videos);
                        break;
                    case "channel":
                        videos = _vimeoClient.vimeo_channels_getVideos(id, true, 1, 50);
                        ToEntries(entries, videos);
                        break;
                    case "album":
                        videos = _vimeoClient.vimeo_albums_getVideos(id, true, null, 1, 50);
                        ToEntries(entries, videos);
                        break;
                }
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }

        protected override Tag GetId3Tag()
        {
            return null;
        }

        public override void ParseChannelInfoFromHtml(VideoUrl url) {}

        private void ToEntries(List<Entry> entries, Videos videos)
        {
            if(videos != null)
                foreach (var video in videos) {
                    var entry = new VimeoEntry(this) {
                        Title = video.title,
                        Url = video.urls[0].Value,
                        Description = video.description,
                        ThumbnailUrl = video.thumbnails[0].Url
                    };
                    entries.Add(entry);
                }
        }

        public override async void DownloadAsync(MediaType mediaType, string baseFolder, bool ignore = false)
        {
            base.DownloadAsync(mediaType, baseFolder, ignore);
            var vimeoUrl = VideoUrl as VimeoUrl;
            if (vimeoUrl == null) return;

            
            //var videoInfo = _vimeoClient.vimeo_videos_getInfo(vimeoUrl.Id);
            //Status.DownloadState = DownloadState.DownloadStart;

            //if (!ignore || !File.Exists(videoFile))
            var html = await DownloadToStringAsync(new Uri(Url));
            var start = html.IndexOf("window.addEvent('domready', function() ", System.StringComparison.Ordinal);
            if (start >= 0) {
                var signature = GetBetween(html, "\"signature\":\"", "\"");
                var timestamp = GetBetween(html, "\"timestamp\":", ",");
                var hd = GetBetween(html, "\"hd\":", ",") == "1" ? "hd" : "";
                Title = GetBetween(html, "<h1 itemprop=\"name\">", "</h1>");
                VideoExtension = ".flv";
                var videoFile = Path.Combine(VideoFolder, Title + VideoExtension);
                var url =
                    String.Format(
                    "http://player.vimeo.com/play_redirect?clip_id={0}&sig={1}&time={2}&quality={3}&codecs=H264,VP8,VP6&type=moogaloop_local&embed_location=",
                        vimeoUrl.Id, signature, timestamp, hd
                        );
                if (!ignore || !File.Exists(videoFile))
                    await DownloadToFileAsync(new Uri(url), videoFile);
            }
             

            //Status.DownloadState = DownloadState.DownloadFinish;
            //Status.Percentage = 100.0;
            //if (MediaType == MediaType.Audio)
            //    ConvertToMp3(ignore);
            //else if (OnDownloadStatusChange != null) {
            //    Status.DownloadState = DownloadState.Ready;
            //    if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
            //}
        }

        private string GetBetween(string html, string start, string end)
        {
            var r = new Regex(Regex.Escape(start) + "(.*?)" + Regex.Escape(end));
            var match = r.Match(html);
            var val= match.Success ? match.Value : "";
            if (val == "") return val;
            var s = val.Substring(start.Length, val.Length - end.Length - start.Length);
            return s;
        }
    }
}