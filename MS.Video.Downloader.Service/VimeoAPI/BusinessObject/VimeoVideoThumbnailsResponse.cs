using System;
using System.Xml.Serialization;

namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "rsp")]
    public class VimeoVideoThumbnailsResponse
    {
        [XmlElement("thumbnails", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public VimeoVideoThumbnailsWrapper thumbnails { get; set; }
        [XmlAttribute()]
        public string generated_in { get; set; }
        [XmlAttribute()]
        public string stat { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public class VimeoVideoThumbnailsWrapper
    {
        [XmlElement("thumbnail", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public VimeoVideoThumbnailWrapper[] thumbnail { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public class VimeoVideoThumbnailWrapper
    {
        [XmlAttribute()]
        public string height { get; set; }
        [XmlAttribute()]
        public string width { get; set; }
        [XmlText()]
        public string thumbnail { get; set; }
    }
}
