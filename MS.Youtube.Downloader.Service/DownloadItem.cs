using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Google.GData.Client;
using Google.YouTube;
using YoutubeExtractor;

namespace MS.Youtube.Downloader.Service
{
    public class DownloadStatus
    {
        public double Percentage { get; set; }
        public DownloadState DownloadState { get; set; }
    }

    public enum DownloadState
    {
        Initialized,
        DownloadStart,
        DownloadProgressChanged,
        DownloadFinish,
        ConvertAudioStart,
        Ready,
        Error,
        AllFinished
    }

    public enum MediaType
    {
        Audio, Video
    }

    internal delegate void DownloadItemEventHandler(DownloadItem item, DownloadState state, double progressPercentage = 0.0);

    public delegate void DownloadStatusEventHandler(DownloadItem item, DownloadStatus status);

    public class DownloadItems : ObservableCollection<DownloadItem>
    {
        private bool _ignoreDownloaded;
        private readonly int _poolSize;

        public DownloadStatusEventHandler OnDownloadStatusChange;

        public DownloadItems(int poolSize = 3)
        {
            _ignoreDownloaded = false;
            _poolSize = poolSize;
        }

        public void Download(bool ignoreDownloaded)
        {
            _ignoreDownloaded = ignoreDownloaded;
            foreach (var item in this) {
                item.OnDownloadStateChange += OnDownloadStateChange;
            }
            DownloadFirst();
        }

        private void OnDownloadStateChange(DownloadItem item, DownloadState state, double progressPercentage)
        {
            var downloadCount = this.Count(p => !(p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error || p.Status.DownloadState == DownloadState.Initialized));
            if (downloadCount != _poolSize) DownloadFirst();
            if (OnDownloadStatusChange == null) return;
            var finishedCount = this.Count(p => (p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error));
            var sumPercentage = this.Average(p => p.Status.Percentage);
            if (downloadCount == 0 && finishedCount == Count)
                OnDownloadStatusChange(null, new DownloadStatus { DownloadState = DownloadState.AllFinished, Percentage = sumPercentage });
            else 
                OnDownloadStatusChange(item, new DownloadStatus {DownloadState = state, Percentage = sumPercentage});
        }

        private void DownloadFirst()
        {
            var first = this.FirstOrDefault(item => item.Status.DownloadState == DownloadState.Initialized);
            if(first != null)
                first.Download(_ignoreDownloaded);
        }
    }

    public class VideoInfo
    {
        public string Extension { get; set; }
        public string Title { get; set; }
        public string DownloadUrl { get; set; }
        public override string ToString()
        {
            return Title;
        }

        public static VideoInfo GetVideoInfo(Uri uri)
        {
            var videoInfos = DownloadUrlResolver.GetDownloadUrls(uri.ToString());
            var video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            var videoInfo = new VideoInfo() {Extension = video.VideoExtension, Title = video.Title, DownloadUrl = video.DownloadUrl};
            return videoInfo;
        }
    }

    public class DownloadItem
    {
        private readonly string _applicationPath;
        private readonly string _downloadFolder;
        private readonly string _videoFolder;

        public VideoInfo VideoInfo { get; private set; }
        public Uri Uri { get; private set; }
        public string BaseFolder { get; private set; }
        public MediaType MediaType { get; private set; }
        public Guid Guid { get; private set; }
        public DownloadStatus Status { get; set; }


        public string Message { get; private set; }

        internal DownloadItemEventHandler OnDownloadStateChange;

        public DownloadItem(Uri uri, MediaType mediaType, string baseFolder) : this(null, uri, mediaType, baseFolder) { }
        public DownloadItem(Video video, MediaType mediaType, string baseFolder) : this(null, video.WatchPage, mediaType, baseFolder) { }
        public DownloadItem(Entry playlist, Video video, MediaType mediaType, string baseFolder) : this(playlist, video.WatchPage, mediaType, baseFolder) { }
        public DownloadItem(Entry playlist, Uri uri, MediaType mediaType, string baseFolder)
        {
            MediaType = mediaType;
            BaseFolder = baseFolder;
            _applicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Uri = uri;
            var playlistName = (playlist == null) ? "" : "\\" + playlist.Title;


            if (!Directory.Exists(BaseFolder)) Directory.CreateDirectory(BaseFolder);

            _videoFolder = BaseFolder + "\\" + Enum.GetName(typeof(MediaType), MediaType.Video);
            if (!Directory.Exists(_videoFolder)) Directory.CreateDirectory(_videoFolder);
            _videoFolder += playlistName;
            if (!Directory.Exists(_videoFolder)) Directory.CreateDirectory(_videoFolder);

            _downloadFolder = BaseFolder + "\\" + Enum.GetName(typeof(MediaType), MediaType);
            if (!Directory.Exists(_downloadFolder)) Directory.CreateDirectory(_downloadFolder);
            _downloadFolder += playlistName;
            if (!Directory.Exists(_downloadFolder)) Directory.CreateDirectory(_downloadFolder);

            Message = "Initialized";
            Guid = Guid.NewGuid();

            VideoInfo = VideoInfo.GetVideoInfo(Uri);
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        public void Download(bool ignore = false)
        {
            if (Status.DownloadState != DownloadState.Initialized) 
                return;
            Status.DownloadState = DownloadState.DownloadStart;
            var videoFile = Path.Combine(_videoFolder, VideoInfo.Title + VideoInfo.Extension);
            if (ignore && File.Exists(videoFile))
            {
                DownloadFileCompleted(null, new AsyncCompletedEventArgs(null, false, null));
            }
            else
            {
                var client = new WebClient();
                client.DownloadFileCompleted += DownloadFileCompleted;
                client.DownloadProgressChanged += OnClientOnDownloadProgressChanged;
                client.DownloadFileAsync(new Uri(VideoInfo.DownloadUrl), videoFile);
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.DownloadStart);
            }
        }

        private void OnClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs args)
        {
            Status.DownloadState = DownloadState.DownloadProgressChanged;
            Status.Percentage = args.ProgressPercentage;
            if (OnDownloadStateChange != null)
                OnDownloadStateChange(this, DownloadState.DownloadProgressChanged, args.ProgressPercentage);
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null) {
                Message = e.Error.Message;
                Status.DownloadState = DownloadState.Error;
                Status.Percentage = 100.0;
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.Error);
            } else {
                Status.DownloadState = DownloadState.DownloadFinish;
                Status.Percentage = 100.0;
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.DownloadFinish);
                if (MediaType == MediaType.Audio) 
                    ConvertToMp3();
                else if (OnDownloadStateChange != null)
                {
                    Status.DownloadState = DownloadState.Ready; 
                    OnDownloadStateChange(this, DownloadState.Ready);
                }
            }
        }

        private void ConvertToMp3()
        {
            if (Status.DownloadState == DownloadState.ConvertAudioStart || Status.DownloadState == DownloadState.Ready ||
                Status.DownloadState == DownloadState.Error)
                return;
            Status.DownloadState = DownloadState.ConvertAudioStart; 
            var audioFile = Path.Combine(_downloadFolder, VideoInfo.Title) + ".mp3";
            var videoFile = Path.Combine(_videoFolder, VideoInfo.Title) + VideoInfo.Extension;
            if (!File.Exists(videoFile)) return;
            if (File.Exists(audioFile)) File.Delete(audioFile);
            var process = new Process {
                EnableRaisingEvents = true,
                StartInfo = {
                    FileName = _applicationPath + "\\Executables\\ffmpeg.exe",
                    Arguments = String.Format("-i \"{0}\" -acodec mp3 -y -ac 2 -ab 160 \"{1}\"", videoFile, audioFile),
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Exited += ProcessExited;
            try {
                process.Start();
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.ConvertAudioStart);
            }
            catch (Exception e)
            {
                Message = e.Message;
                Status.DownloadState = DownloadState.Error;
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.Error);
            }
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            var process = sender as Process;
            if (process == null) return;
            Debug.Write(process.ExitCode);
            if (process.ExitCode == 0)
            {
                Status.DownloadState = DownloadState.Ready;
                Message = process.StandardError.ReadToEnd();
                Debug.Write(Message);
                Debug.Write(process.StandardOutput.ReadToEnd());
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.Ready);
            } else {
                Message = process.StandardError.ReadToEnd();
                Debug.Write(Message);
                Debug.Write(process.StandardOutput.ReadToEnd());
                Status.DownloadState = DownloadState.Error;
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.Error);
            }
        }
    }
}