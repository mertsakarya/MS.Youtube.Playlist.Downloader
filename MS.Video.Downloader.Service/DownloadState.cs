namespace MS.Video.Downloader.Service
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
        AllFinished
    }
}