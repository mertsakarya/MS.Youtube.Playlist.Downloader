namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public enum DownloadState
    {
        Initialized,
        DownloadStart,
        DownloadProgressChanged,
        DownloadFinish,
        ConvertAudioStart,
        Ready,
        Error,
        AllFinished,
        AllStart
    }
}