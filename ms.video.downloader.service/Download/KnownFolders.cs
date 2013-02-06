using System;

namespace ms.video.downloader.service.Download
{
    public static class KnownFolders
    {
        public static readonly StorageFolder ApplicationData = new StorageFolder(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        public static readonly StorageFolder Desktop = new StorageFolder { FolderName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
        public static  readonly StorageFolder Root = Desktop.GetFolder("MS.Video.Downloader");
        public static readonly StorageFolder VideosLibrary = Root.GetFolder("Video");
        public static readonly StorageFolder MusicLibrary = Root.GetFolder("Music");
        public static readonly StorageFolder CompanyFolder = ApplicationData.GetFolder("ms");
        public static readonly StorageFolder AppFolder = CompanyFolder.GetFolder("ms.video.downloader");
        public static readonly StorageFolder TempFolder = new StorageFolder(System.IO.Path.GetTempPath());

        public static StorageFolder GetAppVersionFolder(string version) { return AppFolder.GetFolder(version);  } 

    }
}