using System;
using System.Xml.Serialization;

namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "rsp")]
    public partial class VimeoSearchResponse
    {
        [XmlElement("videos", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public SearchResponseVideosWrapper videos { get; set; }
        [XmlAttribute()]
        public string generated_in { get; set; }
        [XmlAttribute()]
        public string stat { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public partial class SearchResponseVideosWrapper
    {
        [XmlElement("video", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public SearchResponseVideosWrapperVideo[] video { get; set; }
        [XmlAttribute()]
        public string on_this_page { get; set; }
        [XmlAttribute()]
        public string page { get; set; }
        [XmlAttribute()]
        public string perpage { get; set; }
        [XmlAttribute()]
        public string total { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public partial class SearchResponseVideosWrapperVideo
    {
        [XmlAttribute()]
        public string embed_privacy { get; set; }
        [XmlAttribute()]
        public string id { get; set; }
        [XmlAttribute()]
        public string is_hd { get; set; }
        [XmlAttribute()]
        public string owner { get; set; }
        [XmlAttribute()]
        public string privacy { get; set; }
        [XmlAttribute()]
        public string title { get; set; }
        [XmlAttribute()]
        public string upload_date { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class VimeoSearchResponses
    {
        [XmlElement("rsp")]
        public VimeoSearchResponse[] items { get; set; }
    }
}
