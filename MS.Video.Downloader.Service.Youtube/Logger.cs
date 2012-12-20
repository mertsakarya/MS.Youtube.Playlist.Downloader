using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace MS.Video.Downloader.Service.Youtube
{
    public static class Logger
    {
        private static readonly StorageFolder _folder = KnownFolders.VideosLibrary;
        private static StorageFile _file;
        private static async Task<StorageFile> GetFile() { return _file ?? (_file = await _folder.CreateFileAsync("Log.txt", CreationCollisionOption.OpenIfExists)); }
        private static readonly  bool _enabled = false;
        public static async Task Log(string text, string filename = "Log.txt")
        {
            if(_enabled)
                await FileIO.AppendTextAsync(await GetFile(), text + "\r\n");
        }
    }
}
