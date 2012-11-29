using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.GData.Client;
using Google.YouTube;
using MS.Youtube.Downloader.Service.Youtube;

namespace MS.Youtube.Downloader.Service
{
    internal delegate void DownloadItemEventHandler(DownloadItem item, DownloadState state, double progressPercentage = 0.0);

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

            VideoInfo = null;
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        public void Download(bool ignore = false)
        {
            if (Status.DownloadState != DownloadState.Initialized) return;
            var videoInfos = DownloadUrlResolver.GetDownloadUrls(Uri.ToString());
            VideoInfo = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);

            Status.DownloadState = DownloadState.DownloadStart;
            var videoFile = Path.Combine(_videoFolder, VideoInfo.Title + VideoInfo.VideoExtension);
            if (ignore && File.Exists(videoFile)) {
                DownloadFileCompleted(null, new AsyncCompletedEventArgs(null, false, null));
            } else {
                var client = new WebClient();
                client.DownloadFileCompleted += DownloadFileCompleted;
                client.DownloadProgressChanged += OnClientOnDownloadProgressChanged;

                client.DownloadFileAsync(new Uri(VideoInfo.DownloadUrl), videoFile, this);
                
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
            var videoFile = Path.Combine(_videoFolder, VideoInfo.Title) + VideoInfo.VideoExtension;
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
                    UseShellExecute = false,
                }
            };
            //process.Exited += ProcessExited;
            try {
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, DownloadState.ConvertAudioStart);
                process.Start();
                if (!process.WaitForExit(25000)) {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                Message = process.StandardError.ReadToEnd();
                var state = (process.ExitCode == 0) ? DownloadState.Ready : DownloadState.Error;
                Status.DownloadState = state;
                Status.Percentage = 100.0;
                if (OnDownloadStateChange != null) OnDownloadStateChange(this, state);
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