using System.Collections;
using System.Linq;

namespace ms.video.downloader.service.Dowload
{
    public class DownloadLists : Feed
    {
        private readonly LocalService _settings;
        private readonly ListDownloadStatusEventHandler _onStatusChanged;

        public DownloadLists(LocalService settings, ListDownloadStatusEventHandler onStatusChanged)
        {
            _settings = settings;
            _onStatusChanged = onStatusChanged;
            if (_settings.FillDownloadLists(this) && Entries.Count > 0) StartDownload(); else Entries.Clear();
        }

        public Feed Add(IEnumerable entries, MediaType mediaType)
        {
            var downloadList = SoftAdd(entries, mediaType);
            downloadList.Download(false);
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
            foreach (var downloadItems in Entries.Cast<DownloadList>().Where(downloadItems => downloadItems.Entries.Count > 0)) downloadItems.Download(false);
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
            _settings.SaveDownloadLists(this);
        }

        private void UpdateStatus(Feed downloadList, Feed entry, DownloadState state, double percentage)
        {
            DownloadState = state;
            Percentage = percentage;
            if (_onStatusChanged != null) _onStatusChanged(downloadList, entry, DownloadState, Percentage);
        }

        public new string Title { get { return "TOTAL"; } }

    }
}
