using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ms.video.downloader.service.Dowload
{
    public class DownloadLists : Feed
    {
        private readonly Settings _settings;
        private readonly ListDownloadStatusEventHandler _onStatusChanged;

        public DownloadLists(ListDownloadStatusEventHandler onStatusChanged)
        {
            _settings = Settings.Instance;
            _onStatusChanged = onStatusChanged;
            if (_settings.FillDownloadLists(this) && Entries.Count > 0) StartDownload(); else Entries.Clear();
        }

        public Feed Add(IEnumerable entries, MediaType mediaType)
        {
            var downloadList = SoftAdd(entries, mediaType);
            Task.Factory.StartNew(downloadList.Download);
            return downloadList;
        }

        public DownloadList SoftAdd(IEnumerable entries, MediaType mediaType)
        {
            var downloadList = new DownloadList(mediaType, OnDownloadStatusChange);
            foreach (var member in entries.Cast<YoutubeEntry>().Where(member => member.Uri != null)) downloadList.Entries.Add(member.Clone());
            if (downloadList.Entries.Count > 0) Entries.Add(downloadList);
            return downloadList;
        }

        private void StartDownload()
        {
            foreach (var downloadItems in Entries.Cast<DownloadList>().Where(downloadItems => downloadItems.Entries.Count > 0)) {
                var items = downloadItems;
                Task.Factory.StartNew(items.Download);
            }
        }

        private void OnDownloadStatusChange(Feed downloadList, Feed entry, DownloadState downloadState, double percentage)
        {
            if (Entries.Count <= 0 || _onStatusChanged == null) return;
            if (downloadState == DownloadState.Deleted) {
                _settings.SaveDownloadLists(this);
                UpdateStatus(downloadList, entry, DownloadState.AllFinished, 100.0);
                return;
            }
            var finishedCount = 0;
            var average = 0.0;
            foreach (var en in Entries) {
                if (en.DownloadState == DownloadState.AllFinished)
                    finishedCount++;
                average += en.Percentage;
            }
            average = average/Entries.Count;

            if(downloadState == DownloadState.Error || downloadState == DownloadState.Ready || downloadState == DownloadState.AllStart || downloadState == DownloadState.AllFinished)
                _settings.SaveDownloadLists(this);
            if (downloadState == DownloadState.DownloadProgressChanged)
                UpdateStatus(downloadList, entry, DownloadState.DownloadProgressChanged, average);
            else if (finishedCount == Entries.Count) 
                UpdateStatus(downloadList, entry, DownloadState.AllFinished, 100.0);
            else if (Entries.Count == 1 && downloadState == DownloadState.AllStart)
                UpdateStatus(downloadList, entry, DownloadState.AllStart, 0.0);
            else if(_onStatusChanged != null && 
                    !(downloadState == DownloadState.AllFinished || 
                        downloadState == DownloadState.AllStart || 
                        downloadState == DownloadState.DownloadProgressChanged
                    )) 
                _onStatusChanged(downloadList, entry, downloadState, percentage);
        }

        private void UpdateStatus(Feed downloadList, Feed entry, DownloadState state, double percentage)
        {
            DownloadState = state;
            Percentage = percentage;
            if (_onStatusChanged != null) _onStatusChanged(downloadList, entry, DownloadState, Percentage);
        }

        public new string Title { get { return "TOTAL"; } }


        public void UpdatePlaylists()
        {
            var list = new List<string> { "#EXTM3U" };
            var files = Directory.EnumerateFiles(KnownFolders.MusicLibrary.FolderName, "*.mp3", SearchOption.AllDirectories);
            foreach (var fn in files) {
                list.Add(string.Format("#EXTINF:0,{0}", Path.GetFileNameWithoutExtension(fn)));
                list.Add(fn);
            }
            var fileName = KnownFolders.Root.FolderName + "\\" + "music.m3u8";
            if (File.Exists(fileName)) File.Delete(fileName);
            File.WriteAllLines(fileName, list, Encoding.UTF8);

            list = new List<string> { "#EXTM3U" };
            files = Directory.EnumerateFiles(KnownFolders.VideosLibrary.FolderName, "*.mp4", SearchOption.AllDirectories);
            foreach (var fn in files) {
                list.Add(string.Format("#EXTINF:0,{0}", Path.GetFileNameWithoutExtension(fn)));
                list.Add(fn);
            }
            fileName = KnownFolders.Root.FolderName + "\\" + "videos.m3u8";
            if (File.Exists(fileName)) File.Delete(fileName);
            File.WriteAllLines(fileName, list, Encoding.UTF8);

            Process.Start(fileName);
        }

        public override void Delete()
        {
            base.Delete();
            foreach (DownloadList downloadList in Entries)
                downloadList.OnListDownloadStatusChange = null;
            Entries.Clear();
            _settings.SaveDownloadLists(this);
            UpdateStatus(null, null, DownloadState.AllFinished, 100.0);
        }
    }
}
