namespace MS.Youtube.Downloader.Service.Youtube
{
    public class YoutubeEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Url { get; set; }

        public string YoutubeId { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}