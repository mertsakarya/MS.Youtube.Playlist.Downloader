using System.IO;

namespace ms.video.downloader.service.Download
{
    public class StorageFolder
    {
        public string FolderName { get; set; }

        public StorageFolder(string folderName = "")
        {
            FolderName = folderName;
        }

        public override string ToString() { return FolderName; }

        public StorageFile CreateFile(string fileName)
        {
            return new StorageFile { FileName = fileName, StorageFolder = this };
        }

        public StorageFolder GetFolder(string folder)
        {
            if (!Directory.Exists(FolderName)) Directory.CreateDirectory(FolderName);
            var path = FolderName + "\\" + folder;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return new StorageFolder(path);

        }
    }
}