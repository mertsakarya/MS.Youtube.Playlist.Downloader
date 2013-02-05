using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using ms.video.downloader.service.MSYoutube;

namespace ms.video.downloader.service.Dowload
{
    public static class DownloadHelper
    {
        private const long BlockSize = (1024 * 1024);


        public static IEnumerable<VideoInfo> GetDownloadUrlsAsync(Uri videoUri)
        {
            if (videoUri == null) throw new ArgumentNullException("videoUri");

            var videoUrl = NormalizeYoutubeUrl(videoUri.ToString());
            var id = YoutubeUrl.GetVideoId(new Uri(videoUrl));
            var pageSource = GetPageSourceAsync(videoUrl);
            if (IsVideoUnavailable(pageSource)) throw new Exception("Video not available");
            var requestUrl = String.Format("http://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", id);
            var source = GetPageSourceAsync(requestUrl);
            try {
                var videoInfos = GetVideoInfos(source);
                return videoInfos;
            } catch (Exception ex) {
                ThrowYoutubeParseException(ex);
            }
            ThrowYoutubeParseException(null);
            return null;
        }

        #region GetDownloadUrlsAsync helpers
        private static IEnumerable<VideoInfo> GetVideoInfos(string source)
        {
            var t = HttpUtility.ParseQueryString(source); 
            var videoTitle = t.Get("title");
            var splitByUrl = t.Get("url_encoded_fmt_stream_map");
            if(String.IsNullOrWhiteSpace(splitByUrl)) return new List<VideoInfo>();
            var splitByUrls = splitByUrl.Split(',');
            var videoInfos = new List<VideoInfo>();
            foreach (var s in splitByUrls) {
                var queries = HttpUtility.ParseQueryString(s);
                var url = queries.Get("url");
                if(url == null) continue;
                var decoder = HttpUtility.ParseQueryString(url.Substring(url.IndexOf('?')));
                byte formatCode;
                if (!Byte.TryParse(decoder.Get("itag"), out formatCode)) continue;
                var fallbackHost = HttpUtility.UrlDecode(queries.Get("fallback_host"));
                var sig = HttpUtility.UrlDecode(queries.Get("sig"));
                foreach (var videoInfoDefault in VideoInfo.Defaults) {
                    if (videoInfoDefault.FormatCode == formatCode) {
                        var info = videoInfoDefault.Clone();
                        info.DownloadUri = new Uri(url + "&fallback_host=" + fallbackHost + "&signature=" + sig);
                        info.Title = videoTitle;
                        videoInfos.Add(info);
                        break;
                    }
                }
            }
            return videoInfos;
        }

        private static string GetPageSourceAsync(string videoUrl)
        {
            return DownloadToStringAsync(new Uri(videoUrl));
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
        public static StorageFolder GetFolder(StorageFolder baseFolder, string folderName)
        {
            if (!Directory.Exists(baseFolder.ToString())) Directory.CreateDirectory(baseFolder.ToString());
            var path = baseFolder + "\\" + folderName;
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            return new StorageFolder(path);
        }

        public static StorageFile GetFile(StorageFolder folder, string fileName)
        {
            return new StorageFile {StorageFolder = folder, FileName = fileName};
        }

        public static string GetLegalPath(string text)
        {
            if (String.IsNullOrEmpty(text)) return text;
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(text, "_");
        }


        public static bool FileExists(StorageFolder folder, string videoFile)
        {
            var file = new StorageFile {StorageFolder = folder, FileName = videoFile};
            return file.Exists();
        }

        public static void DownloadToFileAsync(YoutubeEntry entry, Uri uri, StorageFile storageFile, MSYoutubeLoading onYoutubeLoading)
        {
            if (entry.ExecutionStatus != ExecutionStatus.Normal) return;
            using (var destinationStream = storageFile.OpenStreamForWriteAsync()) {
                if (destinationStream == null) return;
                var start = destinationStream.Length;
                destinationStream.Position = destinationStream.Length;
                AddToFile(entry, uri, destinationStream, start, start + BlockSize - 1, onYoutubeLoading, storageFile);
            }
        }

        private static void AddToFile(YoutubeEntry entry, Uri uri, Stream destinationStream, long? start, long? stop, MSYoutubeLoading onYoutubeLoading, StorageFile storageFile, bool retry = false)
        {
            if (entry.ExecutionStatus != ExecutionStatus.Normal) return;
            var response = DownloadToStreamAsync(uri, start, stop);
            var cache = CacheManager.Instance;
            if (response == null || response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable) {
                cache.SetUrl(entry.YoutubeUrl.VideoId, entry.Title, (new FileInfo(storageFile.ToString())).Length);
                return;
            }
            var range = GetRange(response);
            var total =  range.Length;
            cache.SetUrl(entry.YoutubeUrl.VideoId, entry.Title, total);
            var to = range.To;
            using (var stream = response.GetResponseStream()) {
                if (stream == null) return;
                try {
                    stream.CopyTo(destinationStream);
                    destinationStream.Flush();
                } catch (WebException ex) {
                    if (retry) return;
                    AddToFile(entry, uri, destinationStream, start, stop, onYoutubeLoading, storageFile, true);
                }
                if (onYoutubeLoading != null && entry.ExecutionStatus == ExecutionStatus.Normal) onYoutubeLoading(to, total);
                    if (total > to + 1)
                        AddToFile(entry, uri, destinationStream, to + 1, to + BlockSize, onYoutubeLoading, storageFile);
            }
        }

        private static ResponseContentRange GetRange(HttpWebResponse response)
        {
            var rangeHeader = response.Headers["Content-Range"];
            var range = new ResponseContentRange {ContentLength = response.ContentLength};
            if (rangeHeader == null || rangeHeader.Length <= 6 || !rangeHeader.StartsWith("bytes ")) return range;
            rangeHeader = rangeHeader.Substring(6);
            var posStart = rangeHeader.IndexOf('-');
            var posTotal = rangeHeader.IndexOf('/');
            if (posTotal <= 0) return range;
            if (posStart <= 0) range.From = 0;
            else {
                var strStart = rangeHeader.Substring(0, posStart);
                range.From = long.Parse(strStart);
            }
            var strStop = rangeHeader.Substring(posStart + 1, posTotal - posStart - 1);
            range.To = long.Parse(strStop);
            var strTotal = rangeHeader.Substring(posTotal + 1);
            range.Length = long.Parse(strTotal);
            return range;
        }

        public static string DownloadToStringAsync(Uri uri, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            var bytes = DownloadToByteArrayAsync(uri, encoding);
            return encoding.GetString(bytes);
        }

        public static byte[] DownloadToByteArrayAsync(Uri uri, Encoding encoding = null)
        {
            var response = DownloadToStreamAsync(uri);
            using (var stream = response.GetResponseStream()) {
                if (stream == null) return new byte[0];
                using (var destinationStream = new MemoryStream()) {
                    stream.CopyTo(destinationStream);
                    destinationStream.Flush();
                    var bytes = destinationStream.ToArray();
                    return bytes;
                }
            }
        }
        
        public static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
                output.Write(buffer, 0, read);
            }
        }

        public static HttpWebResponse DownloadToStreamAsync(Uri uri, long? start = null, long? end = null)
        {
            var req = (HttpWebRequest) WebRequest.Create(uri);
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11";
            if (start != null || end != null) req.AddRange((int)(start ?? 0), (int)(end ?? 0));
            try {
                var response = req.GetResponse() as HttpWebResponse;
                return response;
            }
            catch(WebException ex) {
                return ex.Response as HttpWebResponse;
            }
        }
        #endregion
    }

    internal class ResponseContentRange
    {
        public long Length { get; set; }
        public long From { get; set; }
        public long To { get; set; }
        public long ContentLength { get; set; }
    }
}
