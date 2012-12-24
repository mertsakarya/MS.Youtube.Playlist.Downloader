using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MS.Video.Downloader.Service.Youtube.MSYoutube;
using Windows.Foundation;
using Windows.Storage;

namespace MS.Video.Downloader.Service.Youtube.Dowload
{
    public static class DownloadHelper
    {
        private const long BlockSize = (1024 * 1024);


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

        #region GetDownloadUrlsAsync helpers
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
                info.DownloadUri = new Uri(url + "&fallback_host=" + fallbackHost + "&signature=" + sig);
                info.Title = videoTitle;
                videoInfos.Add(info);
            }
            return videoInfos;
        }

        private static async Task<string> GetPageSourceAsync(string videoUrl)
        {
            return await DownloadToStringAsync(new Uri(videoUrl));
        }

        private static bool IsVideoUnavailable(string pageSource)
        {
            return pageSource.Contains("<div id=\"watch-player-unavailable\">");
        }

        private static string NormalizeYoutubeUrl(string url)
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
        #endregion
        
        #region Download helpers
        public static async Task<StorageFolder> GetFolder(StorageFolder baseFolder, string folderName)
        {
            var found = true;
            StorageFolder folder = null;
            try {
                if (String.IsNullOrEmpty(folderName))
                    folder = baseFolder;
                else
                    folder = await baseFolder.GetFolderAsync(folderName);
            } catch (FileNotFoundException) {
                found = false;
            }
            if (!found && folderName != null)
                folder = await baseFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
            return folder;
        }

        public static async Task<StorageFile> GetFile(StorageFolder folder, string fileName)
        {
            try {
                return await folder.CreateFileAsync(fileName);
            } catch (Exception) {
            }
            return await folder.GetFileAsync(fileName);
        }

        public static string GetLegalPath(string text)
        {
            if (String.IsNullOrEmpty(text)) return text;
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(text, "_");
        }


        public static async Task<bool> FileExists(StorageFolder folder, string videoFile)
        {
            var fileExists = false;
            try {
                await folder.CreateFileAsync(videoFile);
            } catch {
                fileExists = true;
            }
            return fileExists;
        }

        public static async Task DownloadToFileAsync(Uri uri, StorageFolder folder, string fileName, MSYoutubeLoading onYoutubeLoading)
        {
            var storageFile = await GetFile(folder, fileName);
            using (var destinationStream = await storageFile.OpenStreamForWriteAsync()) {
                //var properties = await storageFile.GetBasicPropertiesAsync();
                var start = destinationStream.Length; // (long)properties.Size;
                destinationStream.Position = destinationStream.Length;
                await AddToFile(uri, destinationStream, start, start + BlockSize - 1, onYoutubeLoading);
            }
        }

        private static async Task AddToFile(Uri uri, Stream destinationStream, long? start, long? stop, MSYoutubeLoading onYoutubeLoading)
        {
            var content = await DownloadToStreamAsync(uri, start, stop);
            if (content.Headers.ContentRange == null) return;
            long total = (content.Headers.ContentRange.Length ?? 0);
            long to = (content.Headers.ContentRange.To ?? 0);
            //long downloadedLength = (long) content.Headers.ContentLength;
            using (var stream = await content.ReadAsStreamAsync()) {
                if (stream == null) return;
                await stream.CopyToAsync(destinationStream);
                await destinationStream.FlushAsync();
                if (onYoutubeLoading != null) onYoutubeLoading(to, total);
                if (total > to + 1)
                    await AddToFile(uri, destinationStream, to + 1, to + BlockSize, onYoutubeLoading);
            }

        }

        public static async Task<string> DownloadToStringAsync(Uri uri, Encoding encoding = null)
        {
            var content = await DownloadToStreamAsync(uri);
            using (var stream = await content.ReadAsStreamAsync()) {
                using (var destinationStream = new MemoryStream()) {
                    if (stream != null) {
                        await stream.CopyToAsync(destinationStream);
                    }
                    var bytes = destinationStream.ToArray();
                    if (encoding == null)
                        return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                    return encoding.GetString(bytes, 0, bytes.Length);
                }
            }
        }

        public static async Task<byte[]> DownloadToByteArrayAsync(Uri uri, Encoding encoding = null)
        {
            var content = await DownloadToStreamAsync(uri);
            using (var stream = await content.ReadAsStreamAsync()) {
                using (var destinationStream = new MemoryStream()) {
                    if (stream != null) {
                        await stream.CopyToAsync(destinationStream);
                    }
                    var bytes = destinationStream.ToArray();
                    return bytes;
                }
            }
        }

        public static async Task<HttpContent> DownloadToStreamAsync(Uri uri, long? start = null, long? end = null)
        {
            var req = new HttpClient();
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            if (start != null || end != null) {
                message.Headers.Range = new RangeHeaderValue(start, end);
            }
            message.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11");
            var resp = await req.SendAsync(message);
            return resp.Content;
        }
        #endregion
    }
}
