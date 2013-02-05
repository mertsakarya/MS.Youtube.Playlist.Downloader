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
            foreach (var item in _urlCaches.Where(item => !UrlCaches.ContainsKey(item.VideoId)))
                UrlCaches.Add(item.VideoId, item);
            foreach (var item in _videoCaches.Where(item => !VideoCaches.ContainsKey(item.FileName) && UrlCaches.ContainsKey(item.VideoId))) {
                UrlCaches[item.VideoId].Videos.Add(item.FileName, item);
                VideoCaches.Add(item.FileName, item);
            }
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
        public void Set(UrlCache cache)
        {
            if (UrlCaches.ContainsKey(cache.VideoId)) {
                var item = UrlCaches[cache.VideoId];
                item.Length = cache.Length;
                item.Title = cache.Title;
            } else {
                _urlCaches.Add(cache);
                UrlCaches.Add(cache.VideoId, cache);
            }
        }

        public void Set(VideoCache cache)
        {
            if(!UrlCaches.ContainsKey(cache.VideoId)) throw new Exception("Video Id not found in cache!!!");
            var urlCache = UrlCaches[cache.VideoId];
            if (VideoCaches.ContainsKey(cache.FileName)) {
                var item = VideoCaches[cache.VideoId];
                item.AudioFinished = cache.AudioFinished;
                item.HasAudio = cache.HasAudio;
                item.Finished = cache.Finished;
                item.VideoId = cache.VideoId;
            } else {
                urlCache.Videos.Add(cache.FileName, cache);
                _videoCaches.Add(cache);
                VideoCaches.Add(cache.VideoId, cache);
            }
        }

        public void Delete(UrlCache cache)
        {
            var videoId = cache.VideoId;
            if (!UrlCaches.ContainsKey(videoId)) return;
            foreach(var item in UrlCaches[videoId].Videos)
                Delete(item.Value);
        }
        
        public void Delete(VideoCache cache)
        {
            var videoId = cache.VideoId;
            var fileName = cache.FileName;
            if (VideoCaches.ContainsKey(fileName))
                VideoCaches.Remove(fileName);
            _videoCaches.Remove(cache);
            if (UrlCaches.ContainsKey(videoId)) {
                var urlCache = UrlCaches[videoId];
                if (urlCache.Videos.ContainsKey(fileName)) {
                    urlCache.Videos.Remove(fileName);
                }
                if (urlCache.Videos.Count == 0) {
                    UrlCaches.Remove(videoId);
                    _urlCaches.Remove(urlCache);
                }
            }
        }

        public void Finished(string videoId, string fileName)
        {
            if (VideoCaches.ContainsKey(fileName)) {
                VideoCaches[fileName].Finished = true;
            }
            else {
                var videoCache = new VideoCache() {VideoId = videoId, FileName = fileName, Finished = true};
                Set(videoCache);
            }
        }

        public void SetTotal(string videoId, string title, long length)
        {
            UrlCache urlCache;
            if (UrlCaches.ContainsKey(videoId)) {
                urlCache = UrlCaches[videoId];
                urlCache.Title = title;
                urlCache.Length = length;
            }
            else {
                urlCache = new UrlCache() {Length = length, Title = title, VideoId = videoId};
                Set(urlCache);                
            }

        }
    }

    public class UrlCache
    {
        public UrlCache()
        {
            Videos = new Dictionary<string, VideoCache>();
        }
        public string VideoId { get; set; }
        public string Title { get; set; }
        public long Length { get; set; }
        [JsonIgnore]
        public Dictionary<string, VideoCache> Videos { get; set; }

    }

    public class VideoCache
    {
        public string VideoId { get; set; }
        public string FileName { get; set; }
        public bool HasAudio { get; set; }
        public bool AudioFinished { get; set; }
        public bool Finished { get; set; }
    }

    //public interface _IDbCache
    //{
    //    void CreateDb();
    //    UrlCache GetUrl(string url);
    //    void AddUrl(string url, string title, long length);
    //    void AddFile(long urlId, string fileName, bool finished = false);
    //    IList<UrlFileCache> GetFiles(long urlId);

    //    void SetApplicationConfiguration(ApplicationConfiguration configuration);
    //    ApplicationConfiguration GetApplicationConfiguration();
    //    void SetDownloadLists(DownloadEntry listsEntry);
    //    DownloadEntry GetDownloadLists();
    //}

    //public class _DbCache : IDbCache
    //{
    //    private readonly string _connectionString;

    //    #region Data helper

    //    private void ExecuteNonQueryCommand(string command)
    //    {
    //        using (var conn = new SQLiteConnection(_connectionString)) {
    //            conn.Open();
    //            using (var mytransaction = conn.BeginTransaction()) {
    //                using (var mycommand = new SQLiteCommand(conn)) {
    //                    mycommand.CommandText = command;
    //                    mycommand.ExecuteNonQuery();
    //                }
    //                mytransaction.Commit();
    //            }
    //        }
    //    }

    //    private void ExecuteNonQueryCommand(SQLiteCommand command)
    //    {
    //        using (var conn = new SQLiteConnection(_connectionString)) {
    //            conn.Open();
    //            using (var mytransaction = conn.BeginTransaction()) {
    //                command.Connection = conn;
    //                command.ExecuteNonQuery();
    //                mytransaction.Commit();
    //            }
    //        }
    //    }

    //    private object ExecuteScalarCommand(SQLiteCommand command)
    //    {
    //        using (var conn = new SQLiteConnection(_connectionString)) {
    //            conn.Open();
    //            command.Connection = conn;
    //            try {
    //                var val = command.ExecuteScalar();
    //                return val;
    //            }
    //            catch (Exception ex) {
    //                return null;
    //            }
    //        }
    //    }

    //    #endregion

    //    private readonly string _fileName;

    //    public DbCache(string fileName)
    //    {
    //        _fileName = fileName;
    //        _connectionString = "Pooling=true;Data Source=" + _fileName;
    //        CreateDb();
    //    }

    //    public void CreateDb()
    //    {
    //        try {
    //            if (!File.Exists(_fileName)) {
    //                SQLiteConnection.CreateFile(_fileName);
    //                ExecuteNonQueryCommand(Resources.sqlite);
    //            }
    //        }
    //        catch (SQLiteException) {
    //            File.Delete(_fileName);
    //        }
    //        Task.Factory.StartNew(() => {
    //            try {
    //                ExecuteNonQueryCommand("REINDEX");
    //                CheckAllVideos();
    //            }
    //            catch {}
    //        });
    //    }

    //    public UrlCache GetUrl(string url)
    //    {
    //        try {
    //            using (var conn = new SQLiteConnection(_connectionString)) {
    //                conn.Open();
    //                var command = new SQLiteCommand(conn) { CommandText = "SELECT id, length, title FROM urls WHERE url=@url"};
    //                command.Parameters.Add(new SQLiteParameter {ParameterName = "@url", Value = url});
    //                using (
    //                    var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection)) {
    //                    if (!reader.HasRows) return null;
    //                    return reader.Read()
    //                               ? new UrlCache {
    //                                   Id = reader.GetInt64(0),
    //                                   Length = reader.GetInt64(1),
    //                                   Title = reader.GetString(2),
    //                                   Url = url
    //                               }
    //                               : null;
    //                }
    //            }
    //        }
    //        catch (SQLiteException) {
    //            return null;
    //        }
    //    }

    //    public void AddUrl(string url, string title, long length)
    //    {
    //        try {
    //            var urlCache = GetUrl(url);
    //            if (urlCache != null && urlCache.Length != length) {
    //                UpdateUrl(url, length);
    //                return;
    //            }
    //            var command = new SQLiteCommand {
    //                CommandText = "INSERT INTO urls (url, length, title) VALUES (@url, @length, @title)"
    //            };
    //            command.Parameters.Add(new SQLiteParameter {ParameterName = "@url", Value = url});
    //            command.Parameters.Add(new SQLiteParameter {ParameterName = "@length", Value = length});
    //            command.Parameters.Add(new SQLiteParameter {ParameterName = "@title", Value = title});
    //            ExecuteNonQueryCommand(command);
    //        }
    //        catch (SQLiteException) {}
    //    }

    //    public void UpdateUrl(string url, long length)
    //    {
    //        var command = new SQLiteCommand {CommandText = "UPDATE urls SET length=@length WHERE url=@url;"};
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@url", Value = url});
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@length", Value = length});
    //        ExecuteNonQueryCommand(command);
    //    }

    //    public void AddFile(long urlId, string fileName, bool finished = false)
    //    {
    //        var urlFileCache = GetFile(fileName);
    //        if (urlFileCache != null) {
    //            if (finished != urlFileCache.Finished) UpdateFile(fileName, finished);
    //            return;
    //        }
    //        var source = "videos";
    //        var commandText = String.Format("INSERT INTO {0} (urlId, fileName, finished) VALUES (@urlId, @fileName, @finished)", source);
    //        var command = new SQLiteCommand {CommandText = commandText};
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@urlId", Value = urlId});
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@fileName", Value = fileName});
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@finished", Value = ((finished) ? 1 : 0)});
    //        try {
    //            ExecuteNonQueryCommand(command);
    //        }
    //        catch (SQLiteException) {}
    //    }

    //    public UrlFileCache GetFile(string fileName)
    //    {
    //        try {
    //            var source = "videos";
    //            var commandText = String.Format("SELECT id, finished FROM {0} WHERE fileName=@fileName", source);
    //            using (var conn = new SQLiteConnection(_connectionString)) {
    //                conn.Open();
    //                var command = new SQLiteCommand(conn) {CommandText = commandText};
    //                command.Parameters.Add(new SQLiteParameter {ParameterName = "@fileName", Value = fileName});
    //                using (
    //                    var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection)) {
    //                    if (!reader.HasRows) return null;
    //                    while (reader.Read()) {
    //                        var urlFileCache = new UrlFileCache {
    //                            Id = reader.GetInt64(0),
    //                            FileName = fileName,
    //                            Finished = (reader.GetInt64(1) == 1),
    //                        };
    //                        return urlFileCache;
    //                    }
    //                }
    //            }
    //        }
    //        catch (SQLiteException) {}
    //        return null;
    //    }

    //    public IList<UrlFileCache> GetFiles(long urlId)
    //    {
    //        try {
    //            var source = "videos";
    //            var commandText = String.Format("SELECT id, fileName, finished FROM {0} WHERE urlId=@urlId", source);
    //            using (var conn = new SQLiteConnection(_connectionString)) {
    //                conn.Open();
    //                var command = new SQLiteCommand(conn) {CommandText = commandText};
    //                command.Parameters.Add(new SQLiteParameter {ParameterName = "@urlId", Value = urlId});
    //                using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection)) {
    //                    var list = new List<UrlFileCache>();
    //                    if (!reader.HasRows) return null;
    //                    while (reader.Read()) {
    //                        var urlFileCache = new UrlFileCache {
    //                            Id = reader.GetInt64(0),
    //                            UrlId = urlId,
    //                            FileName = reader.GetString(1),
    //                            Finished = (reader.GetInt64(2) == 1),
    //                        };
    //                        list.Add(urlFileCache);
    //                    }
    //                    return list;
    //                }
    //            }
    //        }
    //        catch (SQLiteException) {
    //            return null;
    //        }
    //    }

    //    private void SetKeyValue<T>(string key, T value)
    //    {
    //        var data = JsonConvert.SerializeObject(value);
    //        var command = new SQLiteCommand {
    //            CommandText = "INSERT OR REPLACE INTO keyValues (key, value) VALUES (@key, @value)"
    //        };
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@key", Value = key});
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@value", Value = data});
    //        ExecuteNonQueryCommand(command);
    //    }

    //    private T GetKeyValue<T>(string key)
    //    {
    //        var command = new SQLiteCommand {CommandText = "SELECT value FROM keyValues WHERE key=@key"};
    //        command.Parameters.Add(new SQLiteParameter {ParameterName = "@key", Value = key});
    //        var value = ExecuteScalarCommand(command) as string;
    //        if (String.IsNullOrEmpty(value)) return default(T);
    //        var result = JsonConvert.DeserializeObject<T>(value);
    //        return result;
    //    }

    //    public void SetApplicationConfiguration(ApplicationConfiguration configuration)
    //    {
    //        SetKeyValue("config", configuration);
    //    }

    //    public void SetDownloadLists(DownloadEntry listsEntry)
    //    {
    //        SetKeyValue("lists", listsEntry);
    //    }

    //    public ApplicationConfiguration GetApplicationConfiguration()
    //    {
    //        return GetKeyValue<ApplicationConfiguration>("config");
    //    }

    //    public DownloadEntry GetDownloadLists()
    //    {
    //        return GetKeyValue<DownloadEntry>("lists");
    //    }


    //    private void UpdateFile(string fileName, bool finished)
    //    {
    //        using (var conn = new SQLiteConnection(_connectionString)) {
    //            conn.Open();
    //            UpdateFile(conn, fileName, finished);
    //        }
    //    }
    //    private void UpdateFile(SQLiteConnection conn, string fileName, bool finished)
    //    {
    //        using (var command = new SQLiteCommand("UPDATE videos SET finished=@finished WHERE fileName=@fileName", conn)) {
    //            command.Parameters.Add(new SQLiteParameter { ParameterName = "@fileName", Value = fileName });
    //            command.Parameters.Add(new SQLiteParameter { ParameterName = "@finished", Value = ((finished) ? 1 : 0) });
    //            command.ExecuteNonQuery();
    //        }
    //    }

    //    private void DeleteFile(SQLiteConnection conn, string fileName, long urlId)
    //    {
    //        var command2 = new SQLiteCommand("DELETE from videos WHERE fileName=@fileName", conn);
    //        command2.Parameters.Add(new SQLiteParameter { ParameterName = "@fileName", Value = fileName });
    //        command2.ExecuteNonQuery();
    //        var command3 = new SQLiteCommand("SELECT urlId from videos WHERE urlId=@urlId", conn);
    //        command3.Parameters.Add(new SQLiteParameter { ParameterName = "@urlId", Value = urlId });
    //        var reader = command3.ExecuteReader();
    //        if (!reader.HasRows) {
    //            var command = new SQLiteCommand("DELETE from urls WHERE id=@urlId", conn);
    //            command.Parameters.Add(new SQLiteParameter { ParameterName = "@urlId", Value = urlId });
    //            command.ExecuteNonQuery();
    //        }
    //    }

    //    private void CheckAllVideos()
    //    {
    //        using (var conn = new SQLiteConnection(_connectionString)) {
    //            conn.Open();
    //            using (var mytransaction = conn.BeginTransaction()) {
    //                try {
    //                    using (
    //                        var command = new SQLiteCommand("SELECT fileName, finished, length, urlId  FROM urls inner join videos on videos.urlid = urls.id", conn)) {
    //                        using (var reader = command.ExecuteReader(CommandBehavior.SingleResult)) {
    //                            while (reader.Read()) {
    //                                var fileName = reader.GetString(0);
    //                                var finished = (reader.GetInt64(1) != 0);
    //                                var videoLength = reader.GetInt64(2);
    //                                var urlId = reader.GetInt64(3);
    //                                long fileLength = 0;
    //                                if (File.Exists(fileName))
    //                                    fileLength = new FileInfo(fileName).Length;
    //                                else
    //                                    DeleteFile(conn, fileName, urlId);
    //                                var shouldBeFinished = fileLength == videoLength;
    //                                if (shouldBeFinished && finished) continue;
    //                                if (!shouldBeFinished && !finished) continue;
    //                                UpdateFile(conn, fileName, shouldBeFinished);
    //                            }
    //                        }
    //                    }
    //                    mytransaction.Commit();

    //                }
    //                catch {
    //                    mytransaction.Rollback();
    //                }
    //            }
    //        }
    //    }
    //}
    #endregion
}
