using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public delegate void ListDownloadStatusEventHandler(DownloadList list, YoutubeEntry entry, DownloadStatus status);

    public class DownloadList : ObservableCollection<YoutubeEntry>
    {
        public MediaType MediaType { get; set; }
        private bool _ignoreDownloaded;
        private readonly int _poolSize;
        public DownloadStatus Status { get; set; }
        public Guid Guid { get; private set; }

        public ListDownloadStatusEventHandler OnListDownloadStatusChange;

        public DownloadList(MediaType mediaType, ListDownloadStatusEventHandler onDownloadStatusChange = null, int poolSize = 3)
        {
            MediaType = mediaType;
            OnListDownloadStatusChange = onDownloadStatusChange;
            _ignoreDownloaded = false;
            _poolSize = poolSize;
            Guid = Guid.NewGuid();
            Status = new DownloadStatus {DownloadState = DownloadState.Initialized, Percentage = 0.0};
        }

        public void Download(bool ignoreDownloaded)
        {
            UpdateStatus(DownloadState.AllStart, null, 0.0);
            _ignoreDownloaded = ignoreDownloaded;
            foreach (var item in this) item.OnEntryDownloadStatusChange += OnDownloadStatusChanged;
            DownloadFirst();
        }

        private void OnDownloadStatusChanged(YoutubeEntry item, DownloadStatus status)
        {
            var downloadCount = this.Count(p => !(p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error || p.Status.DownloadState == DownloadState.Initialized));
            if (OnListDownloadStatusChange != null) {
                Status.DownloadState = status.DownloadState;
                var finishedCount = this.Count( p => (p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error));
                if (status.DownloadState == DownloadState.DownloadProgressChanged) {
                    var avg = this.Average(entry => entry.Status.Percentage);
                    Status.Percentage = avg; 
                }
                if(downloadCount == 0 && finishedCount == Count) 
                    Status.DownloadState = DownloadState.AllFinished;
                OnListDownloadStatusChange(this, item, Status);
            }
            if (downloadCount != _poolSize)  
                DownloadFirst();
        }

        private void UpdateStatus(DownloadState state, YoutubeEntry entry, double percentage)
        {
            Status.DownloadState = state;
            Status.Percentage = percentage;
            if (OnListDownloadStatusChange != null) OnListDownloadStatusChange(this, entry, Status);
        }

        private async void DownloadFirst()
        {
            var first = this.FirstOrDefault(item => item.Status.DownloadState == DownloadState.Initialized);
            if (first == null) return;
            await first.DownloadAsync(MediaType, _ignoreDownloaded);
        }

    }
}