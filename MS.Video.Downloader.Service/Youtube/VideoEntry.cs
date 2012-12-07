using System.Collections.Generic;

namespace MS.Video.Downloader.Service.Youtube
{
    public class VideoEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}