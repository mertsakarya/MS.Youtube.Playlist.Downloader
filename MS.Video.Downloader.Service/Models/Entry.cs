using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube;

namespace MS.Video.Downloader.Service.Models
{
    public delegate void EntriesReady(IList<Entry> entries);

    public abstract class Entry
    {
        protected static string ApplicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        private string _url;

        protected Entry(Entry parent = null)
        {
            Parent = parent;
            Guid = Guid.NewGuid();
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        public static Entry Create(string url, Entry parent = null)
        {
            var videoUrl = VideoUrl.Create(url);
            Entry entry = null;
            switch (videoUrl.Provider) {
                case ContentProviderType.Vimeo:
                    entry = new VimeoEntry(parent) {Url = url};
                    break;
                case ContentProviderType.Youtube:
                    entry = new YoutubeEntry(parent) { Url = url };
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
        public string Content { get; set; }
        public string Url
        {
            get { return _url; }
            set { _url = value;
                VideoUrl = VideoUrl.Create(_url);
            }
        }
        public string MediaUrl { get; set; }
        public VideoUrl VideoUrl { get; protected set; }
        public int Track { get; protected set; }
        public int TrackCount { get; protected set; }

        public string BaseFolder { get; set; }
        public string ProviderFolder { get; set; }
        public string VideoFolder { get; set; }
        public string DownloadFolder { get; set; }
        public MediaType MediaType { get; set; }
        public Guid Guid { get; private set; }
        public DownloadStatus Status { get; set; }
        public string ChannelName { get { return Parent == null ? "" : Parent.Title; } }
        public DownloadStatusEventHandler OnDownloadStatusChange;

        public virtual void DownloadAsync(MediaType mediaType, string baseFolder, bool ignore = false)
        {
            MediaType = mediaType;
            BaseFolder = baseFolder;
            if (!Directory.Exists(BaseFolder)) Directory.CreateDirectory(BaseFolder);

            ProviderFolder = BaseFolder + "\\" + Enum.GetName(typeof(ContentProviderType), VideoUrl.Provider);
            if (!Directory.Exists(ProviderFolder)) Directory.CreateDirectory(ProviderFolder);

            VideoFolder = ProviderFolder + "\\" + Enum.GetName(typeof(MediaType), MediaType.Video);
            if (!Directory.Exists(VideoFolder)) Directory.CreateDirectory(VideoFolder);
            if (!String.IsNullOrEmpty(GetLegalPath(ChannelName))) {
                VideoFolder += "\\" + GetLegalPath(ChannelName);
                if (!Directory.Exists(VideoFolder)) Directory.CreateDirectory(VideoFolder);
            }

            if (MediaType == MediaType.Audio) {
                DownloadFolder = ProviderFolder + "\\" + Enum.GetName(typeof (MediaType), MediaType);
                if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);
                if (!String.IsNullOrEmpty(ChannelName)) {
                    DownloadFolder += "\\" + ChannelName;
                    if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);
                }
            }
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }




        public override string ToString()
        {
            return Title;
        }

        public abstract void GetEntries(EntriesReady onEntriesReady);

        protected abstract TagLib.Id3v2.Tag GetId3Tag();

        protected static async Task DownloadToFileAsync(Uri uri, string fileName)
        {
            var req = WebRequest.Create(uri);
            using (var resp = await req.GetResponseAsync()) {
                using (var stream = resp.GetResponseStream()) {
                    using (var destinationStream = File.Create(fileName)) {
                        if (stream != null) {
                            await stream.CopyToAsync(destinationStream);
                        }
                    }
                }
            }
        }

        protected static async Task<string> DownloadToStringAsync(Uri uri)
        {
            var req = WebRequest.Create(uri);
            using (var resp = await req.GetResponseAsync()) {
                using (var stream = resp.GetResponseStream()) {
                    using (var destinationStream = new MemoryStream()) {
                        if (stream != null) {
                            await stream.CopyToAsync(destinationStream);
                        }
                        var bytes = destinationStream.GetBuffer();
                        return Encoding.UTF8.GetString(bytes);
                    }
                }
            }
        }

        protected async void ConvertToMp3(bool ignore = false)
        {
            if (Status.DownloadState != DownloadState.DownloadFinish) return;
            var audioFile = Path.Combine(DownloadFolder, GetLegalPath(Title)) + ".mp3";
            var videoFile = Path.Combine(VideoFolder, GetLegalPath(Title)) + VideoExtension;
            if (ignore && File.Exists(audioFile)) {
                Status.DownloadState = DownloadState.Ready;
                Status.Percentage = 100.0;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
            }
            else {
                await Task.Factory.StartNew(() => {
                    if (File.Exists(audioFile)) File.Delete(audioFile);
                    if (!File.Exists(videoFile)) return;
                    var arguments = String.Format("-i \"{0}\" -acodec mp3 -y -ac 2 -ab 160 \"{1}\"", videoFile, audioFile);
                    var process = new Process {
                        EnableRaisingEvents = true,
                        StartInfo = {
                            FileName = ApplicationPath + "\\Executables\\ffmpeg.exe",
                            Arguments = arguments,
                            CreateNoWindow = true,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        }
                    };
                    try {
                        process.Start();
                        if (!process.WaitForExit(1800000)) {
                            process.Kill();
                            process.WaitForExit(30000);
                        }
                        DownloadState state;
                        if (process.ExitCode == 0) {
                            Tag(audioFile);
                            state = DownloadState.Ready;
                        } else state = DownloadState.Error;
                        Status.DownloadState = state;
                        Status.Percentage = 100.0;
                        if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                    }
                    catch (Exception) {
                        Status.DownloadState = DownloadState.Error;
                        Status.Percentage = 100.0;
                        if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                    }
                });
            }
        }

        private void Tag(string filename)
        {
            var file = TagLib.File.Create(filename);
            if (file == null) return;
            var tag = GetId3Tag();
            tag.CopyTo(file.Tag, true);
            file.Tag.Pictures = tag.Pictures;
            file.Save();
        }

        protected static string GetLegalPath(string text)
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return  r.Replace(text, "_");
        }

        public abstract void ParseChannelInfoFromHtml(VideoUrl url);
    }
}