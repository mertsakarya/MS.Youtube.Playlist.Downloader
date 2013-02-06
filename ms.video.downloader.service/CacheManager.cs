using System.Collections.Generic;
using System.IO;
using System.Linq;
using ms.video.downloader.service.Download;

namespace ms.video.downloader.service
{

    public class UrlCache
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public long Length { get; set; }
    }

    public class VideoCache
    {
        public string VideoId { get; set; }
        public string FileName { get; set; }
        public bool Finished { get; set; }
    }

    public class CacheManager
    {
        private const string UrlsFileName = "urls.json";
        private const string VideosFileName = "videos.json";

        private static CacheManager _instance;
        private readonly List<UrlCache> _urlCaches;
        private readonly List<VideoCache> _videoCaches;

        private readonly  Settings _settings;
        private readonly StorageFolder _appVersionFolder;
        public Dictionary<string, UrlCache> UrlCaches { get; private set; }
        public Dictionary<string, VideoCache> VideoCaches { get; private set; }

        public static CacheManager Instance { get { return _instance ?? (_instance = new CacheManager(Settings.Instance)); } }

        private CacheManager(Settings settings)
        {
            _settings = settings;
            _appVersionFolder = KnownFolders.GetAppVersionFolder(_settings.Version);
            _urlCaches = GetUrls() ?? new List<UrlCache>();
            _videoCaches = GetVideos() ?? new List<VideoCache>();
            VideoCaches = new Dictionary<string, VideoCache>();
            UrlCaches = new Dictionary<string, UrlCache>();
            foreach (var item in _urlCaches.Where(item => !UrlCaches.ContainsKey(item.VideoId))) UrlCaches.Add(item.VideoId, item);
            foreach (var item in _videoCaches.Where(item => !VideoCaches.ContainsKey(item.FileName) && UrlCaches.ContainsKey(item.VideoId)))  VideoCaches.Add(item.FileName, item);
        }

        private void SetUrls(List<UrlCache> cache) { Settings.SetFile(_appVersionFolder.CreateFile(UrlsFileName), cache); }
        private List<UrlCache> GetUrls() { return Settings.GetFile<List<UrlCache>>(_appVersionFolder.CreateFile(UrlsFileName)); }
        private void SetVideos(List<VideoCache> cache) { Settings.SetFile(_appVersionFolder.CreateFile(VideosFileName), cache); }
        private List<VideoCache> GetVideos() { return Settings.GetFile<List<VideoCache>>(_appVersionFolder.CreateFile(VideosFileName)); }

        public void Save()
        {
            SetUrls(_urlCaches);
            SetVideos(_videoCaches);
        }

        public void SetUrl(string videoId, string title, long length)
        {
            if (UrlCaches.ContainsKey(videoId)) {
                var item = UrlCaches[videoId];
                item.Length = length;
                item.Title = title;
            } else {
                var cache = new UrlCache { Length = length, Title = title, VideoId = videoId };
                _urlCaches.Add(cache);
                UrlCaches.Add(cache.VideoId, cache);
            }
        }

        public void SetFinished(string videoId, string fileName, bool finished)
        {
            if (VideoCaches.ContainsKey(fileName)) {
                var item = VideoCaches[fileName];
                if (item.VideoId == videoId)
                    item.Finished = finished;
            } else {
                var videoCache = new VideoCache { VideoId = videoId, FileName = fileName, Finished = finished };
                _videoCaches.Add(videoCache);
                VideoCaches.Add(fileName, videoCache);
            }
        }

        public bool NeedsDownload(string videoId, StorageFile storageFile)
        {
            if (UrlCaches.ContainsKey(videoId) && storageFile.Exists()) {
                var fileName = storageFile.ToString();
                var urlCache = UrlCaches[videoId];
                if (storageFile.Length >= urlCache.Length) {
                    if (!VideoCaches.ContainsKey(fileName)) 
                        SetFinished(urlCache.VideoId, fileName, false);
                    else if (VideoCaches[fileName].Finished)
                        return false;
                }
                foreach (var item in VideoCaches.Where(p => p.Value.FileName != fileName && p.Value.Finished && File.Exists(p.Value.FileName) && p.Value.VideoId == videoId)) {
                    try {
                        File.Copy(item.Value.FileName, fileName, true);
                    } catch (IOException) {
                        return true;
                    }
                    SetFinished(urlCache.VideoId, fileName, true);
                    return false;
                }
            }
            return true;
        }
    }
}