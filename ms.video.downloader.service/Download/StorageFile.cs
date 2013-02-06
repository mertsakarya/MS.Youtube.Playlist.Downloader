using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ms.video.downloader.service.Download
{
    public class StorageFile
    {
        public StorageFolder StorageFolder { get; set; }
        public string FileName { get; set; }
        public override string ToString() { return Path.Combine(StorageFolder.ToString(), FileName); }
        public long Length { get { return (new FileInfo(ToString())).Length; } }
        public void Delete() { File.Delete(ToString()); }
        public Stream OpenStreamForWrite()
        {
            try {
                return File.OpenWrite(ToString());
            } catch(IOException) { //File is opened/locked by another process
                return null;
            }
        }

        public bool Exists() { return File.Exists(ToString()); }
        public void Move(StorageFile file) { File.Move(ToString(), file.ToString()); }
        public void WriteAllLines(List<string> list, Encoding encoding = null)
        {
            File.WriteAllLines(ToString(), list, encoding ?? Encoding.UTF8);
        }

        public void Write(string text, Encoding encoding = null)
        {
            File.WriteAllText(ToString(), text, encoding ?? Encoding.UTF8);
        }

        public string Read()
        {
            return File.ReadAllText(ToString());
        }
    }
}