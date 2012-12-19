using System;

namespace MS.Video.Downloader.Service.Youtube.Models
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

        public static VideoUrl Create(Uri u)
        {

            var surl = u.ToString();
            if (surl.StartsWith("https://")) {
                surl = "http://" + surl.Substring(8);
            } else if (!surl.StartsWith("http://")) {
                surl = "http://" + u;
            }
            var url = new YoutubeUrl {Uri = u, Type = VideoUrlType.Unknown, Provider = ContentProviderType.NONE};
            url.Parse(surl);
            return url;
        }


        public static VideoUrl Create(string id, ContentProviderType contentProviderType, VideoUrlType videoUrlType)
        {
            VideoUrl url = null;
            switch (contentProviderType) {
                case ContentProviderType.Vimeo:
                    break;
                case ContentProviderType.Youtube:
                    url = new YoutubeUrl { Id = id, Provider = contentProviderType, Type = videoUrlType };
                    break;
            }
            return url;
        }
    }
}