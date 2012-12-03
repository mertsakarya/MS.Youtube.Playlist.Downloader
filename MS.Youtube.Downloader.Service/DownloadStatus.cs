namespace MS.Youtube.Downloader.Service
{
    public class DownloadStatus
    {
        public double Percentage { get; set; }
        public DownloadState DownloadState { get; set; }
        public object UserData { get; set; }
    }
}