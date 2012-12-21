using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.Models;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public class DownloadLists : Dictionary<Guid, DownloadItems>
    {
        public DownloadStatusEventHandler OnStatusChanged;

        public DownloadItems Add(IEnumerable entries, MediaType mediaType, bool ignoreDownloaded)
        {
            var downloadItems = new DownloadItems(mediaType, OnDownloadStatusChange);
            foreach (Entry member in entries)
                if (member.Uri != null)
                    downloadItems.Add(member.Clone());
            downloadItems.Download(ignoreDownloaded);
            return downloadItems;
        }

        private void OnDownloadStatusChange(DownloadItems downloadItems, Entry entry, DownloadStatus status)
        {
            if (OnStatusChanged != null) OnStatusChanged(downloadItems, entry, status);
        }

    }
}
