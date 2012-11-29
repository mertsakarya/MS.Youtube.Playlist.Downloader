using System;

namespace MS.Youtube.Downloader.Service.Youtube
{
    public class YoutubeUrl
    {
        public Uri Uri { get; set; }
        public YoutubeUrlType Type { get; set; }
        public string Id { get; set; }
        public override string ToString()
        {
            return Uri.ToString();
        }
    }
}