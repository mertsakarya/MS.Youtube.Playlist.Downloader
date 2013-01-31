using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ms.video.downloader.service.Dowload;
using ms.video.downloader.service.Properties;

namespace ms.video.downloader.service
{
    public class LocalService
    {
        private static LocalService _instance = null;
        public static LocalService Instance {
            get
            {
                if(_instance == null) _instance = new LocalService();
                return _instance;
            }
        }
        private readonly string _configFile;
        private readonly string _downloadsFile;
        private readonly ApplicationConfiguration _configuration;
        public IDbCache DbCache { get; private set; }
        public string CompanyFolder { get; private set; }
        public string AppFolder { get; private set; }
        public string AppVersionFolder { get; private set; }
        public string Version { get; private set; }
        public Guid Guid { get { return _configuration.Guid; } }
        public bool FirstTime { get; private set; }

        private LocalService()
        {
            FirstTime = false;
            
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
                System.Deployment.Application.ApplicationDeployment cd = System.Deployment.Application.ApplicationDeployment.CurrentDeployment;
                Version = cd.CurrentVersion.ToString();
            } else 
                Version = "0.0.0.1";

            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            CompanyFolder = path + @"\ms";
            if (!Directory.Exists(CompanyFolder)) Directory.CreateDirectory(CompanyFolder);
            AppFolder = CompanyFolder + @"\ms.video.downloader";
            if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
            AppVersionFolder = AppFolder + @"\" + Version;
            if (!Directory.Exists(AppVersionFolder)) Directory.CreateDirectory(AppVersionFolder);
            _configFile = AppFolder + "\\applicationConfiguration.json";
            _downloadsFile = AppVersionFolder + "\\downloads.json";
            DbCache = new DbCache(AppFolder + "\\dbCache.sqlite3");
            if (!File.Exists(_configFile)) {
                FirstTime = true;
                _configuration = new ApplicationConfiguration { Guid = Guid.NewGuid() };
                using (var file = new StreamWriter(_configFile)) {
                    file.Write(JsonConvert.SerializeObject(_configuration));
                }
            }
            using (var file = new StreamReader(_configFile)) {
                _configuration = JsonConvert.DeserializeObject<ApplicationConfiguration>(file.ReadToEnd());
            }
        }

        #region Load / Save DownloadLists 

        public void SaveDownloadLists(DownloadLists lists)
        {
            try {
                var listsEntry = new DownloadEntry {Title = lists.Title, ThumbnailUrl = lists.ThumbnailUrl, ExecutionStatus = lists.ExecutionStatus };
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
                using (var file = new StreamWriter(_downloadsFile)) file.Write(JsonConvert.SerializeObject(listsEntry));
            }
            catch {}
        }

        public bool FillDownloadLists(DownloadLists lists)
        {
            try {
                lists.Entries.Clear();
                DownloadEntry listsEntry;
                if (!File.Exists(_downloadsFile))
                    listsEntry = new DownloadEntry();
                else
                    using (var file = new StreamReader(_downloadsFile))
                        listsEntry = JsonConvert.DeserializeObject<DownloadEntry>(file.ReadToEnd());
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
            if (feed.ExecutionStatus==  ExecutionStatus.Deleted) feed.DownloadState = DownloadState.Deleted;
        }

        private class DownloadEntry
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

        #endregion

    }

    #region Download

    public class UrlCache
    {
        public long Id { get; set; }
        public string Url { get; set; }
        public long Length { get; set; }
    }

    public class UrlFileCache
    {
        public long Id { get; set; }
        public long UrlId { get; set; }
        public string FileName { get; set; }
        public bool Finished { get; set; }
        public MediaType MediaType { get; set; }
    }

    public interface IDbCache
    {
        void CreateDb();
        UrlCache GetUrl(string url);
        void AddUrl(string url, long length);
        void AddFile(long urlId, string fileName, MediaType mediaType, bool finished = false);
        IList<UrlFileCache> GetFiles(long urlId, MediaType mediaType);
    }

    public class DbCache : IDbCache
    {
        private readonly string _connectionString;

        #region Data helper

        private void ExecuteNonQueryCommand(string command)
        {
            using (var conn = new SQLiteConnection(_connectionString)) {
                conn.Open();
                using (var mytransaction = conn.BeginTransaction()) {
                    using (var mycommand = new SQLiteCommand(conn)) {
                        mycommand.CommandText = command;
                        mycommand.ExecuteNonQuery();
                    }
                    mytransaction.Commit();
                }
            }
        }

        private void ExecuteNonQueryCommand(SQLiteCommand command)
        {
            using (var conn = new SQLiteConnection(_connectionString)) {
                conn.Open();
                using (var mytransaction = conn.BeginTransaction()) {
                    command.Connection = conn;
                    command.ExecuteNonQuery();
                    mytransaction.Commit();
                }
            }
        }

        private SQLiteDataReader ExecuteReaderCommand(string command)
        {
            using (var conn = new SQLiteConnection(_connectionString)) {
                conn.Open();
                using (var mycommand = new SQLiteCommand(conn)) {
                    mycommand.CommandText = command;
                    return mycommand.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
        }

        private SQLiteDataReader ExecuteSingleRowReaderCommand(SQLiteCommand command)
        {
            using (var conn = new SQLiteConnection(_connectionString)) {
                conn.Open();
                command.Connection = conn;
                return command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection);
            }
        }

        private object ExecuteScalarCommand(SQLiteCommand command)
        {
            using (var conn = new SQLiteConnection(_connectionString)) {
                conn.Open();
                command.Connection = conn;
                var val = command.ExecuteScalar();
                return val;
            }
        }

        #endregion

        private readonly string _fileName;
        public DbCache(string fileName)
        {
            _fileName = fileName;
            _connectionString = "Pooling=true;Data Source=" + _fileName;
            CreateDb();
        }

        public void CreateDb()
        {
            try {
                if (!File.Exists(_fileName)) {
                    SQLiteConnection.CreateFile(_fileName);
                    ExecuteNonQueryCommand(Resources.sqlite);
                }
            }
            catch (SQLiteException) {
                File.Delete(_fileName);
            }
            Task.Factory.StartNew(() => {
                try {
                    ExecuteNonQueryCommand("REINDEX");
                }
                catch {}
            });
        }

        public UrlCache GetUrl(string url)
        {
            try {
                using (var conn = new SQLiteConnection(_connectionString)) {
                    conn.Open();
                    var command = new SQLiteCommand(conn) {CommandText = "SELECT id, length FROM urls WHERE url=@url"};
                    command.Parameters.Add(new SQLiteParameter {ParameterName = "@url", Value = url});
                    using (var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection)) {
                        if (!reader.HasRows) return null;
                        return reader.Read() ? new UrlCache {Id = reader.GetInt64(0), Length = reader.GetInt64(1), Url = url} : null;
                    }
                }
            }
            catch (SQLiteException) {
                return null;
            }
        }
        public void AddUrl(string url, long length)
        {
            try {
                var urlCache = GetUrl(url);
                if (urlCache != null && urlCache.Length != length) {
                    UpdateUrl(url, length);
                    return;
                }
                var command = new SQLiteCommand() { CommandText = "INSERT INTO urls (url, length) VALUES (@url, @length)" };
                command.Parameters.Add(new SQLiteParameter {ParameterName = "@url", Value = url});
                command.Parameters.Add(new SQLiteParameter {ParameterName = "@length", Value = length});
                //    return new UrlCache { Id = (long)ExecuteScalarCommand(command), Length = length, Url = url };
                ExecuteNonQueryCommand(command);
            }
            catch (SQLiteException) {}
        }
        public void UpdateUrl(string url, long length)
        {
            var command = new SQLiteCommand() { CommandText = "UPDATE urls SET length=@length WHERE url=@url;" };
            command.Parameters.Add(new SQLiteParameter { ParameterName = "@url", Value = url });
            command.Parameters.Add(new SQLiteParameter { ParameterName = "@length", Value = length });
            ExecuteNonQueryCommand(command);
        }

        public void UpdateFile(long urlId, string fileName, MediaType mediaType, bool finished)
        {
            var source = (mediaType == MediaType.Audio) ? "audios" : "videos";
            var commandText = String.Format("UPDATE {0} SET finished=@finished WHERE urlId=@urlId AND fileName=@fileName", source);
            var command = new SQLiteCommand() { CommandText = commandText };
            command.Parameters.Add(new SQLiteParameter { ParameterName = "@urlId", Value = urlId });
            command.Parameters.Add(new SQLiteParameter { ParameterName = "@fileName", Value = fileName });
            command.Parameters.Add(new SQLiteParameter { ParameterName = "@finished", Value = ((finished)?1:0) });
            ExecuteNonQueryCommand(command);
        }

        public void AddFile(long urlId, string fileName, MediaType mediaType, bool finished = false)
        {
            var urlFileCache = GetFile(urlId, fileName, mediaType);
            if (urlFileCache != null) {
                if(finished != urlFileCache.Finished) UpdateFile(urlId, fileName, mediaType, finished);
                return;
            }
            var source = (mediaType == MediaType.Audio) ? "audios" : "videos";
            var commandText = String.Format( "INSERT INTO {0} (urlId, fileName, finished) VALUES (@urlId, @fileName, @finished)", source);
            var command = new SQLiteCommand() {CommandText = commandText};
            command.Parameters.Add(new SQLiteParameter {ParameterName = "@urlId", Value = urlId});
            command.Parameters.Add(new SQLiteParameter {ParameterName = "@fileName", Value = fileName});
            command.Parameters.Add(new SQLiteParameter { ParameterName = "@finished", Value = ((finished) ? 1 : 0) });
            try {
                ExecuteNonQueryCommand(command);
            } catch (SQLiteException) { }
        }

        public UrlFileCache GetFile(long urlId, string fileName, MediaType mediaType)
        {
            try {
                var source = (mediaType == MediaType.Audio) ? "audios" : "videos";
                var commandText = String.Format("SELECT id, finished FROM {0} WHERE urlId=@urlId AND fileName=@fileName", source);
                using (var conn = new SQLiteConnection(_connectionString)) {
                    conn.Open();
                    var command = new SQLiteCommand(conn) { CommandText = commandText };
                    command.Parameters.Add(new SQLiteParameter { ParameterName = "@urlId", Value = urlId });
                    command.Parameters.Add(new SQLiteParameter { ParameterName = "@fileName", Value = fileName });
                    using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection)) {
                        if (!reader.HasRows) return null;
                        while (reader.Read()) {
                            var urlFileCache = new UrlFileCache {
                                Id = reader.GetInt64(0),
                                UrlId = urlId,
                                FileName = fileName,
                                Finished = (reader.GetInt64(1) == 1),
                                MediaType = mediaType
                            };
                            return urlFileCache;
                        }
                    }
                }
            } catch (SQLiteException) {
            }
            return null;
        }
        public IList<UrlFileCache> GetFiles(long urlId, MediaType mediaType)
        {
            try {
                var source = (mediaType == MediaType.Audio) ? "audios" : "videos";
                var commandText = String.Format("SELECT id, fileName, finished FROM {0} WHERE urlId=@urlId", source);
                using (var conn = new SQLiteConnection(_connectionString)) {
                    conn.Open();
                    var command = new SQLiteCommand(conn) { CommandText = commandText };
                    command.Parameters.Add(new SQLiteParameter { ParameterName = "@urlId", Value = urlId });
                    using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection)) {
                        var list = new List<UrlFileCache>();
                        if (!reader.HasRows) return null;
                        while (reader.Read()) {
                            var urlFileCache = new UrlFileCache {
                                Id = reader.GetInt64(0),
                                UrlId = urlId,
                                FileName = reader.GetString(1),
                                Finished = (reader.GetInt64(2) == 1),
                                MediaType = mediaType
                            };
                            list.Add(urlFileCache);
                        }
                        return list;
                    }
                }
            } catch (SQLiteException) {
                return null;
            }
        }
    
    }

    #endregion
}
