using System;
using System.IO;

namespace ms.video.downloader.service.Dowload
{
    public class StorageFolder
    {
        public string FolderName { get; set; }

        public StorageFolder(string folderName = "")
        {
            FolderName = folderName;
        }

        public override string ToString() { return FolderName; }

        public StorageFile GetFileAsync(string fileName)
        {
            return new StorageFile { StorageFolder = this, FileName = fileName };
        }

        public StorageFile CreateFileAsync(string fileName)
        {
            return new StorageFile { FileName = fileName, StorageFolder = this };
        }
    }

    public class StorageFile
    {
        public StorageFolder StorageFolder { get; set; }
        public string FileName { get; set; }
        public override string ToString() { return Path.Combine(StorageFolder.ToString(), FileName); }

        public void DeleteAsync() { File.Delete(ToString()); }
        public Stream OpenStreamForWriteAsync()
        {
            try {
                return File.OpenWrite(ToString());
            } catch(IOException) { //File is opened/locked by another process
                return null;
            }
        }
    }

    public class KnownFolders
    {
        private static StorageFolder GetStorageFolder(string folderName)
        {
            var desktop = new StorageFolder { FolderName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
            var sf = DownloadHelper.GetFolder(desktop, "MS.Video.Downloader");
            return DownloadHelper.GetFolder(sf, folderName);
        }

        public static StorageFolder VideosLibrary
        {
            get {
                return GetStorageFolder("Video");
            }
        }

        public static StorageFolder MusicLibrary
        {
            get
            {
                return GetStorageFolder("Music");
            }
        }
    }
}