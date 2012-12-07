using System;
using System.Xml.Serialization;

namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "oembed")]
    public class OEmbed
    {
        public string type { get; set; }
        public string version { get; set; }
        public string provider_name { get; set; }
        public string provider_url { get; set; }
        public string title { get; set; }
        public string author_name { get; set; }
        public string author_url { get; set; }
        public string is_plus { get; set; }
        public string html { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string duration { get; set; }
        public string thumbnail_url { get; set; }
        public string thumbnail_width { get; set; }
        public string thumbnail_height { get; set; }
        public string video_id { get; set; }
    }
}