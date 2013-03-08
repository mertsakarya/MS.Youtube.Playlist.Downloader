using System;
using System.Collections.Generic;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using ms.video.downloader.service.Download;
using ms.video.downloader.service.S3;

namespace ms.video.downloader.service
{
    public class Settings
    {
        private static Settings _instance;

        public static Settings Instance
        {
            get { return _instance ?? (_instance = new Settings()); }
        }

        private static string DevelopmentVersion = "0.0.0.1";
        private static string SettingsFileName = "settings.json";
        private static string DownloadsFileName = "downloads.json";

        private readonly ApplicationConfiguration _configuration;
        private readonly StorageFolder _appVersionFolder;
        private S3FileSystem _fileSystem;
        public string Version { get; private set; }

        public bool IsDevelopment
        {
            get { return Version.Equals(DevelopmentVersion); }
        }

        public ApplicationConfiguration ApplicationConfiguration
        {
            get { return _configuration; }
        }

        public S3FileSystem FileSystem
        {
            get { return _fileSystem; }
        }

        public void UpdateConfiguration()
        {
            SetApplicationConfiguration(_configuration);
            _fileSystem = new S3FileSystem(_configuration.S3AccessKey, _configuration.S3SecretAccessKey, _configuration.S3RegionHost, _configuration.S3BucketName);
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
            _appVersionFolder = KnownFolders.GetAppVersionFolder(Version);
            //DbCache = new DbCache(AppVersionFolder + "\\ms.video.downloader.settings.sqlite");
            _configuration = GetApplicationConfiguration();

            if (_configuration != null) {
                _fileSystem = new S3FileSystem(_configuration.S3AccessKey, _configuration.S3SecretAccessKey, _configuration.S3RegionHost, _configuration.S3BucketName);
                return;
            }
            FirstTime = true;
            _configuration = new ApplicationConfiguration {Guid = Guid.NewGuid(), S3IsActive = false};
            SetApplicationConfiguration(_configuration);
            _fileSystem = new S3FileSystem(_configuration.S3AccessKey, _configuration.S3SecretAccessKey, _configuration.S3RegionHost, _configuration.S3BucketName);
        }

        private void SetApplicationConfiguration(ApplicationConfiguration configuration) { SetFile(_appVersionFolder.CreateFile(SettingsFileName), configuration); }
        private ApplicationConfiguration GetApplicationConfiguration() { return GetFile<ApplicationConfiguration>(_appVersionFolder.CreateFile(SettingsFileName)); }
        private void SetDownloadLists(DownloadEntry entries) { SetFile(_appVersionFolder.CreateFile(DownloadsFileName), entries); }
        private DownloadEntry GetDownloadLists() { return GetFile<DownloadEntry>(_appVersionFolder.CreateFile(DownloadsFileName)); }

        public static void SetFile<T>(StorageFile file, T obj)
        {
            file.Write( JsonConvert.SerializeObject(obj));
        }


        public static T GetFile<T>(StorageFile file)
        {
            if (!file.Exists()) return default(T);
            var text = file.Read();
            var obj = JsonConvert.DeserializeObject<T>(text);
            return obj;
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

    #endregion
}
