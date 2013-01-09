using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ms.video.downloader.service.Dowload;

namespace ms.video.downloader.service
{
    public class LocalService
    {
        private readonly string _configFile;
        private readonly string _downloadsFile;
        private readonly ApplicationConfiguration _configuration;

        public string CompanyFolder { get; private set; }
        public string AppFolder { get; private set; }
        public string AppVersionFolder { get; private set; }
        public string Version { get; private set; }
        public Guid Guid { get { return _configuration.Guid; } }
        public bool FirstTime { get; private set; }

        public LocalService()
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
                        youtubeListEntry.ExecutionStatus = itemList.ExecutionStatus;
                        youtubeListEntry.ThumbnailUrl = itemList.ThumbnailUrl;
                        foreach (var item in itemList.List) {
                            var youtubeEntry = YoutubeEntry.Create(new Uri(item.Url), youtubeListEntry);
                            youtubeEntry.ThumbnailUrl = item.ThumbnailUrl;
                            youtubeEntry.Title = item.Title;
                            youtubeEntry.ExecutionStatus = item.ExecutionStatus;
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

}
