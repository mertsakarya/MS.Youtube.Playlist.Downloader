using System;
using MS.Video.Downloader.Service.Youtube;

namespace MS.Video.Downloader.Service.Models
{
    public class VimeoUrl : VideoUrl
    {
        public string UserId { get; set; }
        public string ChannelId { get; set; }
        public string VideoId { get; set; }
        public string Command { get; set; }

        protected override void Parse(string surl)
        {
            Provider = ContentProviderType.Vimeo;
            var uri = new Uri(surl);
            Uri = uri;
            var arr = uri.AbsolutePath.Substring(1).ToLowerInvariant().Split('/');
            if (arr.Length == 0) return;
            if (arr[0].Length > 4 && arr[0].StartsWith("user")) {
                Id = arr[0].Substring(4);
                Type = VideoUrlType.Channel;
                if (arr.Length >= 2)
                    Command = arr[1]; //videos,albums,channels,likes,groups,following
                return;
            }
            if (arr.Length == 1) {
                long v;
                if (long.TryParse(arr[0], out v)) {
                    Id = arr[0];
                    VideoId = arr[0];
                    Type = VideoUrlType.Video;
                    return;
                }
                if (!String.IsNullOrWhiteSpace(arr[0])) {
                    Id = arr[0];
                    ChannelId = Id;
                    Type = VideoUrlType.Channel;
                    Command = "channel";
                    return;
                }
                return;
            }
            if (arr[0] == "channels") {
                if (arr.Length > 1) {
                    Id = arr[1];
                    ChannelId = Id;
                    Type = VideoUrlType.Channel;
                }
                if (arr.Length >= 3) {
                    VideoId = TryParseNumber(arr[2]);
                    if (String.IsNullOrEmpty(VideoId))
                        Command = arr[2];
                }
            } else if (arr[0] == "album") {
                Id = arr[1];
                Type = VideoUrlType.Channel;
                Command = arr[0];
                if (arr.Length >= 4 && arr[2].StartsWith("video"))
                    VideoId = TryParseNumber(arr[3]);
            }
        }

        private static string TryParseNumber(string s)
        {
            long v;
            return long.TryParse(s, out v) ? s : "";
        }

    }
}