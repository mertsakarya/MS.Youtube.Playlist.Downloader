namespace MS.Youtube.Downloader.Service
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