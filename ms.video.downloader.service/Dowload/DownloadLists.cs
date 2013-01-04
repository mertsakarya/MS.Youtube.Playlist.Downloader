using System.Collections;

namespace ms.video.downloader.service.Dowload
{
    public class DownloadLists : Feed
    {
        private readonly ListDownloadStatusEventHandler _onStatusChanged;

        public DownloadLists(ListDownloadStatusEventHandler onStatusChanged)
        {
            _onStatusChanged = onStatusChanged;
        }

        public Feed Add(IEnumerable entries, MediaType mediaType, bool ignoreDownloaded)
        {
            var downloadItems = new DownloadList(mediaType, OnDownloadStatusChange);
            foreach (YoutubeEntry member in entries)
                if (member.Uri != null)
                    downloadItems.Entries.Add(member.Clone());
            downloadItems.Download(ignoreDownloaded);
            Entries.Add(downloadItems);
            return downloadItems;
        }

        private void OnDownloadStatusChange(Feed downloadList, Feed entry, DownloadState downloadState, double percentage)
        {
            if (Entries.Count <= 0 || _onStatusChanged == null) return;
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
