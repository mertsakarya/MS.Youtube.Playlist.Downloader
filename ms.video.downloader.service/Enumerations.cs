namespace ms.video.downloader.service
{
    public enum VideoUrlType { Video, Channel, User, Unknown }
    public enum MediaType { Audio, Video }
    public enum ContentProviderType { Youtube = 1, Vimeo = 2, NONE = 0 }
    public enum AudioType { Aac, Mp3, Vorbis, Unknown }
    public enum VideoType { Mobile, Flash, Mp4, WebM, Unknown }
    public enum DownloadState { Initialized, DownloadStart, Paused, Deleted, TitleChanged, DownloadProgressChanged, DownloadFinish, ConvertAudioStart, Ready, Error, AllFinished, AllStart}
    public enum ExecutionStatus { Normal, Pause, Paused, Delete, Deleted }
}