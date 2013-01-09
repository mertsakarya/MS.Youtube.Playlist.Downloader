using System;
using ms.video.downloader.service.Dowload;

namespace ms.video.downloader.service
{
    public class ApplicationConfiguration
    {
        public Guid Guid { get; set; }
        public string[] Downloads { get; set; }
    }
}