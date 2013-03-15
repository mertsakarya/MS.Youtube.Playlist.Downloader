using System;
using System.IO;
using System.Text;

namespace AmazonSetupUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            var fs = new S3FolderSynchronizer("0QEGQJ1M8RF3X413SY02", "rh/NhdUpGlobZH+NgfuBzMLbOSf57+tHUwVKieT+", "US Standard", "www.mertsakarya.com");
            const string rootFolderName = "ms.video.downloader";
            var localFolder = Path.Combine(DropboxFolder(), "Public\\" + rootFolderName);
            fs.Sync(localFolder, rootFolderName, StatusUpdate);
            Console.WriteLine("DONE!");
            Console.ReadLine();

        }

        private static void StatusUpdate(S3Status status)
        {
            Console.WriteLine(status);
        }

        public static string DropboxFolder()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropbox\\host.db");

            var lines = File.ReadAllLines(dbPath);
            var dbBase64Text = Convert.FromBase64String(lines[1]);
            var folderPath = ASCIIEncoding.ASCII.GetString(dbBase64Text);
            return folderPath;
        }
    }
}
