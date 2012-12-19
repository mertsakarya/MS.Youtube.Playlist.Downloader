namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public class DownloadStatus
    {
        public double Percentage { get; set; }
        public DownloadState DownloadState { get; set; }
        public object UserData { get; set; }
    }
}