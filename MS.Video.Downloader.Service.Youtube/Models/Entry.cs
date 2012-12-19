using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.Dowload;
using Windows.Foundation;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace MS.Video.Downloader.Service.Youtube.Models
{
    public delegate void EntriesReady(IList<Entry> entries);

    public abstract class Entry
    {
        //protected static string ApplicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        private Uri _uri;

        protected Entry(Entry parent = null)
        {
            Parent = parent;
            Guid = Guid.NewGuid();
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        public static Entry Create(Uri uri, Entry parent = null)
        {
            var videoUrl = VideoUrl.Create(uri);
            Entry entry = null;
            switch (videoUrl.Provider) {
                case ContentProviderType.Youtube:
                    entry = new YoutubeEntry(parent) { Uri = uri };
                    break;
            }
            return entry;
        }

        public Entry Parent { get; private set; }
        public string Title { get; set; }
        public string VideoExtension { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string[] ThumbnailUrls { get; set; }
        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value;
                VideoUrl = VideoUrl.Create(_uri);
            }
        }
        public VideoUrl VideoUrl { get; protected set; }

        public StorageFolder BaseFolder { get; set; }
        public StorageFolder ProviderFolder { get; set; }
        public StorageFolder VideoFolder { get; set; }
        public StorageFolder DownloadFolder { get; set; }
        public MediaType MediaType { get; set; }
        public Guid Guid { get; private set; }
        public DownloadStatus Status { get; set; }
        public string ChannelName { get { return Parent == null ? "" : Parent.Title; } }
        public DownloadStatusEventHandler OnDownloadStatusChange;

        public async virtual Task DownloadAsync(MediaType mediaType, bool ignore = false)
        {
            MediaType = mediaType;
            //BaseFolder = baseFolder;
            BaseFolder = KnownFolders.VideosLibrary;
            ProviderFolder = await GetFolder(BaseFolder, Enum.GetName(typeof(ContentProviderType), VideoUrl.Provider));
            VideoFolder = await GetFolder(ProviderFolder, GetLegalPath(ChannelName));

            if (MediaType == MediaType.Audio) {
                var audioFolder = KnownFolders.MusicLibrary;
                ProviderFolder = await GetFolder(audioFolder, Enum.GetName(typeof (ContentProviderType), VideoUrl.Provider));
                DownloadFolder = await GetFolder(ProviderFolder, GetLegalPath(ChannelName));
            }

            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        protected static async Task<StorageFolder> GetFolder(StorageFolder baseFolder, string folderName)
        {
            var found = true;
            StorageFolder folder = null;
            try {
                if (String.IsNullOrEmpty(folderName))
                    folder = baseFolder;
                else  if (await baseFolder.GetFolderAsync(folderName) != null)
                    folder =  await baseFolder.GetFolderAsync(folderName);
            }
            catch (FileNotFoundException ex) {
                found = false;
            }
            if(!found)
                folder = await baseFolder.CreateFolderAsync(folderName);
            return folder;
        }

        protected static async Task<bool> FileExists(StorageFolder folder, string videoFile)
        {
            var fileExists = false;
            try {
                await folder.CreateFileAsync(videoFile);
            } catch {
                fileExists = true;
            }
            return fileExists;
        }

        public override string ToString() { return Title; }

        public abstract void GetEntries(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading);


        public static async Task DownloadToFileAsync(Uri uri, StorageFolder folder, string fileName)
        {
            using (var stream = await DownloadToStreamAsync(uri)) {
                var storageFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                if (stream != null) {
                    using (var destinationStream = await storageFile.OpenStreamForWriteAsync()) {
                        await stream.CopyToAsync(destinationStream, 16384);
                    }
                }
            }
        }

        public static async Task CopyToAsync(Stream input, Stream output, int bufferSize = 16384)
        {
            var buffer = new byte[bufferSize]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                await output.WriteAsync(buffer, 0, bytesRead);
            }
        }

        public static async Task<string> DownloadToStringAsync(Uri uri, Encoding encoding = null)
        {
            using (var stream = await DownloadToStreamAsync(uri)) {
                using (var destinationStream = new MemoryStream()) {
                    if (stream != null) {
                        await stream.CopyToAsync(destinationStream);
                    }
                    var bytes = destinationStream.ToArray();
                    if (encoding == null)
                        return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                    return encoding.GetString(bytes, 0, bytes.Length);
                }
            }
        }

        public static async Task<byte[]> DownloadToByteArrayAsync(Uri uri, Encoding encoding = null)
        {
            using (var stream = await DownloadToStreamAsync(uri)) {
                using (var destinationStream = new MemoryStream()) {
                    if (stream != null) {
                        await stream.CopyToAsync(destinationStream);
                    }
                    var bytes = destinationStream.ToArray();
                    return bytes;
                }
            }
        }

        public static async Task<Stream> DownloadToStreamAsync(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");
            //var req = WebRequest.Create(uri);
            var req = new HttpClient();
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11");
            var resp = await req.SendAsync(message);
            return await resp.Content.ReadAsStreamAsync();
        }

        protected async void ConvertToMp3(bool ignore = false)
        {
            if (Status.DownloadState != DownloadState.DownloadFinish) return;
            var title = GetLegalPath(Title);
            var audioFileName =  title + ".mp3";
            var videoFileName = title + VideoExtension;
            var fileExists = await FileExists(DownloadFolder, audioFileName);
            if (ignore && fileExists) {
                Status.DownloadState = DownloadState.Ready;
                Status.Percentage = 100.0;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
            }
            else {
                //try {
                //    var tmpFile = await DownloadFolder.GetFileAsync(audioFileName);
                //    if (tmpFile != null)
                //        await tmpFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                //}
                //catch {}
                var audioFile = await DownloadFolder.CreateFileAsync(audioFileName, CreationCollisionOption.ReplaceExisting);
                var videoFile = await VideoFolder.GetFileAsync(videoFileName);
                if (videoFile == null) return;
                try {
                    TranscodeFile(videoFile, audioFile, (info, asyncStatus) => {
                        var state = DownloadState.Ready;
                        Status.DownloadState = state;
                        Status.Percentage = 100.0;
                        if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                    });
                }
                catch (Exception) {
                    Status.DownloadState = DownloadState.Error;
                    Status.Percentage = 100.0;
                    if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                }
            }
        }

        private async void TranscodeFile(StorageFile srcFile, StorageFile destFile, AsyncActionWithProgressCompletedHandler<double> action  )
        {
            var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
            var transcoder = new MediaTranscoder();
            var prepareOp = await transcoder.PrepareFileTranscodeAsync(srcFile, destFile, profile);
            if (prepareOp.CanTranscode) {
                var transcodeOp = prepareOp.TranscodeAsync();
                //transcodeOp.Progress += TranscodeProgress;
                transcodeOp.Completed += action;
            } //else {
                //switch (prepareOp.FailureReason) {
                //    case TranscodeFailureReason.CodecNotFound:
                //        OutputText("Codec not found.");
                //        break;
                //    case TranscodeFailureReason.InvalidProfile:
                //        OutputText("Invalid profile.");
                //        break;
                //    default:
                //        OutputText("Unknown failure.");
                //        break;
                //}
            //}
        }

        protected static string GetLegalPath(string text)
        {
            if (String.IsNullOrEmpty(text)) return text;
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return  r.Replace(text, "_");
        }

        public abstract void ParseChannelInfoFromHtml(VideoUrl url);

        public abstract Entry Clone();

        protected void CopyTo(Entry entry)
        {
            entry.Title = Title;
            entry.BaseFolder = BaseFolder;
            entry.Parent = Parent;
            entry.Description = Description;
            entry.DownloadFolder = DownloadFolder;
            entry.Guid = new Guid();
            entry.ProviderFolder = ProviderFolder;
            entry.MediaType = MediaType;
            entry.Status = new DownloadStatus {
                DownloadState = DownloadState.Initialized,
                Percentage = 0.0,
                UserData = Status.UserData
            };
            entry.ThumbnailUrl = ThumbnailUrl;
            if (ThumbnailUrls != null && ThumbnailUrls.Length > 0) {
                entry.ThumbnailUrls = new string[ThumbnailUrls.Length];
                for (var i = 0; i < ThumbnailUrls.Length; i++)
                    entry.ThumbnailUrls[i] = ThumbnailUrls[i];
            }
            entry.Uri = Uri;
            entry.VideoExtension = VideoExtension;
            entry.VideoFolder = VideoFolder;
        }
    }
}