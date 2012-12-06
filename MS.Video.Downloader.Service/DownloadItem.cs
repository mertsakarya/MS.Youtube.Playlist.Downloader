using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.GData.Client;
using MS.Video.Downloader.Service.Youtube;
using VideoInfo = MS.Video.Downloader.Service.Youtube.VideoInfo;

namespace MS.Video.Downloader.Service
{
    internal delegate void DownloadItemEventHandler(DownloadItem item, DownloadState state, double progressPercentage = 0.0);

    public class DownloadItem
    {
        private readonly string _applicationPath;
        private readonly string _downloadFolder;
        private readonly string _videoFolder;
        private bool _ignoreIfFileExists;

        public VideoInfo VideoInfo { get; private set; }
        public Uri Uri { get; private set; }
        public string BaseFolder { get; private set; }
        public MediaType MediaType { get; private set; }
        public Guid Guid { get; private set; }
        public DownloadStatus Status { get; set; }
        public string ChannelName { get; set; }

        internal DownloadStatusEventHandler OnDownloadStatusChange;

        public DownloadItem(Entry playlist, Uri uri, MediaType mediaType, string baseFolder)
        {
            MediaType = mediaType;
            BaseFolder = baseFolder;
            _applicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Uri = uri;
            var playlistName = (playlist == null) ? "" : "\\" + playlist.Title;
            ChannelName = (playlist == null) ? "" : playlist.Title;

            if (!Directory.Exists(BaseFolder)) Directory.CreateDirectory(BaseFolder);

            _videoFolder = BaseFolder + "\\" + Enum.GetName(typeof(MediaType), MediaType.Video);
            if (!Directory.Exists(_videoFolder)) Directory.CreateDirectory(_videoFolder);
            _videoFolder += playlistName;
            if (!Directory.Exists(_videoFolder)) Directory.CreateDirectory(_videoFolder);

            _downloadFolder = BaseFolder + "\\" + Enum.GetName(typeof(MediaType), MediaType);
            if (!Directory.Exists(_downloadFolder)) Directory.CreateDirectory(_downloadFolder);
            _downloadFolder += playlistName;
            if (!Directory.Exists(_downloadFolder)) Directory.CreateDirectory(_downloadFolder);

            Guid = Guid.NewGuid();

            VideoInfo = null;
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        public async void DownloadAsync(bool ignore = false)
        {
            _ignoreIfFileExists = ignore;
            if (Status.DownloadState != DownloadState.Initialized) return;
            var videoInfos = await DownloadUrlResolver.GetDownloadUrlsAsync(Uri.ToString());
            VideoInfo = videoInfos.FirstOrDefault(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            if (VideoInfo == null) {
                Status.DownloadState = DownloadState.Error;
                Status.Percentage = 100.0;
                Status.UserData = "SKIPPING! No MP4 with 360 pixel resolution";
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(this, Status);
                return;
            }
            Status.DownloadState = DownloadState.DownloadStart;
            var videoFile = Path.Combine(_videoFolder, VideoInfo.Title + VideoInfo.VideoExtension);

            if (!_ignoreIfFileExists || !File.Exists(videoFile))
                await DownloadToFileAsync(new Uri(VideoInfo.DownloadUrl), videoFile);

            Status.DownloadState = DownloadState.DownloadFinish;
            Status.Percentage = 100.0;
            if (MediaType == MediaType.Audio)
                ConvertToMp3();
            else if (OnDownloadStatusChange != null) {
                Status.DownloadState = DownloadState.Ready;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(this, Status);
            }
        }

        private static async Task DownloadToFileAsync(Uri uri, string fileName)
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

        private void ConvertToMp3()
        {
            if (Status.DownloadState != DownloadState.DownloadFinish) return;
            var audioFile = Path.Combine(_downloadFolder, VideoInfo.Title) + ".mp3";
            var videoFile = Path.Combine(_videoFolder, VideoInfo.Title) + VideoInfo.VideoExtension;
            if (_ignoreIfFileExists && File.Exists(audioFile)) {
                Status.DownloadState = DownloadState.Ready;
                Status.Percentage = 100.0;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(this, Status);
            } else {
                if (File.Exists(audioFile)) File.Delete(audioFile);
                if (!File.Exists(videoFile)) return;
                var titleParameter = (String.IsNullOrEmpty(VideoInfo.Title)) ? "": String.Format(" -metadata title=\"{0}\"", VideoInfo.Title);
                var albumParameter = (String.IsNullOrEmpty(ChannelName)) ? "": String.Format(" -metadata album=\"{0}\"", ChannelName);
                var process = new Process {
                    EnableRaisingEvents = true,
                    StartInfo = {
                        FileName = _applicationPath + "\\Executables\\ffmpeg.exe",
                        Arguments = String.Format("-i \"{0}\" {2}{3} -acodec mp3 -y -ac 2 -ab 160 \"{1}\"", videoFile, audioFile, titleParameter, albumParameter),
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                try {
                    process.Start();
                    if (!process.WaitForExit(25000)) { process.Kill(); process.WaitForExit(5000); }
                    var state = (process.ExitCode == 0) ? DownloadState.Ready : DownloadState.Error;
                    Status.DownloadState = state;
                    Status.Percentage = 100.0;
                    if (OnDownloadStatusChange != null) OnDownloadStatusChange(this, Status);
                }
                catch (Exception) {
                    Status.DownloadState = DownloadState.Error;
                    Status.Percentage = 100.0;
                    if (OnDownloadStatusChange != null) OnDownloadStatusChange(this, Status);
                }
            }
        }

    }
}