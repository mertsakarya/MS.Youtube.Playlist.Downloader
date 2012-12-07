using System;
using System.Xml.Serialization;

namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "rsp")]
    public class VimeoErrorResponse
    {
        [XmlElement("err", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public VimeoErrorWrapper error { get; set; }
        [XmlAttribute()]
        public string generated_in { get; set; }
        [XmlAttribute()]
        public string stat { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public class VimeoErrorWrapper
    {
        [XmlAttribute()]
        public string code { get; set; }
        [XmlAttribute()]
        public string msg { get; set; }
    }
}
