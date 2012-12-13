using System;
using MS.Video.Downloader.Service.Youtube;

namespace MS.Video.Downloader.Service.Models
{
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
}