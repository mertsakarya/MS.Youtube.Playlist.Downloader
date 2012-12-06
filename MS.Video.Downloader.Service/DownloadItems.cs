using System.Collections.ObjectModel;
using System.Linq;

namespace MS.Video.Downloader.Service
{
    public delegate void DownloadStatusEventHandler(DownloadItem item, DownloadStatus status);

    public class DownloadItems : ObservableCollection<DownloadItem>
    {
        private bool _ignoreDownloaded;
        private readonly int _poolSize;

        public DownloadStatusEventHandler OnDownloadStatusChange;

        public DownloadItems(int poolSize = 3)
        {
            _ignoreDownloaded = false;
            _poolSize = poolSize;
        }

        public void Download(bool ignoreDownloaded)
        {
            _ignoreDownloaded = ignoreDownloaded;
            foreach (var item in this) {
                item.OnDownloadStatusChange += OnDownloadStatusChanged;
            }
            DownloadFirst();
        }

        private void OnDownloadStatusChanged(DownloadItem item, DownloadStatus status)
        {
            if (OnDownloadStatusChange == null) return;
            var finishedCount = this.Count(p => (p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error));
            double sumPercentage = ((double)finishedCount/Count)*100;
            var downloadCount = this.Count(p => !(p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error || p.Status.DownloadState == DownloadState.Initialized));
            if(status.DownloadState == DownloadState.Ready || status.DownloadState == DownloadState.DownloadFinish)
                OnDownloadStatusChange(item, new DownloadStatus { DownloadState = DownloadState.DownloadProgressChanged, Percentage = sumPercentage });
            if (downloadCount == 0 && finishedCount == Count)
                OnDownloadStatusChange(null, new DownloadStatus { DownloadState = DownloadState.AllFinished, Percentage = sumPercentage });
            else 
                OnDownloadStatusChange(item, new DownloadStatus {DownloadState = status.DownloadState, Percentage = sumPercentage});
            if (downloadCount != _poolSize) 
                DownloadFirst();
        }

        private void DownloadFirst()
        {
            var first = this.FirstOrDefault(item => item.Status.DownloadState == DownloadState.Initialized);
            if(first != null)
                first.DownloadAsync(_ignoreDownloaded);
        }
    }
}