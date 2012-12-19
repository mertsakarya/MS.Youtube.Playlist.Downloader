using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Collections;

namespace MS.Video.Downloader.Service.Youtube.Models
{
    public class MSYoutubeSettings
    {
        public string ApplicationName { get; set; }
        public string DeveloperKey { get; set; }
        public int PageSize { get; set; }
        public bool AutoPaging { get; set; }

        public MSYoutubeSettings(string applicationName, string developerKey)
        {
            ApplicationName = applicationName;
            DeveloperKey = developerKey;
            AutoPaging = false;
        }
    }

    public delegate void MSYoutubeLoading(int count, int total);

    public class MSYoutubeRequest
    {
        private readonly MSYoutubeSettings _settings;

        public MSYoutubeRequest(MSYoutubeSettings settings)
        {
            _settings = settings;
        }

        public async Task<MSYoutubeEntry> GetAsync(YoutubeUrl youtubeUrl, Uri uri, MSYoutubeLoading loading = null)
        {
            var feed = new MSYoutubeEntry {YoutubeUrl = youtubeUrl, NextPageUri = new Uri(uri + ((String.IsNullOrEmpty(uri.Query)) ? "?" : "&") + "start-index=1&max-results=40")};
            await _GetAsync(uri, feed, loading);
            return feed;
        }

        public async Task _GetAsync(Uri uri, MSYoutubeEntry feed, MSYoutubeLoading loading = null)
        {
            if (feed.NextPageUri == null) return;
            var xml = await GetXmlDocumentAsync(feed.NextPageUri);
            FillFeed(xml, feed);
            if (loading != null) loading(feed.Entries.Count, feed.Total);
            await _GetAsync(uri, feed, loading);
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
                var entry = new MSYoutubeEntry();
                entry.Title = GetNodeValue(node, "root:title");
                entry.Description = GetNodeValue(node, "media:group/media:description");
                var tmp = GetNodeValue(node, "yt:position");
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
            var ns = new PropertySet {
                {"root", "http://www.w3.org/2005/Atom"},
                {"media", "http://search.yahoo.com/mrss/"},
                {"openSearch", "http://a9.com/-/spec/opensearchrss/1.0/"}, //"http://a9.com/-/spec/opensearch/1.1/"}, //http://a9.com/-/spec/opensearchrss/1.0/
                {"gd", "http://schemas.google.com/g/2005"},
                {"yt", "http://gdata.youtube.com/schemas/2007"}
            };

            var node = xml.SelectSingleNodeNS(xpath, ns);
            return node == null ? "" : node.InnerText;
        }

        private static XmlNodeList GetNodes(XmlElement xml, string xpath)
        {
            var ns = new PropertySet {
                {"root", "http://www.w3.org/2005/Atom"},
                {"media", "http://search.yahoo.com/mrss/"},
                {"openSearch", "http://a9.com/-/spec/opensearchrss/1.0/"}, //"http://a9.com/-/spec/opensearch/1.1/"}, //http://a9.com/-/spec/opensearchrss/1.0/
                {"gd", "http://schemas.google.com/g/2005"},
                {"yt", "http://gdata.youtube.com/schemas/2007"}
            };

            return xml.SelectNodesNS(xpath, ns);
        }

        public async Task<XmlDocument> GetXmlDocumentAsync(Uri uri)
        {
            var xml = new XmlDocument();
            var req = WebRequest.Create(uri);
            req.Headers["GData-Key"] = _settings.DeveloperKey;
            using (var resp = await req.GetResponseAsync()) {
                using (var stream = resp.GetResponseStream()) {
                    using (var destinationStream = new MemoryStream()) {
                        if (stream != null) {
                            await stream.CopyToAsync(destinationStream);
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

    public class MSYoutubeEntry
    {
        public Uri NextPageUri { get; set; }

        public IList<MSYoutubeEntry> Entries { get; set; }

        public string Description { get; set; }
        public string Title { get; set; }
        public Uri Uri { get; set; }
        public string Content { get; set; }
        public IList<MSYoutubeThumbnail> Thumbnails { get; set; }

        public YoutubeUrl YoutubeUrl { get; set; }

        public string Author { get; set; }
        public string AuthorId { get; set; }

        public int Total { get; set; }

        public override string ToString() { return Title; }

        public MSYoutubeEntry()
        {
            Thumbnails = new List<MSYoutubeThumbnail>();
            Entries = new List<MSYoutubeEntry>();
        }
    }

    public class MSYoutubeThumbnail
    {
        public string Url { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
    }
}
