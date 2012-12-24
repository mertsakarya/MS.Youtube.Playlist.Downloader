using System;
using System.Collections.Generic;
using MS.Video.Downloader.Service.Youtube.Dowload;

namespace MS.Video.Downloader.Service.Youtube.MSYoutube
{
    public class MSYoutubeEntry
    {
        public Uri NextPageUri { get; set; }

        public IList<MSYoutubeEntry> Entries { get; set; }

        public string Description { get; set; }
        public string Title { get; set; }
        public Uri Uri { get; set; }
        public string Content { get; set; }
        public IList<MSYoutubeThumbnail> Thumbnails { get; set; }

        public YoutubeUrl YoutubeUrl { get; set; }

        public string Author { get; set; }
        public string AuthorId { get; set; }

        public int Total { get; set; }

        public override string ToString() { return Title; }

        public MSYoutubeEntry()
        {
            Thumbnails = new List<MSYoutubeThumbnail>();
            Entries = new List<MSYoutubeEntry>();
        }
    }
}