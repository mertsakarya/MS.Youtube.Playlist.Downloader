using System.Linq;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public delegate void ListDownloadStatusEventHandler(Feed list, Feed feed, DownloadState downloadState, double percentage);

    public class DownloadList : Feed
    {
        public MediaType MediaType { get; set; }
        private bool _ignoreDownloaded;
        private readonly int _poolSize;

        public ListDownloadStatusEventHandler OnListDownloadStatusChange;

        public DownloadList(MediaType mediaType, ListDownloadStatusEventHandler onDownloadStatusChange = null, int poolSize = 3)
        {
            MediaType = mediaType;
            OnListDownloadStatusChange = onDownloadStatusChange;
            _ignoreDownloaded = false;
            _poolSize = poolSize;
        }

        public void Download(bool ignoreDownloaded)
        {
            var count = Entries.Count;
            if (count == 0) return;
            var firstEntry = Entries[0] as YoutubeEntry;
            if (firstEntry != null) 
                if (count == 1) 
                    Title = firstEntry.Title;
                else {
                    Title = firstEntry.ChannelName;
                    if (string.IsNullOrEmpty(Title)) Title = firstEntry.Title;
                }

            UpdateStatus(DownloadState.AllStart, null, 0.0);
            _ignoreDownloaded = ignoreDownloaded;
            foreach (YoutubeEntry item in Entries) item.OnEntryDownloadStatusChange += OnDownloadStatusChanged;
            DownloadFirst();

        }

        private void OnDownloadStatusChanged(Feed feed, DownloadState downloadState, double percentage)
        {
            var downloadCount = Entries.Count(p => !(p.DownloadState == DownloadState.Ready || p.DownloadState == DownloadState.Error || p.DownloadState == DownloadState.Initialized));
            if (OnListDownloadStatusChange != null) {
                DownloadState = downloadState;
                var finishedCount = Entries.Count(p => (p.DownloadState == DownloadState.Ready || p.DownloadState == DownloadState.Error));
                if (downloadState == DownloadState.DownloadProgressChanged) {
                    var avg = Entries.Average(entry => entry.Percentage);
                    Percentage = avg; 
                }
                if (downloadCount == 0 && finishedCount == Entries.Count) 
                    DownloadState = DownloadState.AllFinished;
                if (Entries.Count == 1 && downloadState == DownloadState.TitleChanged) {
                    Title = Entries[0].Title;
                }
                OnListDownloadStatusChange(this, feed, DownloadState, Percentage);
            }
            if (downloadCount != _poolSize)  
                DownloadFirst();
        }

        private void UpdateStatus(DownloadState state, YoutubeEntry entry, double percentage)
        {
            DownloadState = state;
            Percentage = percentage;
            if (OnListDownloadStatusChange != null) OnListDownloadStatusChange(this, entry, DownloadState, Percentage);
        }

        private async void DownloadFirst()
        {
            var first = Entries.FirstOrDefault(item => item.DownloadState == DownloadState.Initialized);
            var entry = first as YoutubeEntry;
            if (entry == null) return;
            await entry.DownloadAsync(MediaType, _ignoreDownloaded);
        }
    }
}