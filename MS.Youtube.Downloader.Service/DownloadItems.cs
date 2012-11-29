using System.Collections.ObjectModel;
using System.Linq;

namespace MS.Youtube.Downloader.Service
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
                item.OnDownloadStateChange += OnDownloadStateChange;
            }
            DownloadFirst();
        }

        private void OnDownloadStateChange(DownloadItem item, DownloadState state, double progressPercentage)
        {
            if (OnDownloadStatusChange == null) return;
            var finishedCount = this.Count(p => (p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error));
            var sumPercentage = this.Average(p => p.Status.Percentage);
            var downloadCount = this.Count(p => !(p.Status.DownloadState == DownloadState.Ready || p.Status.DownloadState == DownloadState.Error || p.Status.DownloadState == DownloadState.Initialized));
            if (downloadCount == 0 && finishedCount == Count)
                OnDownloadStatusChange(null, new DownloadStatus { DownloadState = DownloadState.AllFinished, Percentage = sumPercentage });
            else 
                OnDownloadStatusChange(item, new DownloadStatus {DownloadState = state, Percentage = sumPercentage});
            if (downloadCount != _poolSize) DownloadFirst();
        }

        private void DownloadFirst()
        {
            var first = this.FirstOrDefault(item => item.Status.DownloadState == DownloadState.Initialized);
            if(first != null)
                first.Download(_ignoreDownloaded);
        }
    }
}