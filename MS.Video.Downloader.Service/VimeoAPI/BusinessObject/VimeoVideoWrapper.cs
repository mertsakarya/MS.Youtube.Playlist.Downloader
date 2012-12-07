using System;
using System.Xml.Serialization;

namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject
{
    [Serializable]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "videos")]
    public class VimeoVideoWrapper
    {
        [XmlElement("video")]
        public VimeoVideo[] Videos { get; set;}
    }

    [Serializable]
    public class VimeoVideo
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public string upload_date { get; set; }
        public string thumbnail_small { get; set; }
        public string thumbnail_medium { get; set; }
        public string thumbnail_large { get; set; }
        public string user_name { get; set; }
        public string user_url { get; set; }
        public string user_portrait_small { get; set; }
        public string user_portrait_medium { get; set; }
        public string user_portrait_large { get; set; }
        public string user_portrait_huge { get; set; }
        public string stats_number_of_likes { get; set; }
        public string stats_number_of_plays { get; set; }
        public string stats_number_of_comments { get; set; }
        public string duration { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string tags { get; set; }
    }
}