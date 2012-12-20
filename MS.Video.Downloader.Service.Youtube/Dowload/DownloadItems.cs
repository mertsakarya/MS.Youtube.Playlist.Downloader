using System;
using System.Collections.ObjectModel;
using System.Linq;
using MS.Video.Downloader.Service.Youtube.Models;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public delegate void DownloadStatusEventHandler(DownloadItems downloadItems, Entry entry, DownloadStatus status);

    public class DownloadItems : ObservableCollection<Entry>
    {
        public MediaType MediaType { get; set; }
        private bool _ignoreDownloaded;
        private readonly int _poolSize;

        public Guid Guid { get; private set; }

        public DownloadStatusEventHandler OnDownloadStatusChange;

        public DownloadItems(MediaType mediaType, DownloadStatusEventHandler onDownloadStatusChange = null, int poolSize = 3)
        {
            MediaType = mediaType;
            OnDownloadStatusChange = onDownloadStatusChange;
            _ignoreDownloaded = false;
            _poolSize = poolSize;
            Guid = Guid.NewGuid();
        }

        public void Download(bool ignoreDownloaded)
        {
            if (OnDownloadStatusChange != null)
                OnDownloadStatusChange(this, null, new DownloadStatus {Percentage = 0.0, DownloadState = DownloadState.AllStart});
            _ignoreDownloaded = ignoreDownloaded;
            foreach (var item in this) {
                item.OnDownloadStatusChange += OnDownloadStatusChanged;
            }
            DownloadFirst();
        }

        private void OnDownloadStatusChanged(DownloadItems downloadItems, Entry item, DownloadStatus status)
        {
            if (OnDownloadStatusChange == null) return;
            if (status.DownloadState == DownloadState.TitleChanged) {
                OnDownloadStatusChange(this, item, status);
                return;
            }
            var finishedCount = this.Count(p => (p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error));
            var sumPercentage = ((double)finishedCount/Count)*100;
            var downloadCount = this.Count(p => !(p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error || p.Status.DownloadState == DownloadState.Initialized));
            if (status.DownloadState == DownloadState.Ready || status.DownloadState == DownloadState.DownloadFinish ||
                status.DownloadState == DownloadState.DownloadProgressChanged) {
                OnDownloadStatusChange(this, item,
                                       new DownloadStatus {
                                           DownloadState = DownloadState.DownloadProgressChanged,
                                           Percentage =
                                               sumPercentage +
                                               ((status.DownloadState == DownloadState.DownloadProgressChanged)
                                                    ? ((status.Percentage/Count) * 100)
                                                    : 0)
                                       });
            }
            if (downloadCount == 0 && finishedCount == Count)
                OnDownloadStatusChange(this, null, new DownloadStatus { DownloadState = DownloadState.AllFinished, Percentage = sumPercentage });
            else 
                OnDownloadStatusChange(this, item, new DownloadStatus {DownloadState = status.DownloadState, Percentage = sumPercentage});
            if (downloadCount != _poolSize) 
                DownloadFirst();
        }

        private async void DownloadFirst()
        {
            var first = this.FirstOrDefault(item => item.Status.DownloadState == DownloadState.Initialized);
            if (first == null) return;
            OnDownloadStatusChange(this, first, new DownloadStatus {Percentage = 0.0, DownloadState = DownloadState.DownloadStart});
            await first.DownloadAsync(MediaType, _ignoreDownloaded);
        }

    }
}