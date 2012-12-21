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
                var videoInfos = GetVideoInfos(source);
                return videoInfos;
            } catch (Exception ex) {
                ThrowYoutubeParseException(ex);
            }

            if (IsVideoUnavailable(pageSource)) throw new Exception("Video not available");

            ThrowYoutubeParseException(null);

            return null;
        }

        private static IEnumerable<VideoInfo> GetVideoInfos(string source)
        {
            var t = new WwwFormUrlDecoder(source);
            var videoTitle = t.GetFirstValueByName("title");
            var splitByUrls = t.GetFirstValueByName("url_encoded_fmt_stream_map").Split(',');
            var videoInfos = new List<VideoInfo>();
            foreach (var s in splitByUrls) {
                var queries = new WwwFormUrlDecoder(s);
                var url = queries.GetFirstValueByName("url");
                var decoder = new WwwFormUrlDecoder(url.Substring(url.IndexOf('?')));
                byte formatCode;
                if (!Byte.TryParse(decoder.GetFirstValueByName("itag"), out formatCode)) continue;
                var fallbackHost = WebUtility.UrlDecode(queries.GetFirstValueByName("fallback_host"));
                var sig = WebUtility.UrlDecode(queries.GetFirstValueByName("sig"));
                var item = VideoInfo.Defaults.SingleOrDefault(videoInfo => videoInfo.FormatCode == formatCode);
                var info = item != null ? item.Clone() : new VideoInfo(formatCode);
                info.DownloadUrl = url + "&fallback_host=" + fallbackHost + "&signature=" + sig;
                info.Title = videoTitle;
                videoInfos.Add(info);
            }
            return videoInfos;
        }

        private static async Task<string> GetPageSourceAsync(string videoUrl)
        {
            return await Entry.DownloadToStringAsync(new Uri(videoUrl));
        }

        private static bool IsVideoUnavailable(string pageSource)
        {
            return pageSource.Contains("<div id=\"watch-player-unavailable\">");
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
