using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ms.video.downloader.service.Dowload;
using ms.video.downloader.service.Properties;

namespace ms.video.downloader.service
{
    public class Settings
    {
        private static Settings _instance;

        public static Settings Instance
        {
            get
            {
                if (_instance == null) _instance = new Settings();
                return _instance;
            }
        }

        private const string DevelopmentVersion = "0.0.0.1";
        private const string SettingsFileName = "settings.json";
        private const string DownloadsFileName = "downloads.json";

        private readonly ApplicationConfiguration _configuration;
        public string CompanyFolder { get; private set; }
        public string AppFolder { get; private set; }
        public string AppVersionFolder { get; private set; }
        public string Version { get; private set; }

        public bool IsDevelopment
        {
            get { return Version.Equals(DevelopmentVersion); }
        }

        public Guid Guid
        {
            get { return _configuration.Guid; }
        }

        public bool FirstTime { get; private set; }

        private Settings()
        {
            FirstTime = false;

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
                System.Deployment.Application.ApplicationDeployment cd =
                    System.Deployment.Application.ApplicationDeployment.CurrentDeployment;
                Version = cd.CurrentVersion.ToString();
            }
            else {
                Version = DevelopmentVersion;
            }

            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            CompanyFolder = path + @"\ms";
            if (!Directory.Exists(CompanyFolder)) Directory.CreateDirectory(CompanyFolder);
            AppFolder = CompanyFolder + @"\ms.video.downloader";
            if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
            AppVersionFolder = AppFolder + @"\" + Version;
            if (!Directory.Exists(AppVersionFolder)) Directory.CreateDirectory(AppVersionFolder);
            //DbCache = new DbCache(AppVersionFolder + "\\ms.video.downloader.settings.sqlite");
            _configuration = GetApplicationConfiguration();

            if (_configuration != null) return;
            FirstTime = true;
            _configuration = new ApplicationConfiguration {Guid = Guid.NewGuid()};
            SetApplicationConfiguration(_configuration);
        }

        private void SetApplicationConfiguration(ApplicationConfiguration configuration) { SetFile(Path.Combine(AppVersionFolder, SettingsFileName), configuration); }
        private ApplicationConfiguration GetApplicationConfiguration() { return GetFile<ApplicationConfiguration>(Path.Combine(AppVersionFolder, SettingsFileName)); }
        private void SetDownloadLists(DownloadEntry entries) { SetFile(Path.Combine(AppVersionFolder, DownloadsFileName), entries); }
        private DownloadEntry GetDownloadLists() { return GetFile<DownloadEntry>(Path.Combine(AppVersionFolder, DownloadsFileName)); }

        public static void SetFile<T>(string fileName, T obj)
        {
            using (var writer = new StreamWriter(fileName)) writer.Write( JsonConvert.SerializeObject(obj));
        }


        public static T GetFile<T>(string fileName)
        {
            if (!File.Exists(fileName)) return default(T);
            using (var reader = new StreamReader(fileName)) {
                var text = reader.ReadToEnd();
                var obj = JsonConvert.DeserializeObject<T>(text);
                return obj;
            }
        }


        #region Load / Save DownloadLists

        public void SaveDownloadLists(DownloadLists lists)
        {
            try {
                var listsEntry = new DownloadEntry {
                    Title = lists.Title,
                    ThumbnailUrl = lists.ThumbnailUrl,
                    ExecutionStatus = lists.ExecutionStatus
                };
                foreach (DownloadList list in lists.Entries) {
                    if (list.DownloadState == DownloadState.AllFinished || list.Entries.Count <= 0) continue;
                    var entry = new DownloadEntry {
                        Title = list.Title,
                        ThumbnailUrl = list.ThumbnailUrl,
                        MediaType = list.MediaType,
                        ExecutionStatus = list.ExecutionStatus,
                        Url = ""
                    };
                    var firstEntry = list.Entries[0] as YoutubeEntry;
                    if (firstEntry == null) continue;
                    if (firstEntry.Parent != null) {
                        entry.Url = String.Format("{0}", firstEntry.Parent.Uri);
                        entry.Title = firstEntry.Parent.Title;
                    }
                    foreach (YoutubeEntry youtubeEntry in list.Entries)
                        entry.List.Add(new DownloadEntry {
                            Title = youtubeEntry.Title,
                            Url = youtubeEntry.Uri.ToString(),
                            MediaType = youtubeEntry.MediaType,
                            ThumbnailUrl = youtubeEntry.ThumbnailUrl,
                            ExecutionStatus = youtubeEntry.ExecutionStatus
                        });
                    listsEntry.List.Add(entry);
                }
                SetDownloadLists(listsEntry);
            }
            catch {}
        }

        public bool FillDownloadLists(DownloadLists lists)
        {
            try {
                lists.Entries.Clear();
                var listsEntry = GetDownloadLists() ?? new DownloadEntry();
                if (listsEntry.List != null && listsEntry.List.Count > 0) {
                    foreach (var itemList in listsEntry.List) {
                        var youtubeEntries = new List<YoutubeEntry>();
                        var mediaType = itemList.MediaType;
                        Uri uri;
                        var youtubeListEntry =
                            YoutubeEntry.Create(Uri.TryCreate(itemList.Url, UriKind.Absolute, out uri) ? uri : null);
                        youtubeListEntry.Title = itemList.Title;
                        SetExecutionStatus(youtubeListEntry, itemList);
                        youtubeListEntry.ThumbnailUrl = itemList.ThumbnailUrl;
                        foreach (var item in itemList.List) {
                            var youtubeEntry = YoutubeEntry.Create(new Uri(item.Url), youtubeListEntry);
                            youtubeEntry.ThumbnailUrl = item.ThumbnailUrl;
                            youtubeEntry.Title = item.Title;
                            SetExecutionStatus(youtubeEntry, item);
                            youtubeEntries.Add(youtubeEntry);
                        }
                        if (youtubeEntries.Count > 0)
                            lists.SoftAdd(youtubeEntries, mediaType);
                    }
                }
                return true;
            }
            catch {
                return false;
            }
        }

        private static void SetExecutionStatus(Feed feed, DownloadEntry entry)
        {
            feed.ExecutionStatus = entry.ExecutionStatus;
            if (feed.ExecutionStatus == ExecutionStatus.Deleted) feed.DownloadState = DownloadState.Deleted;
        }

        #endregion
    }

    #region Download

    public class DownloadEntry
    {
        public DownloadEntry()
        {
            Url = "";
            ThumbnailUrl = "";
            Title = "";
            ExecutionStatus = ExecutionStatus.Normal;
            List = new List<DownloadEntry>();
        }

        public MediaType MediaType { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Title { get; set; }
        public ExecutionStatus ExecutionStatus { get; set; }
        public List<DownloadEntry> List { get; private set; }
    }

    public class CacheManager
    {
        private const string UrlsFileName = "urls.json";
        private const string VideosFileName = "videos.json";

        private static CacheManager _instance;
        private readonly List<UrlCache> _urlCaches;
        private readonly List<VideoCache> _videoCaches;

        private static Settings _settings;
        public Dictionary<string, UrlCache> UrlCaches { get; private set; }
        public Dictionary<string, VideoCache> VideoCaches { get; private set; }

        public static CacheManager Instance {
            get
            { 
                if (_instance == null) {
                    _settings = Settings.Instance;
                    _instance = new CacheManager();
                }
                return _instance;
            }
        }

        private CacheManager()
        {
            _urlCaches = GetUrls() ?? new List<UrlCache>();
            _videoCaches = GetVideos() ?? new List<VideoCache>();
            VideoCaches = new Dictionary<string, VideoCache>();
            UrlCaches = new Dictionary<string, UrlCache>();
            foreach (var item in _urlCaches.Where(item => !UrlCaches.ContainsKey(item.VideoId))) UrlCaches.Add(item.VideoId, item);
            foreach (var item in _videoCaches.Where(item => !VideoCaches.ContainsKey(item.FileName) && UrlCaches.ContainsKey(item.VideoId)))  VideoCaches.Add(item.FileName, item);
        }

        private void SetUrls(List<UrlCache> cache) { Settings.SetFile(Path.Combine(_settings.AppVersionFolder, UrlsFileName), cache); }
        private List<UrlCache> GetUrls() { return Settings.GetFile<List<UrlCache>>(Path.Combine(_settings.AppVersionFolder, UrlsFileName)); }
        private void SetVideos(List<VideoCache> cache) { Settings.SetFile(Path.Combine(_settings.AppVersionFolder, VideosFileName), cache); }
        private List<VideoCache> GetVideos() { return Settings.GetFile<List<VideoCache>>(Path.Combine(_settings.AppVersionFolder, VideosFileName)); }

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
    #endregion
}
