using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            var pageSource = await GetPageSourceAsync(videoUrl);
            var requestUrl = String.Format("http://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", id);
            var source = await GetPageSourceAsync(requestUrl);

            try {
                string videoTitle;
                var downloadUrls = ExtractDownloadUrls(source, out videoTitle);
                var videoInfos = GetVideoInfos(downloadUrls, videoTitle);
                return videoInfos;
            } catch (Exception ex) {
                ThrowYoutubeParseException(ex);
            }

            if (IsVideoUnavailable(pageSource)) throw new Exception("Video not available");

            ThrowYoutubeParseException(null);

            return null;
        }
        private static IEnumerable<Uri> ExtractDownloadUrls(string source, out string title)
        {
            var urls = new List<Uri>();
            var list = ParseFormEncoded(source);
            title = "";
            foreach (var kv in list) {
                if (kv[0] == "title") title = kv[1];
                if (kv[0] != "url_encoded_fmt_stream_map") continue;
                var list2 = kv[1].Split(',');
                foreach (var kv2 in list2) {
                    var list3 = ParseFormEncoded(kv2);

                    var url = "";
                    var fallbackHost = "";
                    var sig = "";

                    foreach (var kv3 in list3) {
                        switch (kv3[0]) {
                            case "url":
                                url = kv3[1];
                                break;
                            case "fallback_host":
                                fallbackHost = kv3[1];
                                break;
                            case "sig":
                                sig = kv3[1];
                                break;
                        }
                    }
                    if(String.IsNullOrEmpty(url)) throw new Exception("Could not find URL");
                    if (url.IndexOf("&fallback_host=", StringComparison.Ordinal) < 0)
                        url += "&fallback_host=" + WebUtility.UrlEncode(fallbackHost);
                    if (url.IndexOf("&signature=", StringComparison.Ordinal) < 0)
                        url += "&signature=" + WebUtility.UrlEncode(sig);
                    urls.Add(new Uri(url));
                }
            }
            return urls;
        }

        private static IEnumerable<string[]> ParseFormEncoded(string qs)
        {
            var parameters = qs.Split('&');
            var list = new List<string[]>(parameters.Length);
            foreach (var parameter in parameters) {
                var parameterKeyValue = parameter.Split('=');
                var key = WebUtility.UrlDecode(parameterKeyValue[0]).ToLowerInvariant();
                var value = WebUtility.UrlDecode(parameterKeyValue[1]);
                list.Add(new[] { key, value });
            }
            return list;
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
                byte formatCode;
                if(!Byte.TryParse(a.GetFirstValueByName("itag"), out formatCode)) continue;

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
