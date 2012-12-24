namespace MS.Video.Downloader.Service.Youtube.MSYoutube
{
    public class MSYoutubeSettings
    {
        public string ApplicationName { get; set; }
        public string DeveloperKey { get; set; }
        public int PageSize { get; set; }
        public bool AutoPaging { get; set; }

        public MSYoutubeSettings(string applicationName, string developerKey)
        {
            ApplicationName = applicationName;
            DeveloperKey = developerKey;
            AutoPaging = false;
        }
    }
}
