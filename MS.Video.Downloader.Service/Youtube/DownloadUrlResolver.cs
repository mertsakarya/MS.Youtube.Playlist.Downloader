using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MS.Video.Downloader.Service.Youtube
{
    public static class DownloadUrlResolver
    {
        public static IEnumerable<VideoInfo> GetDownloadUrls(string videoUrl)
        {
            if (videoUrl == null)
                throw new ArgumentNullException("videoUrl");

            videoUrl = NormalizeYoutubeUrl(videoUrl);

            var pageSource = GetPageSource(videoUrl);
            var videoTitle = GetVideoTitle(pageSource);

            var id = HttpUtility.ParseQueryString(new Uri(videoUrl).Query)["v"];

            var requestUrl = String.Format("http://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", id);

            var source = GetPageSource(requestUrl);

            var decoded = HttpUtility.UrlDecode(source);
            decoded = HttpUtility.UrlDecode(decoded);

            try {
                var downloadUrls = ExtractDownloadUrls(decoded);
                return GetVideoInfos(downloadUrls, videoTitle);
            } catch (Exception ex) {
                ThrowYoutubeParseException(ex);
            }

            if (IsVideoUnavailable(pageSource)) {
                throw new Exception("Video not available");
            }

            // If everything else fails, throw a generic YoutubeParseException
            ThrowYoutubeParseException(null);

            return null; // Will never happen, but the compiler requires it
        }

        public static async Task<IEnumerable<VideoInfo>> GetDownloadUrlsAsync(string videoUrl)
        {
            if (videoUrl == null)
                throw new ArgumentNullException("videoUrl");

            videoUrl = NormalizeYoutubeUrl(videoUrl);

            var pageSource = await GetPageSourceAsync(videoUrl);
            var videoTitle = GetVideoTitle(pageSource);

            var id = HttpUtility.ParseQueryString(new Uri(videoUrl).Query)["v"];

            var requestUrl = String.Format("http://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", id);

            var source = GetPageSource(requestUrl);

            var decoded = HttpUtility.UrlDecode(source);
            decoded = HttpUtility.UrlDecode(decoded);

            try {
                var downloadUrls = ExtractDownloadUrls(decoded);
                return GetVideoInfos(downloadUrls, videoTitle);
            } catch (Exception ex) {
                ThrowYoutubeParseException(ex);
            }

            if (IsVideoUnavailable(pageSource)) {
                throw new Exception("Video not available");
            }

            // If everything else fails, throw a generic YoutubeParseException
            ThrowYoutubeParseException(null);

            return null; // Will never happen, but the compiler requires it
        }

        private static IEnumerable<Uri> ExtractDownloadUrls(string availableFormats)
        {
            const string argument = "url=";
            const string endOfQueryString = "&quality";

            var urlList = Regex.Split(availableFormats, argument).ToList();

            // Format the URL
            var urls = from url in urlList
                       let index = url.IndexOf(endOfQueryString, StringComparison.Ordinal)
                       where index > 0
                       let finalUrl = url.Substring(0, index).Replace("&sig=", "&signature=")
                       select new Uri(Uri.UnescapeDataString(finalUrl));

            return urls;
        }

        private static async Task<string> GetPageSourceAsync(string videoUrl)
        {
            string pageSource;
            var req = WebRequest.Create(videoUrl);

            using (var resp = await req.GetResponseAsync()) {
                var stream = resp.GetResponseStream();
                pageSource = stream != null ? new StreamReader(stream, Encoding.UTF8).ReadToEnd() : "";
            }
            return pageSource;
        }

        private static string GetPageSource(string videoUrl)
        {
            string pageSource;
            var req = WebRequest.Create(videoUrl);

            using (var resp = req.GetResponse()) {
                var stream = resp.GetResponseStream();
                pageSource = stream != null ? new StreamReader(stream, Encoding.UTF8).ReadToEnd() : "";
            }
            return pageSource;
        }

        private static IEnumerable<VideoInfo> GetVideoInfos(IEnumerable<Uri> downloadUrls, string videoTitle)
        {
            var downLoadInfos = new List<VideoInfo>();

            foreach (var url in downloadUrls) {
                var queryString = HttpUtility.ParseQueryString(url.Query);

                // for this version, only get the download URL
                var formatCode = Byte.Parse(queryString["itag"]);

                // Currently based on YouTube specifications (later we'll depend on the MIME type returned from the web request)
                var item = VideoInfo.Defaults.SingleOrDefault(videoInfo => videoInfo.FormatCode == formatCode);
                VideoInfo info;
                if (item != null) {
                    info = item.Clone();
                    info.DownloadUrl = url.ToString();
                    info.Title = videoTitle;
                } else {
                    info = new VideoInfo(formatCode);
                }
                downLoadInfos.Add(info);
            }

            return downLoadInfos;
        }

        private static string GetVideoTitle(string pageSource)
        {
            string videoTitle = null;

            try {
                const string videoTitlePattern = @"\<meta name=""title"" content=""(?<title>.*)""\>";
                var videoTitleRegex = new Regex(videoTitlePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var videoTitleMatch = videoTitleRegex.Match(pageSource);

                if (videoTitleMatch.Success) {
                    videoTitle = videoTitleMatch.Groups["title"].Value;
                    videoTitle = HttpUtility.HtmlDecode(videoTitle);

                    // Remove the invalid characters in file names
                    // In Windows they are: \ / : * ? " < > |
                    videoTitle = Regex.Replace(videoTitle, @"[:\*\?""\<\>\|]", String.Empty);
                    videoTitle = videoTitle.Replace("\\", "-").Replace("/", "-").Trim();
                }
            } catch (Exception) {
                videoTitle = null;
            }

            return videoTitle;
        }

        private static bool IsVideoUnavailable(string pageSource)
        {
            const string unavailableContainer = "<div id=\"watch-player-unavailable\">";

            return pageSource.Contains(unavailableContainer);
        }

        public static string NormalizeYoutubeUrl(string url)
        {
            url = url.Trim();

            if (url.StartsWith("https://")) {
                url = "http://" + url.Substring(8);
            } else if (!url.StartsWith("http://")) {
                url = "http://" + url;
            }

            url = url.Replace("youtu.be/", "youtube.com/watch?v=");
            url = url.Replace("www.youtube.com", "youtube.com");

            if (url.StartsWith("http://youtube.com/v/")) {
                url = url.Replace("youtube.com/v/", "youtube.com/watch?v=");
            } else if (url.StartsWith("http://youtube.com/watch#")) {
                url = url.Replace("youtube.com/watch#", "youtube.com/watch?");
            }

            if (!url.StartsWith("http://youtube.com/watch")) {
                throw new ArgumentException("URL is not a valid youtube URL!");
            }

            return url;
        }

        private static void ThrowYoutubeParseException(Exception innerException)
        {
            throw new Exception("Could not parse the Youtube page.\n" +
                                            "This may be due to a change of the Youtube page structure.\n" +
                                            "Please report this bug at www.github.com/flagbug/YoutubeExtractor/issues", innerException);
        }
    }
}
