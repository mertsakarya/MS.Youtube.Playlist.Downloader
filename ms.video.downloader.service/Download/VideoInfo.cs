using System;
using System.Collections.Generic;

namespace ms.video.downloader.service.Download
{
    public class VideoInfo
    {
        internal static IEnumerable<VideoInfo> Defaults = new List<VideoInfo>
        {
            new VideoInfo(5, VideoType.Flash, 240, false, AudioType.Mp3, 64),
            new VideoInfo(6, VideoType.Flash, 270, false, AudioType.Mp3, 64),
            new VideoInfo(13, VideoType.Mobile, 0, false, AudioType.Aac, 0),
            new VideoInfo(17, VideoType.Mobile, 144, false, AudioType.Aac, 24),
            new VideoInfo(18, VideoType.Mp4, 360, false, AudioType.Aac, 96),
            new VideoInfo(22, VideoType.Mp4, 720, false, AudioType.Aac, 192),
            new VideoInfo(34, VideoType.Flash, 360, false, AudioType.Aac, 128),
            new VideoInfo(35, VideoType.Flash, 480, false, AudioType.Aac, 128),
            new VideoInfo(36, VideoType.Mobile, 240, false, AudioType.Aac, 38),
            new VideoInfo(37, VideoType.Mp4, 1080, false, AudioType.Aac, 192),
            new VideoInfo(38, VideoType.Mp4, 3072, false, AudioType.Aac, 192),
            new VideoInfo(43, VideoType.WebM, 360, false, AudioType.Vorbis, 128),
            new VideoInfo(44, VideoType.WebM, 480, false, AudioType.Vorbis, 128),
            new VideoInfo(45, VideoType.WebM, 720, false, AudioType.Vorbis, 192),
            new VideoInfo(46, VideoType.WebM, 1080, false, AudioType.Vorbis, 192),
            new VideoInfo(82, VideoType.Mp4, 360, true, AudioType.Aac, 96),
            new VideoInfo(83, VideoType.Mp4, 240, true, AudioType.Aac, 96),
            new VideoInfo(84, VideoType.Mp4, 720, true, AudioType.Aac, 152),
            new VideoInfo(85, VideoType.Mp4, 520, true, AudioType.Aac, 152),
            new VideoInfo(100, VideoType.WebM, 360, true, AudioType.Vorbis, 128),
            new VideoInfo(101, VideoType.WebM, 360, true, AudioType.Vorbis, 192),
            new VideoInfo(102, VideoType.WebM, 720, true, AudioType.Vorbis, 192)
        };

        internal VideoInfo(int formatCode) : this(formatCode, VideoType.Unknown, 0, false, AudioType.Unknown, 0) { }

        private VideoInfo(int formatCode, VideoType videoType, int resolution, bool is3D, AudioType audioType, int audioBitrate)
        {
            FormatCode = formatCode;
            VideoType = videoType;
            Resolution = resolution;
            Is3D = is3D;
            AudioType = audioType;
            AudioBitrate = audioBitrate;
        }

        public int AudioBitrate { get; private set; }
        public AudioType AudioType { get; private set; }
        public bool CanExtractAudio { get { return VideoType == VideoType.Flash; } }
        public Uri DownloadUri { get; internal set; }
        public int FormatCode { get; private set; }
        public bool Is3D { get; private set; }
        public int Resolution { get; private set; }
        public string Title { get; internal set; }
        public VideoType VideoType { get; private set; }

        public string AudioExtension {
            get {
                switch (AudioType) {
                    case AudioType.Aac: return ".aac";
                    case AudioType.Mp3: return ".mp3";
                    case AudioType.Vorbis: return ".ogg";
                }
                return null;
            }
        }

        public string VideoExtension {
            get {
                switch (VideoType) {
                    case VideoType.Mp4: return ".mp4";
                    case VideoType.Mobile: return ".3gp";
                    case VideoType.Flash: return ".flv";
                    case VideoType.WebM: return ".webm";
                }
                return null;
            }
        }

        public override string ToString() { return string.Format("Full Title: {0}, Type: {1}, Resolution: {2}p", Title + VideoExtension, VideoType, Resolution); }
        public VideoInfo Clone() { return new VideoInfo(FormatCode, VideoType, Resolution, Is3D, AudioType, AudioBitrate); }
    }
}
