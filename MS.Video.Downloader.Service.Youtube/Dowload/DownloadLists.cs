using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public class DownloadLists : ObservableCollection<DownloadList>
    {
        public ListDownloadStatusEventHandler OnStatusChanged;
        public DownloadStatus Status { get; set; }

        public DownloadLists()
        {
            Status = new DownloadStatus { DownloadState = DownloadState.Initialized, Percentage = 0.0 };
        }

        public DownloadList Add(IEnumerable entries, MediaType mediaType, bool ignoreDownloaded)
        {
            var downloadItems = new DownloadList(mediaType, OnDownloadStatusChange);
            foreach (YoutubeEntry member in entries)
                if (member.Uri != null)
                    downloadItems.Add(member.Clone());
            downloadItems.Download(ignoreDownloaded);
            Add(downloadItems);
            return downloadItems;
        }

        private void OnDownloadStatusChange(DownloadList downloadList, YoutubeEntry entry, DownloadStatus status)
        {
            if (Count <= 0 || OnStatusChanged == null) return;
            var finishedCount = this.Count(p => p.Status.DownloadState == DownloadState.AllFinished);
            if (status.DownloadState == DownloadState.DownloadProgressChanged) 
                UpdateStatus(downloadList, entry, DownloadState.DownloadProgressChanged, this.Average(en => en.Status.Percentage));
            else if (finishedCount == Count) 
                UpdateStatus(downloadList, entry, DownloadState.AllFinished, 100.0);
            else if (Count == 1 && status.DownloadState == DownloadState.AllStart)
                UpdateStatus(downloadList, entry, DownloadState.AllStart, 0.0);
            else if(OnStatusChanged != null && 
                    !(status.DownloadState == DownloadState.AllFinished || 
                        status.DownloadState == DownloadState.AllStart || 
                        status.DownloadState == DownloadState.DownloadProgressChanged
                    )) 
                OnStatusChanged(downloadList, entry, status);
        }

        private void UpdateStatus(DownloadList downloadList, YoutubeEntry entry, DownloadState state, double percentage)
        {
            Status.DownloadState = state;
            Status.Percentage = percentage;
            if (OnStatusChanged != null) OnStatusChanged(downloadList, entry, Status);
        }


    }
}
