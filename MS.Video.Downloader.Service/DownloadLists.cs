using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Models;

namespace MS.Video.Downloader.Service
{
    public class DownloadLists : Dictionary<Guid, DownloadItems>
    {
        public DownloadItems Add(Entry entry, MediaType mediaType, bool ignoreDownloaded) { }
        public DownloadItems Add(IList<Entry> entries, MediaType mediaType, bool ignoreDownloaded) { }
    }
}
