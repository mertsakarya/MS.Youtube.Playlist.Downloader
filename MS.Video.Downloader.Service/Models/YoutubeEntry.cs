using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Google.GData.Client;
using Google.YouTube;
using HtmlAgilityPack;
using MS.Video.Downloader.Service.Youtube;
using TagLib;
using File = System.IO.File;

namespace MS.Video.Downloader.Service.Models
{
    public class YoutubeEntry : Entry
    {
        private readonly YouTubeRequestSettings _settings;

        protected YoutubeUrl YoutubeUrl {
            get
            {
                var youtubeUrl = VideoUrl as YoutubeUrl;
                if (youtubeUrl == null) throw new Exception("Inavlid URL");
                return youtubeUrl;
            }
        }

        public YoutubeEntry(Entry parent = null) : base(parent)
        {
            _settings = new YouTubeRequestSettings(
                "MS.Youtube.Downloader",
                "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg"
                //key
                ) {AutoPaging = true, PageSize = 50};
        }

        public override void GetEntries(EntriesReady onEntriesReady)
        {
            if(YoutubeUrl.ChannelId != "")
                FillEntriesChannel(onEntriesReady);
            if(YoutubeUrl.Type == VideoUrlType.User)
                FillEntriesUser(onEntriesReady);
        }

        private async void FillEntriesUser(EntriesReady onEntriesReady)
        {
            await Task.Factory.StartNew(() => {
                var youtubeUrl = YoutubeUrl;
                var request = new YouTubeRequest(_settings);
                var items = request.Get<Playlist>(new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/playlists?v=2", youtubeUrl.UserId)));
                if (items == null) return;
                var entries = new List<Entry>();
                try {
                    entries.Add(new YoutubeEntry(this) {
                        Title = "Favorites",
                        Content = youtubeUrl.UserId,
                        VideoUrl = VideoUrl.Create(youtubeUrl.UserId, VideoUrl.Provider, VideoUrlType.Channel)
                    });
                    foreach (var member in items.Entries) {
                        var entry = new YoutubeEntry(this) {
                            Title = member.Title,
                            Url = member.PlaylistsEntry.AlternateUri.ToString(),
                            Description = member.Summary,
                        };
                        entries.Add(entry);
                    }
                }
                catch {
                    entries.Clear();
                }
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }

        private async void FillEntriesChannel(EntriesReady onEntriesReady)
        {
            if (Title == "Favorites") {
                FillEntriesYoutubeFavorites(onEntriesReady);
                return;
            }
            var youtubeUrl = YoutubeUrl;
            if (!String.IsNullOrEmpty(youtubeUrl.ChannelId)) {
                await Task.Factory.StartNew(() => {
                    var request = new YouTubeRequest(_settings);
                    var items = request.Get<PlayListMember>(new Uri("http://gdata.youtube.com/feeds/api/playlists/" + youtubeUrl.ChannelId));
                    if (items == null) return;
                    var entries = GetMembers(items);
                    if (onEntriesReady != null) onEntriesReady(entries);
                }).ConfigureAwait(false);
            }
        }

        private async void FillEntriesYoutubeFavorites(EntriesReady onEntriesReady)
        {
            await Task.Factory.StartNew(() => {
                var request = new YouTubeRequest(_settings);
                var items = request.Get<PlayListMember>( new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/favorites", Content)));
                if (items == null) return;
                var entries = GetMembers(items);
                if (onEntriesReady != null) onEntriesReady(entries);
            }).ConfigureAwait(false);
        }

        public override async void DownloadAsync(MediaType mediaType, string baseFolder, bool ignore = false)
        {
            base.DownloadAsync(mediaType, baseFolder, ignore);
            var videoInfos = await DownloadUrlResolver.GetDownloadUrlsAsync(Url);
            var videoInfo = videoInfos.FirstOrDefault(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            if (videoInfo == null) {
                Status.DownloadState = DownloadState.Error;
                Status.Percentage = 100.0;
                Status.UserData = "SKIPPING! No MP4 with 360 pixel resolution";
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                return;
            }
            Title = videoInfo.Title;
            VideoExtension = videoInfo.VideoExtension;
            Status.DownloadState = DownloadState.DownloadStart;
            var videoFile = Path.Combine(VideoFolder, GetLegalPath(Title) + VideoExtension);

            if (!ignore || !File.Exists(videoFile))
                await DownloadToFileAsync(new Uri(videoInfo.DownloadUrl), videoFile);

            Status.DownloadState = DownloadState.DownloadFinish;
            Status.Percentage = 100.0;
            if (MediaType == MediaType.Audio)
                ConvertToMp3(ignore);
            else if (OnDownloadStatusChange != null) {
                Status.DownloadState = DownloadState.Ready;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
            }
        }

        private List<Entry> GetMembers(Feed<PlayListMember> items)
        {
            var entries = new List<Entry>();
            PlayListMember[] members;
            try {
                members = items.Entries.Where(member => member.WatchPage != null).ToArray();
            } catch {
                return entries;
            }
            var count = members.Length;
            foreach (var member in members) {
                var thumbnailUrl = "";
                var thumbnailUrls = new List<string>(member.Thumbnails.Count);
                foreach (var tn in member.Thumbnails) {
                    thumbnailUrls.Add(tn.Url);
                    if (tn.Height == "90" && tn.Width == "120")
                        thumbnailUrl = tn.Url;
                }
                entries.Add(new YoutubeEntry(this) {
                    Title = member.Title,
                    Url = member.WatchPage.ToString(),
                    Description = member.Description,
                    ThumbnailUrl = thumbnailUrl,
                    Content = member.Content,
                    Track = member.Position,
                    TrackCount = count
                });
            }
            return entries;
        }

        protected override TagLib.Id3v2.Tag GetId3Tag()
        {
            var uri = new Uri(String.Format("https://gdata.youtube.com/feeds/api/videos/{0}?v=2", VideoUrl.Id));
            var tag = new TagLib.Id3v2.Tag {Title = Title, Album = Parent == null ? "" : Parent.Title};
            try {
                var xml = new XmlDocument();
                var req = WebRequest.Create(uri);
                using (var resp = req.GetResponse()) {
                    using (var stream = resp.GetResponseStream()) {
                        if (stream != null) xml.Load(stream);
                    }
                }
                if (xml.DocumentElement != null) {
                    var manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("root", "http://www.w3.org/2005/Atom");
                    manager.AddNamespace("app", "http://www.w3.org/2007/app");
                    manager.AddNamespace("media", "http://search.yahoo.com/mrss/");
                    manager.AddNamespace("gd", "http://schemas.google.com/g/2005");
                    manager.AddNamespace("yt", "http://gdata.youtube.com/schemas/2007");
                    tag.Title = GetText(xml, "media:group/media:title", manager);
                    tag.Lyrics = "MS.Video.Downloader\r\n" + GetText(xml, "media:group/media:description", manager);
                    tag.Copyright = GetText(xml, "media:group/media:license", manager);
                    tag.TrackCount = (uint) Math.Abs(TrackCount);
                    tag.Track = (uint) Math.Abs(Track);
                    if (Parent != null)
                        if (!String.IsNullOrEmpty(Parent.Title)) tag.Album = Parent.Title;
                    tag.Composers = new[] {
                        "MS.Video.Downloader", "Youtube",
                        GetText(xml, "root:link[@rel=\"alternate\"]/@href", manager),
                        GetText(xml, "root:author/root:name", manager),
                        GetText(xml, "root:author/root:uri", manager),
                    };
                    var urlNodes = xml.DocumentElement.SelectNodes("media:group/media:thumbnail", manager);
                    var webClient = new WebClient();
                    var pics = new List<IPicture>();
                    if (urlNodes != null && urlNodes.Count > 0)
                        pics.AddRange((from XmlNode urlNode in urlNodes
                                       let attributes = urlNode.Attributes
                                       where attributes != null
                                       where attributes != null
                                       select attributes["url"]
                                       into url where url != null select webClient.DownloadData(url.Value)
                                       into data select new Picture(new ByteVector(data))).Cast<IPicture>());
                    tag.Pictures = pics.ToArray();
                }
            }
            catch {}
            return tag;
        }

        public async override void ParseChannelInfoFromHtml(VideoUrl url)
        {
            await Task.Factory.StartNew(() => {
                var doc = new HtmlDocument();
                var req = WebRequest.Create(url.Uri);
                using (var resp = req.GetResponse()) {
                    using (var stream = resp.GetResponseStream()) {
                        if (stream != null) doc.Load(stream, Encoding.UTF8);
                    }
                }
                Title = GetDomValue(doc, "//meta", "property", "og:title", "content");
                Description = GetDomValue(doc, "//meta", "property", "og:description", "content");
                ThumbnailUrl = GetDomValue(doc, "//meta", "property", "og:image", "content");
            }).ConfigureAwait(true);
        }

        private static string GetDomValue(HtmlDocument document, string xpath, string queryAttribute, string value, string resultAttribute)
        {
            var tags = document.DocumentNode.SelectNodes(xpath);
            foreach (var element in tags)
                if (element.GetAttributeValue(queryAttribute.ToLowerInvariant(), "") == value)
                    return System.Web.HttpUtility.HtmlDecode(element.GetAttributeValue(resultAttribute, ""));
            return "";
        }

        private static string GetText(XmlDocument xml, string xpath, XmlNamespaceManager manager)
        {
            var node = xml.DocumentElement.SelectSingleNode(xpath, manager);
            return node == null ? "" : node.InnerText;
        }
    }
}
