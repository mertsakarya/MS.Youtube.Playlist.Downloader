using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.Models;
using Windows.Foundation;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public static class DownloadUrlResolver
    {

        public static async Task<IEnumerable<VideoInfo>> GetDownloadUrlsAsync(Uri videoUri)
        {
            if (videoUri == null) throw new ArgumentNullException("videoUri");

            var videoUrl = NormalizeYoutubeUrl(videoUri.ToString());
            var id = YoutubeUrl.GetVideoId(new Uri(videoUrl));
            var videoTitle = await GetVideoTitle(videoUrl);
            var requestUrl = String.Format("http://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", id);
            var source = await GetPageSourceAsync(requestUrl);
            var decoded = WebUtility.UrlDecode(source);
            decoded = WebUtility.UrlDecode(decoded);

            try {
                var downloadUrls = ExtractDownloadUrls(decoded);
                return GetVideoInfos(downloadUrls, videoTitle);
            } catch (Exception ex) {
                ThrowYoutubeParseException(ex);
            }

            //if (IsVideoUnavailable(pageSource)) {
            //    throw new Exception("Video not available");
            //}

            // If everything else fails, throw a generic YoutubeParseException
            ThrowYoutubeParseException(null);

            return null; // Will never happen, but the compiler requires it
        }
        #region test code
        private static string GetTitle(string qs)
        {
            var querySegments = qs.Split('&');
            foreach (var segment in querySegments) {
                var parts = segment.Split('=');
                if (parts.Length <= 0) continue;
                var key = parts[0].Trim(new char[] { '?', ' ' });
                if (key == "title") {
                    return parts[1].Trim();
                }
            }
            return "";
        }

        private static string ParseQS(string source)
        {
            var qs = WebUtility.UrlDecode(source);
            var querySegments = qs.Split('&');
            var list = new List<string[]>();
            foreach (var segment in querySegments) {
                var parts = segment.Split('=');
                if (parts.Length <= 0) continue;
                var key = parts[0].Trim(new char[] { '?', ' ' });
                var val = parts[1].Trim();
                list.Add( new[] {key, val});
            }
            var str = "";
            foreach (var i in list)
                str += i[0] + "\t" + i[1] + "\r\n";
            return str;
        }

        private static string ExtractVideoTitle(string decoded)
        {
            var dict = ParseQueryString(decoded);
            var titles = dict["title"];
            return titles[titles.Count - 1];
        }

        private static Dictionary<string, List<string>> ParseQueryString(string qs)
        {
            var d = new Dictionary<string, List<string>>();
            var querySegments = qs.Split('&');
            foreach (var segment in querySegments) {
                var parts = segment.Split('=');
                if (parts.Length <= 0) continue;
                var key = parts[0].Trim(new char[] { '?', ' ' });
                var val = parts[1].Trim();
                if (!d.ContainsKey(key)) {
                    d.Add(key, new List<string> {val});
                } else {
                    d[key].Add(val);
                }
            }
            return d;
        }
#endregion
        
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
            return await Entry.DownloadToStringAsync(new Uri(videoUrl));
        }

        private static IEnumerable<VideoInfo> GetVideoInfos(IEnumerable<Uri> downloadUrls, string videoTitle)
        {
            var downLoadInfos = new List<VideoInfo>();

            foreach (var url in downloadUrls) {
                var a = new WwwFormUrlDecoder(url.Query);
                var formatCode = Byte.Parse(a.GetFirstValueByName("itag"));

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

        private static async Task<string> GetVideoTitle(string videoUrl)
        {
            var pageSource = await GetPageSourceAsync(videoUrl);

            string videoTitle = null;

            try {
                const string videoTitlePattern = @"\<meta name=""title"" content=""(?<title>.*)""\>";
                var videoTitleRegex = new Regex(videoTitlePattern, RegexOptions.IgnoreCase);
                var videoTitleMatch = videoTitleRegex.Match(pageSource);

                if (videoTitleMatch.Success) {
                    videoTitle = videoTitleMatch.Groups["title"].Value;
                    videoTitle = WebUtility.HtmlDecode(videoTitle);

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
