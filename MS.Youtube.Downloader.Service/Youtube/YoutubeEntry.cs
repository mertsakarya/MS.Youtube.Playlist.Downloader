using System;

namespace MS.Youtube.Downloader.Service.Youtube
{
    public class YoutubeEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public Uri WatchPage { get; set; }
        public override string ToString()
        {
            return Title;
        }
    }
}