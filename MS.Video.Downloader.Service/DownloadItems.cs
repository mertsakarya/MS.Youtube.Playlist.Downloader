using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Models;

namespace MS.Video.Downloader.Service
{
    public delegate void DownloadStatusEventHandler(DownloadItems downloadItems, Entry entry, DownloadStatus status);

    public class DownloadItems : ObservableCollection<Entry>
    {
        public MediaType MediaType { get; set; }
        public string BaseFolder { get; set; }
        private bool _ignoreDownloaded;
        private readonly int _poolSize;

        public Guid Guid { get; private set; }

        public DownloadStatusEventHandler OnDownloadStatusChange;

        public DownloadItems(MediaType mediaType, string baseFolder, DownloadStatusEventHandler onDownloadStatusChange = null, int poolSize = 3)
        {
            MediaType = mediaType;
            BaseFolder = baseFolder;
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
            var finishedCount = this.Count(p => (p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error));
            double sumPercentage = ((double)finishedCount/Count)*100;
            var downloadCount = this.Count(p => !(p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error || p.Status.DownloadState == DownloadState.Initialized));
            if(status.DownloadState == DownloadState.Ready || status.DownloadState == DownloadState.DownloadFinish)
                OnDownloadStatusChange(this, item, new DownloadStatus { DownloadState = DownloadState.DownloadProgressChanged, Percentage = sumPercentage });
            if (downloadCount == 0 && finishedCount == Count)
                OnDownloadStatusChange(this, null, new DownloadStatus { DownloadState = DownloadState.AllFinished, Percentage = sumPercentage });
            else 
                OnDownloadStatusChange(this, item, new DownloadStatus {DownloadState = status.DownloadState, Percentage = sumPercentage});
            if (downloadCount != _poolSize) 
                DownloadFirst();
        }

        private async void DownloadFirst()
        {
            await Task.Factory.StartNew(() => {
                var first = this.FirstOrDefault(item => item.Status.DownloadState == DownloadState.Initialized);
                if (first != null) {
                    OnDownloadStatusChange(this, first, new DownloadStatus {Percentage = 0.0, DownloadState = DownloadState.DownloadStart});
                    first.DownloadAsync(MediaType, BaseFolder, _ignoreDownloaded);
                }
            });
        }
    }
}