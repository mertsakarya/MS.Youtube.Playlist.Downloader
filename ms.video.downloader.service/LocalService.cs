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
        public string CompanyFolder { get; private set; }
        public string AppFolder { get; private set; }
        public string AppVersionFolder { get; private set; }
        public string Version { get; private set; }

        public Guid Guid
        {
            get { return Configuration.Guid; }
        }

        public bool FirstTime { get; private set; }

        public string[] Downloads
        {
            get { return Configuration.Downloads; }
        }

        private ApplicationConfiguration Configuration { get; set; }

        public LocalService()
        {

            FirstTime = false;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            CompanyFolder = path + @"\ms";
            if (!Directory.Exists(CompanyFolder)) Directory.CreateDirectory(CompanyFolder);
            AppFolder = CompanyFolder + @"\ms.video.downloader";
            if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
            Version = GetVersion();
            AppVersionFolder = AppFolder + @"\" + Version;
            if (!Directory.Exists(AppVersionFolder)) Directory.CreateDirectory(AppVersionFolder);
            _configFile = AppFolder + "\\applicationConfiguration.json";
            _downloadsFile = AppVersionFolder + "\\downloads.json";
            Configuration = GetConfiguration();
            Entry = GetDownloadEntry();
        }

        protected DownloadEntry Entry { get; private set; }

        private DownloadEntry GetDownloadEntry()
        {
            if (!File.Exists(_downloadsFile)) 
                using (var file = new StreamWriter(_downloadsFile)) 
                    file.Write(JsonConvert.SerializeObject(new DownloadEntry()));
            using (var file = new StreamReader(_downloadsFile)) 
                return JsonConvert.DeserializeObject<DownloadEntry>(file.ReadToEnd());
        }

        private string GetVersion()
        {
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
                System.Deployment.Application.ApplicationDeployment cd =
                    System.Deployment.Application.ApplicationDeployment.CurrentDeployment;
                return cd.CurrentVersion.ToString();
            }
            return "0.0.0.1";
        }

        private ApplicationConfiguration GetConfiguration()
        {
            if (!File.Exists(_configFile)) {
                FirstTime = true;
                Configuration = new ApplicationConfiguration {Guid = Guid.NewGuid()};
                using (var file = new StreamWriter(_configFile)) {
                    file.Write(JsonConvert.SerializeObject(Configuration));
                }
            }
            using (var file = new StreamReader(_configFile)) {
                return JsonConvert.DeserializeObject<ApplicationConfiguration>(file.ReadToEnd());
            }
        }

        public void SaveDownloadLists(DownloadLists lists)
        {
            Entry.Title = lists.Title;
            Entry.ThumbnailUrl = lists.ThumbnailUrl;
            if(Entry.List == null)
                Entry.List = new List<DownloadEntry>();
            else
                Entry.List.Clear();
            foreach (DownloadList list in lists.Entries) {
                if (list.DownloadState == DownloadState.AllFinished || list.Entries.Count <= 0) continue;
                var entry = new DownloadEntry { Title = list.Title, ThumbnailUrl = list.ThumbnailUrl, MediaType = list.MediaType, Url = "" };
                var firstEntry = list.Entries[0] as YoutubeEntry;
                if (firstEntry == null) continue;
                if (firstEntry.Parent != null) { entry.Url = String.Format("{0}", firstEntry.Parent.Uri); entry.Title = firstEntry.Parent.Title; }
                entry.List = new List<DownloadEntry>();
                foreach (YoutubeEntry youtubeEntry in list.Entries) 
                    entry.List.Add(new DownloadEntry {Title = youtubeEntry.Title, Url = youtubeEntry.Uri.ToString(), MediaType = youtubeEntry.MediaType, ThumbnailUrl = youtubeEntry.ThumbnailUrl });
                Entry.List.Add(entry);
            }

            using (var file = new StreamWriter(_downloadsFile))
                file.Write(JsonConvert.SerializeObject(Entry));
        }

        public void FillDownloadLists(DownloadLists lists)
        {
            lists.Entries.Clear();
            if (Entry.List != null && Entry.List.Count > 0) {
                foreach (var itemList in Entry.List) {
                    var youtubeEntries = new List<YoutubeEntry>();
                    var mediaType = itemList.MediaType;
                    Uri uri;
                    var youtubeListEntry = YoutubeEntry.Create(Uri.TryCreate(itemList.Url, UriKind.Absolute, out uri) ? uri : null);
                    youtubeListEntry.Title = itemList.Title;
                    youtubeListEntry.ThumbnailUrl = itemList.ThumbnailUrl;
                    foreach (var item in itemList.List) {
                        var youtubeEntry = YoutubeEntry.Create(new Uri(item.Url), youtubeListEntry);
                        youtubeEntry.ThumbnailUrl = item.ThumbnailUrl;
                        youtubeEntries.Add(youtubeEntry);
                    }
                    if (youtubeEntries.Count > 0) 
                        lists.SoftAdd(youtubeEntries, mediaType);
                }
            }
            if(lists.Entries.Count > 0)
                lists.StartDownload();
        }
    }

    public class DownloadEntry
    {
        public MediaType MediaType { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Title { get; set; }
        public List<DownloadEntry> List { get; set; }
    }
}
