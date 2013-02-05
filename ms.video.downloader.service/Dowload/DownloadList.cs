using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ms.video.downloader.service.Dowload
{
    public delegate void ListDownloadStatusEventHandler(Feed list, Feed feed, DownloadState downloadState, double percentage);

    public class DownloadList : Feed
    {
        public MediaType MediaType { get; set; }
        private bool _ignoreDownloaded;
        private readonly int _poolSize;

        [JsonIgnore] public ListDownloadStatusEventHandler OnListDownloadStatusChange;

        public DownloadList(MediaType mediaType, ListDownloadStatusEventHandler onDownloadStatusChange = null,
                            int poolSize = 3)
        {
            MediaType = mediaType;
            OnListDownloadStatusChange = onDownloadStatusChange;
            _ignoreDownloaded = false;
            _poolSize = poolSize;
        }

        public void Download(bool ignoreDownloaded)
        {
            if (ExecutionStatus == ExecutionStatus.Deleted) { Delete(); return; }
            var count = Entries.Count;
            if (count == 0) return;
            var firstEntry = Entries[0] as YoutubeEntry;
            if (firstEntry != null) {
                if (count == 1) { Title = firstEntry.Title; }
                else { Title = firstEntry.ChannelName; if (string.IsNullOrEmpty(Title)) Title = firstEntry.Title; }
            }
            UpdateStatus(DownloadState.AllStart, null, 0.0);
            _ignoreDownloaded = ignoreDownloaded;


            var cache = CacheManager.Instance;
            if (cache.UrlCaches.ContainsKey(YoutubeUrl.VideoId)) {
                var storageFile = DownloadHelper.GetFile(VideoFolder, videoFile);

                var fn = storageFile.ToString();
                var urlCache = cache.UrlCaches[YoutubeUrl.VideoId];
                if (storageFile.Length >= urlCache.Length) {
                    if (!cache.VideoCaches.ContainsKey(fn)) {
                        var videoCache = new VideoCache() { FileName = fn, VideoId = urlCache.VideoId, AudioFinished = false, Finished = false };
                        cache.Set(videoCache);
                    }
                    if (cache.VideoCaches[fn].Finished)
                        return;
                }
                var first = cache.VideoCaches.FirstOrDefault(p => p.Value.FileName != storageFile.ToString() && p.Value.Finished && File.Exists(p.Value.FileName));
                if (first.Value != null) {
                    File.Copy(first.Value.FileName, fn, true);
                    cache.Set(new VideoCache() { Finished = true, FileName = fn, VideoId = urlCache.VideoId, AudioFinished = false });
                    return;
                }
            }


            foreach (YoutubeEntry item in Entries) item.OnEntryDownloadStatusChange += OnDownloadStatusChanged;
            DownloadFirst();

        }

        private void OnDownloadStatusChanged(Feed feed, DownloadState downloadState, double percentage)
        {
            var finishedCount = 0;
            var downloadCount = 0;
            var average = 0.0;
            var entry = feed as YoutubeEntry;
            if (downloadState == DownloadState.Deleted) {
                if (entry != null) { entry.OnEntryDownloadStatusChange = null; Entries.Remove(entry); }
                return;
            }
            foreach (var en in Entries) {
                if (en.DownloadState == DownloadState.Ready || en.DownloadState == DownloadState.Error) finishedCount++;
                if (!(en.DownloadState == DownloadState.Ready || en.DownloadState == DownloadState.Error || en.DownloadState == DownloadState.Initialized)) downloadCount++;
                average += en.Percentage;
            }
            average = average/Entries.Count;

            if (OnListDownloadStatusChange != null) {
                DownloadState = downloadState;
                if (downloadState == DownloadState.DownloadProgressChanged) {
                    Percentage = average;
                }
                if (downloadCount == 0 && finishedCount == Entries.Count)
                    DownloadState = DownloadState.AllFinished;
                if (Entries.Count == 1 && downloadState == DownloadState.TitleChanged) {
                    Title = Entries[0].Title;
                }
                OnListDownloadStatusChange(this, feed, DownloadState, Percentage);
            }
            if (downloadCount <= _poolSize)
                DownloadFirst();
        }

        private void UpdateStatus(DownloadState state, YoutubeEntry entry, double percentage)
        {
            DownloadState = state;
            Percentage = percentage;
            if (OnListDownloadStatusChange != null) OnListDownloadStatusChange(this, entry, DownloadState, Percentage);
        }

        private void DownloadFirst()
        {
            for (var i = 0; i < Entries.Count; i++) {
                var entry = Entries[i] as YoutubeEntry;
                if (entry == null || entry.DownloadState != DownloadState.Initialized || entry.DownloadState == DownloadState.Deleted) continue;
                entry.UpdateStatus(DownloadState.DownloadStart, 0.0);
                Task.Factory.StartNew(() => entry.DownloadAsync(MediaType, _ignoreDownloaded));
                break;
            }
        }

        public override void Delete()
        {
            base.Delete();
            foreach (YoutubeEntry youtubeEntry in Entries) 
                youtubeEntry.OnEntryDownloadStatusChange = null;
            Entries.Clear();
            UpdateStatus(DownloadState.Deleted, null, 0.0);
        }
    }
}