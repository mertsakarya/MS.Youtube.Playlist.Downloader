namespace ms.video.downloader.service
{
    public enum VideoUrlType { Video, Channel, User, Unknown }
    public enum MediaType { Audio, Video }
    public enum ContentProviderType { Youtube = 1, Vimeo = 2, NONE = 0 }
    public enum AudioType { Aac, Mp3, Vorbis, Unknown }
    public enum VideoType { Mobile, Flash, Mp4, WebM, Unknown }
    public enum DownloadState { Initialized, DownloadStart, Deleted, TitleChanged, DownloadProgressChanged, DownloadFinish, ConvertAudioStart, Ready, Error, AllFinished, AllStart, UpdateCache }
    public enum ExecutionStatus { Normal, Deleted }
}