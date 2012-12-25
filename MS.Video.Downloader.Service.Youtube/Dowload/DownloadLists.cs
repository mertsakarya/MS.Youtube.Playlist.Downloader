using System.Collections;
using System.Linq;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public class DownloadLists : Feed
    {
        public ListDownloadStatusEventHandler OnStatusChanged;

        public DownloadList Add(IEnumerable entries, MediaType mediaType, bool ignoreDownloaded)
        {
            var downloadItems = new DownloadList(mediaType, OnDownloadStatusChange);
            foreach (YoutubeEntry member in entries)
                if (member.Uri != null)
                    downloadItems.Entries.Add(member.Clone());
            downloadItems.Download(ignoreDownloaded);
            Entries.Add(downloadItems);
            return downloadItems;
        }

        private void OnDownloadStatusChange(DownloadList downloadList, IFeed entry, DownloadState downloadState, double percentage)
        {
            if (Entries.Count <= 0 || OnStatusChanged == null) return;
            var finishedCount = Entries.Count(p => p.DownloadState == DownloadState.AllFinished);
            if (downloadState == DownloadState.DownloadProgressChanged)
                UpdateStatus(downloadList, entry, DownloadState.DownloadProgressChanged, Entries.Average(en => en.Percentage));
            else if (finishedCount == Entries.Count) 
                UpdateStatus(downloadList, entry, DownloadState.AllFinished, 100.0);
            else if (Entries.Count == 1 && downloadState == DownloadState.AllStart)
                UpdateStatus(downloadList, entry, DownloadState.AllStart, 0.0);
            else if(OnStatusChanged != null && 
                    !(downloadState == DownloadState.AllFinished || 
                        downloadState == DownloadState.AllStart || 
                        downloadState == DownloadState.DownloadProgressChanged
                    )) 
                OnStatusChanged(downloadList, entry, downloadState, percentage);
        }

        private void UpdateStatus(DownloadList downloadList, IFeed entry, DownloadState state, double percentage)
        {
            DownloadState = state;
            Percentage = percentage;
            if (OnStatusChanged != null) OnStatusChanged(downloadList, entry, DownloadState, Percentage);
        }

        public new string Title { get { return "TOTAL"; } }
    }
}
