using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.Dowload;

namespace MS.Video.Downloader.Service.Youtube.Models
{
    public class YoutubeEntry : Entry
    {
        private readonly MSYoutubeSettings _settings;

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
            _settings = new MSYoutubeSettings(
                "MS.Youtube.Downloader",
                "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg"
                ) {AutoPaging = true, PageSize = 50};
        }

        public override void GetEntries(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            if(YoutubeUrl.Type == VideoUrlType.Channel || YoutubeUrl.ChannelId != "" || YoutubeUrl.FeedId != "")
                FillEntriesChannel(onEntriesReady, onYoutubeLoading);
            if(YoutubeUrl.Type == VideoUrlType.User)
                FillEntriesUser(onEntriesReady, onYoutubeLoading);
        }

        private async void FillEntriesUser(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            var youtubeUrl = YoutubeUrl;
            var request = new MSYoutubeRequest(_settings);
            var items = await request.GetAsync(YoutubeUrl, new Uri(String.Format("https://gdata.youtube.com/feeds/api/users/{0}/playlists?v=2", youtubeUrl.UserId)), onYoutubeLoading);
            if (items == null) return;
            var entries = new List<Entry>();

            try {
                if (!String.IsNullOrWhiteSpace(items.AuthorId)) {
                    var favoritesEntry = new YoutubeEntry(this) {
                        Title = "Favorite Videos",
                        Uri = new Uri("http://www.youtube.com/playlist?list=FL" + items.AuthorId),
                    };
                    entries.Add(favoritesEntry);
                }
                foreach (var member in items.Entries) {
                    var entry = new YoutubeEntry(this) {
                        Title = member.Title,
                        Uri = member.Uri,
                        Description = member.Description
                    };
                    entries.Add(entry);
                }
            }
            catch {
                entries.Clear();
            }
            if (onEntriesReady != null) onEntriesReady(entries);
        }

        private async void FillEntriesChannel(EntriesReady onEntriesReady, MSYoutubeLoading onYoutubeLoading)
        {
            //if (Title == "Favorite Videos") {
            //    FillEntriesYoutubeFavorites(onEntriesReady, onYoutubeLoading);
            //    return;
            //}
            var youtubeUrl = YoutubeUrl;

            //https://gdata.youtube.com/feeds/api/users/{0}/uploads"
            //http://www.youtube.com/feed/UC2xskkQVFEpLcGFnNSLQY0A
            //https://gdata.youtube.com/feeds/api/users/UC2xskkQVFEpLcGFnNSLQY0A/uploads
            var url = "";
            if (!String.IsNullOrEmpty(YoutubeUrl.ChannelId)) {
                url = "https://gdata.youtube.com/feeds/api/playlists/" + youtubeUrl.ChannelId;
            } else if (!String.IsNullOrEmpty(YoutubeUrl.FeedId)) {
                url = String.Format("https://gdata.youtube.com/feeds/api/users/{0}/uploads", YoutubeUrl.FeedId);
            } 
            if (url.Length > 0) {
                var request = new MSYoutubeRequest(_settings);

                MSYoutubeEntry items;
                try {
                    items = await request.GetAsync(YoutubeUrl, new Uri(url), onYoutubeLoading);
                    if (items == null) {
                        if (onEntriesReady != null) onEntriesReady(new List<Entry>());
                        return;
                    }
                }
                catch {
                    if (onEntriesReady != null) onEntriesReady(new List<Entry>());
                    return;
                }
                var entries = GetMembers(items);
                if (onEntriesReady != null) onEntriesReady(entries);
            }
        }

        public override async Task DownloadAsync(MediaType mediaType, bool ignore)
        {
            await base.DownloadAsync(mediaType, ignore);
            var videoInfos = await DownloadUrlResolver.GetDownloadUrlsAsync(Uri);
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
            Status.DownloadState = DownloadState.TitleChanged;
            var videoFile = GetLegalPath(Title) + VideoExtension;
            var fileExists = await FileExists(VideoFolder, videoFile);
            if (!(ignore && fileExists)) {
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
                //await Logger.Log("SUCCESS: " + videoInfo.DownloadUrl+"\r\n");
                await DownloadToFileAsync(new Uri(videoInfo.DownloadUrl), VideoFolder, videoFile, OnYoutubeLoading);
            }

            Status.DownloadState = DownloadState.DownloadFinish;
            if (MediaType == MediaType.Audio) {
                Status.Percentage = 50.0;
                ConvertToMp3(ignore);
            }  else if (OnDownloadStatusChange != null) {
                Status.Percentage = 100.0;
                Status.DownloadState = DownloadState.Ready;
                if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
            }
        }

        private void OnYoutubeLoading(object self, int count, int total)
        {
            var percentage = ((double)count / total) * ((MediaType == MediaType.Audio) ? 50 : 100);
            Status.DownloadState = DownloadState.DownloadProgressChanged;
            Status.Percentage = percentage;
            if (OnDownloadStatusChange != null) OnDownloadStatusChange(null, this, Status);
        }

        private List<Entry> GetMembers(MSYoutubeEntry items)
        {
            var entries = new List<Entry>();
            MSYoutubeEntry[] members;
            try {
                members = items.Entries.Where(member => member.Uri != null).ToArray();
            } catch {
                return entries;
            }
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
                    Uri = member.Uri,
                    Description = member.Description,
                    ThumbnailUrl = thumbnailUrl
                });
            }
            return entries;
        }
/*
        protected async override Task<TagLib.Id3v2.Tag> GetId3Tag()
        {
            var uri = new Uri(String.Format("https://gdata.youtube.com/feeds/api/videos/{0}?v=2", VideoUrl.Id));
            var tag = new TagLib.Id3v2.Tag {Title = Title, Album = Parent == null ? "" : Parent.Title};
            try {
                var xml = new XmlDocument();
                var xmlData = await DownloadToStringAsync(uri);
                xml.LoadXml(xmlData);
                if (xml.DocumentElement != null) {
                    //var manager = new XmlNamespaceManager(xml.NameTable);
                    //manager.AddNamespace("root", "http://www.w3.org/2005/Atom");
                    //manager.AddNamespace("app", "http://www.w3.org/2007/app");
                    //manager.AddNamespace("media", "http://search.yahoo.com/mrss/");
                    //manager.AddNamespace("gd", "http://schemas.google.com/g/2005");
                    //manager.AddNamespace("yt", "http://gdata.youtube.com/schemas/2007");
                    tag.Title = GetText(xml, "media:group/media:title");
                    tag.Lyrics = "MS.Video.Downloader\r\n" + GetText(xml, "media:group/media:description");
                    tag.Copyright = GetText(xml, "media:group/media:license");
                    tag.TrackCount = (uint) Math.Abs(TrackCount);
                    tag.Track = (uint) Math.Abs(Track);
                    if (Parent != null)
                        if (!String.IsNullOrEmpty(Parent.Title)) tag.Album = Parent.Title;
                    tag.Composers = new[] {
                        "MS.Video.Downloader", "Youtube",
                        GetText(xml, "root:link[@rel=\"alternate\"]/@href"),
                        GetText(xml, "root:author/root:name"),
                        GetText(xml, "root:author/root:uri")
                    };
                    var urlNodes = xml.DocumentElement.SelectNodes("media:group/media:thumbnail");
                    var pics = new List<IPicture>();
                    if (urlNodes != null && urlNodes.Count > 0) {
                        foreach (var urlNode in urlNodes) {
                            var attributes = urlNode.Attributes;
                            var urlAttr = attributes.GetNamedItem("url");
                            if (urlAttr != null) {
                                var url = urlAttr.InnerText;
                                var bytes = await DownloadToByteArrayAsync(new Uri(url));
                                pics.Add(new Picture(new ByteVector(bytes)));
                            }
                        }
                    }
                    tag.Pictures = pics.ToArray();
                }
            }
            catch {}
            return tag;
        }
        */
        public async override void ParseChannelInfoFromHtml(VideoUrl url)
        {

             //const string videoTitlePattern = @"\<meta name=""title"" content=""(?<title>.*)""\>";
             //   var videoTitleRegex = new Regex(videoTitlePattern, RegexOptions.IgnoreCase);
             //   var videoTitleMatch = videoTitleRegex.Match(pageSource);
            //   if (videoTitleMatch.Success) {
             //       videoTitle = videoTitleMatch.Groups["title"].Value;
             //       videoTitle = WebUtility.HtmlDecode(videoTitle);

            //var doc = new HtmlDocument();
            //var req = WebRequest.Create(url.Uri);
            //using (var resp = await req.GetResponseAsync()) {
            //    using (var stream = resp.GetResponseStream()) {
            //        if (stream != null) doc.Load(stream, Encoding.UTF8);
            //    }
            //}
            //var metaTags = doc.DocumentNode.Descendants("meta");
            //Title = GetDomValue(metaTags, "property", "og:title", "content");
            //Description = GetDomValue(metaTags, "property", "og:description", "content");
            //ThumbnailUrl = GetDomValue(metaTags, "property", "og:image", "content");
        }

        public override Entry Clone()
        {
            var entry = new YoutubeEntry();
            CopyTo(entry);
            return entry;
        }

        //private static string GetDomValue(IEnumerable<HtmlNode> tags, string queryAttribute, string value, string resultAttribute)
        //{
        //    foreach (var element in tags)
        //        if (element.GetAttributeValue(queryAttribute.ToLowerInvariant(), "") == value)
        //            return WebUtility.HtmlDecode(element.GetAttributeValue(resultAttribute, ""));
        //    return "";
        //}

        //private static string GetText(XmlDocument xml, string xpath)
        //{
        //    var node = xml.DocumentElement.SelectSingleNode(xpath);
        //    return node == null ? "" : node.InnerText;
        //}
    }
}
