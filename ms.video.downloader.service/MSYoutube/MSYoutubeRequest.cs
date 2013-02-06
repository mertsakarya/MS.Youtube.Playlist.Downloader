using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using ms.video.downloader.service.Download;

namespace ms.video.downloader.service.MSYoutube
{
    public delegate void MSYoutubeLoading(long count, long total);

    public class MSYoutubeRequest
    {
        private readonly MSYoutubeSettings _settings;

        public MSYoutubeRequest(MSYoutubeSettings settings)
        {
            _settings = settings;
        }

        public MSYoutubeEntry GetAsync(YoutubeUrl youtubeUrl, Uri uri, MSYoutubeLoading loading)
        {
            var feed = new MSYoutubeEntry {
                YoutubeUrl = youtubeUrl,
                NextPageUri =
                    new Uri(uri + ((String.IsNullOrEmpty(uri.Query)) ? "?" : "&") + "start-index=1&max-results=40")
            };
            _GetAsync(uri, feed, loading);
            return feed;
        }

        public void _GetAsync(Uri uri, MSYoutubeEntry feed, MSYoutubeLoading loading)
        {
            
            if (feed.NextPageUri == null) return;
            var document = GetXmlDocumentAsync(feed.NextPageUri);
            FillFeed(document, feed);
            if (loading != null) loading(feed.Entries.Count, feed.Total);
            _GetAsync(uri, feed, loading);
        }

        private static void FillFeed(XmlDocument xml, MSYoutubeEntry feed)
        {
            feed.Title = GetNodeValue(xml.DocumentElement, "root:title");
            feed.Author = GetNodeValue(xml.DocumentElement, "root:author/root:name");
            feed.AuthorId = GetNodeValue(xml.DocumentElement, "root:author/yt:userId");
            var nextPageUri = GetNodeValue(xml.DocumentElement, "root:link[@rel='next']/@href");
            feed.NextPageUri = (String.IsNullOrEmpty(nextPageUri)) ? null : new Uri(nextPageUri);
            string stotal = GetNodeValue(xml.DocumentElement, "openSearch:totalResults");
            int total;
            feed.Total = (int.TryParse(stotal, out total)) ? total : 0;
            var nodes = GetNodes(xml.DocumentElement, "root:entry");
            foreach (XmlElement node in nodes) {
                var entry = new MSYoutubeEntry {
                    Title = GetNodeValue(node, "root:title"),
                    Description = GetNodeValue(node, "media:group/media:description")
                };
                string tmp; // = GetNodeValue(node, "yt:position");
                foreach (XmlElement thumbNode in GetNodes(node, "media:group/media:thumbnail")) {
                    var thumbnail = new MSYoutubeThumbnail();
                    thumbnail.Url = GetNodeValue(thumbNode, "@url");
                    thumbnail.Height = GetNodeValue(thumbNode, "@height");
                    thumbnail.Width = GetNodeValue(thumbNode, "@width");
                    entry.Thumbnails.Add(thumbnail);
                }
                tmp = GetNodeValue(node, "root:link[@rel='alternate']/@href");
                Uri uri;
                if (!Uri.TryCreate(tmp, UriKind.Absolute, out uri)) continue;
                entry.Uri = uri;
                feed.Entries.Add(entry);
            }
        }

        private static string GetNodeValue(XmlElement xml, string xpath)
        {
            var ns = GetXmlNamespaceManager(xml);
            var node = xml.SelectSingleNode(xpath, ns);
            return node == null ? "" : node.InnerText;
        }

        private static XmlNamespaceManager GetXmlNamespaceManager(XmlNode xml)
        {
            if (xml.OwnerDocument == null) return null;
            var ns = new XmlNamespaceManager(xml.OwnerDocument.NameTable);
            ns.AddNamespace("root", "http://www.w3.org/2005/Atom");
            ns.AddNamespace("media", "http://search.yahoo.com/mrss/");
            ns.AddNamespace("openSearch", "http://a9.com/-/spec/opensearchrss/1.0/");
            //"http://a9.com/-/spec/opensearch/1.1/"}, //http://a9.com/-/spec/opensearchrss/1.0/
            ns.AddNamespace("gd", "http://schemas.google.com/g/2005");
            ns.AddNamespace("yt", "http://gdata.youtube.com/schemas/2007");
            return ns;
        }

        private static XmlNodeList GetNodes(XmlElement xml, string xpath)
        {
            var ns = GetXmlNamespaceManager(xml);
            return xml.SelectNodes(xpath, ns);
        }

        public XmlDocument GetXmlDocumentAsync(Uri uri)
        {
            var xml = new XmlDocument();
            var req = WebRequest.Create(uri);
            req.Headers["GData-Key"] = _settings.DeveloperKey;
            using (var resp = req.GetResponse()) {
                using (var stream = resp.GetResponseStream()) {
                    using (var destinationStream = new MemoryStream()) {
                        if (stream != null) {
                            stream.CopyTo(destinationStream);
                        }
                        var bytes = destinationStream.ToArray();
                        var xmlData = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        xml.LoadXml(xmlData);
                    }
                }
            }
            return xml;
        }
    }
}