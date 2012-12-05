using System;

namespace MS.Youtube.Downloader.Service.Youtube
{
    public class VideoUrl
    {
        public Uri Uri { get; set; }
        public VideoUrlType Type { get; set; }

        public ContentProviderType Provider { get; set; }

        public string Id { get; set; }

        public override string ToString()
        {
            return Uri.ToString();
        }

        public static VideoUrl Create(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return new VideoUrl { Type = VideoUrlType.Unknown, Provider = ContentProviderType.NONE };
            return GetUrlType(uri);
        }

        public static VideoUrl Create(Uri u)
        {
            var url = new VideoUrl { Uri = u, Type = VideoUrlType.Unknown, Provider = ContentProviderType.NONE };

            var surl = u.ToString();
            if (surl.StartsWith("https://")) {
                surl = "http://" + surl.Substring(8);
            } else if (!surl.StartsWith("http://")) {
                surl = "http://" + url;
            }

            surl = surl.Replace("youtu.be/", "youtube.com/watch?v=");
            surl = surl.Replace("www.youtube.com", "youtube.com");

            if (surl.StartsWith("http://youtube.com/v/")) {
                surl = surl.Replace("youtube.com/v/", "youtube.com/watch?v=");
            } else if (surl.StartsWith("http://youtube.googleapis.com/v")) {
                surl = surl.Replace("youtube.googleapis.com/v/", "youtube.com/watch?v=");
            } else if (surl.StartsWith("http://youtube.com/watch#")) {
                surl = surl.Replace("youtube.com/watch#", "youtube.com/watch?");
            }

            surl = surl.Replace("//youtube.com", "//www.youtube.com");

            if (surl.IndexOf("youtube.com", System.StringComparison.Ordinal) >= 0) url.Provider = ContentProviderType.Youtube;
            if (surl.IndexOf("vimeo.com", System.StringComparison.Ordinal) >= 0) url.Provider = ContentProviderType.Vimeo;

            var uri = new Uri(surl);
            url.Uri = uri;
            switch (url.Provider) {
                case ContentProviderType.Youtube:
                    if (uri.Host != "www.youtube.com") return url;
                    var arr = uri.AbsolutePath.Substring(1).Split('/');
                    if (arr[0].ToLowerInvariant() == "user") {
                        url.Id = arr[1];
                        url.Type = VideoUrlType.User;
                        return url;
                    }
                    url.Id = GetPlaylistId(uri);
                    if (!String.IsNullOrEmpty(url.Id)) {
                        url.Type = VideoUrlType.Channel;
                        return url;
                    }
                    try {
                        if (arr[0].ToLowerInvariant() == "watch") {
                            url.Id = GetVideoId(uri);
                            url.Type = VideoUrlType.Video;
                            url.Uri = uri;
                        }
                    } catch { }
                    break;
                case ContentProviderType.Vimeo:


            }
            return url;
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
}