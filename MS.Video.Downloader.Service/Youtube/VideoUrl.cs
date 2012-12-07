using System;

namespace MS.Video.Downloader.Service.Youtube
{
    public class YoutubeUrl : VideoUrl
    {
        protected override void Parse(string lurl)
        {
            var surl = lurl.Replace("youtu.be/", "youtube.com/watch?v=");
            surl = surl.Replace("www.youtube.com", "youtube.com");
            ParseYoutubeUrl(surl);
        }

        private void ParseYoutubeUrl(string surl)
        {
            if (surl.StartsWith("http://youtube.com/v/")) {
                surl = surl.Replace("youtube.com/v/", "youtube.com/watch?v=");
            }
            else if (surl.StartsWith("http://youtube.googleapis.com/v")) {
                surl = surl.Replace("youtube.googleapis.com/v/", "youtube.com/watch?v=");
            }
            else if (surl.StartsWith("http://youtube.com/watch#")) {
                surl = surl.Replace("youtube.com/watch#", "youtube.com/watch?");
            }

            surl = surl.Replace("//youtube.com", "//www.youtube.com");


            var uri = new Uri(surl);
            Uri = uri;
            if (uri.Host != "www.youtube.com") return;
            Provider = ContentProviderType.Youtube;
            var arr = uri.AbsolutePath.Substring(1).Split('/');
            if (arr[0].ToLowerInvariant() == "user") {
                Id = arr[1];
                Type = VideoUrlType.User;
                return;
            }
            Id = GetPlaylistId(uri);
            if (!String.IsNullOrEmpty(Id)) {
                Type = VideoUrlType.Channel;
                return;
            }

            if (arr[0].ToLowerInvariant() != "watch") return;
            Id = GetVideoId(uri);
            Type = VideoUrlType.Video;
            Uri = uri;
        }

        private static string GetVideoId(Uri uri)
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

        private static string GetPlaylistId(Uri uri)
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

    }

    public abstract class VideoUrl
    {
        public Uri Uri { get; set; }
        public VideoUrlType Type { get; set; }

        public ContentProviderType Provider { get; set; }

        public string Id { get; set; }

        public override string ToString()
        {
            return Uri.ToString();
        }

        protected abstract void Parse(string lurl);

        public static VideoUrl Create(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            return Create(uri);
        }

        public static VideoUrl Create(Uri u)
        {

            var surl = u.ToString();
            if (surl.StartsWith("https://")) {
                surl = "http://" + surl.Substring(8);
            } else if (!surl.StartsWith("http://")) {
                surl = "http://" + u;
            }
            var url = surl.IndexOf("vimeo.com", System.StringComparison.Ordinal) >= 0
                      ? (VideoUrl)
                        new VimeoUrl {Uri = u, Type = VideoUrlType.Unknown, Provider = ContentProviderType.NONE}
                      : new YoutubeUrl {Uri = u, Type = VideoUrlType.Unknown, Provider = ContentProviderType.NONE};
            url.Parse(surl);
            return url;
        }


        public static VideoUrl Create(string id, ContentProviderType contentProviderType, VideoUrlType videoUrlType)
        {
            VideoUrl url = null;
            switch (contentProviderType) {
                case ContentProviderType.Vimeo:
                    url = new VimeoUrl() {Id = id, Provider = contentProviderType, Type = videoUrlType};
                    break;
                case ContentProviderType.Youtube:
                    url = new YoutubeUrl() { Id = id, Provider = contentProviderType, Type = videoUrlType };
                    break;
            }
            return url;
        }
    }

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
            } else if (arr[0].Length > 4 && arr[0].StartsWith("user")) {
                Id = arr[0].Substring(4);
                Type = VideoUrlType.User;
                if (arr.Length >= 2) 
                    Command = arr[1]; //videos,albums,channels,likes,groups,following
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