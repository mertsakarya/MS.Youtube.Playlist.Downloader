using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml;
using TagLib;
using File = TagLib.File;
using Tag = TagLib.Id3v2.Tag;

namespace ms.video.downloader.service.Dowload
{
    public class AudioConverter
    {
        private readonly EntryDownloadStatusEventHandler _onEntryDownloadStatusChange;
        private readonly YoutubeEntry _youtubeEntry;
        private readonly string _applicationPath;

        public AudioConverter(YoutubeEntry youtubeEntry, EntryDownloadStatusEventHandler onEntryDownloadStatusChange)
        {
            _youtubeEntry = youtubeEntry;
            _onEntryDownloadStatusChange = onEntryDownloadStatusChange;
            _applicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        public void ConvertToMp3(bool ignoreIfFileExists = false)
        {
            if (_youtubeEntry.DownloadState != DownloadState.DownloadFinish) return;
            var title = DownloadHelper.GetLegalPath(_youtubeEntry.Title);
            var audioFileName = title + ".mp3";
            var videoFileName = title + _youtubeEntry.VideoExtension;
            var fileExists = DownloadHelper.FileExists(_youtubeEntry.VideoFolder, videoFileName);
            if (!fileExists) return;
            fileExists = DownloadHelper.FileExists(_youtubeEntry.DownloadFolder, audioFileName);
            if (ignoreIfFileExists && fileExists) {
                if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, DownloadState.Ready, 100.0);
            } else {
                if (fileExists) _youtubeEntry.DownloadFolder.GetFileAsync(audioFileName).DeleteAsync();
                var videoFile = _youtubeEntry.VideoFolder.GetFileAsync(videoFileName);
                var audioFile = _youtubeEntry.DownloadFolder.CreateFileAsync(audioFileName);
                TranscodeFile(videoFile, audioFile);
            }
        }


        protected void TranscodeFile(StorageFile videoFile, StorageFile audioFile)
        {
            var arguments = String.Format("-i \"{0}\" -acodec mp3 -y -ac 2 -ab 160 \"{1}\"", videoFile, audioFile);
            var process = new Process {
                EnableRaisingEvents = true,
                StartInfo = {
                    FileName = _applicationPath + "\\Executables\\ffmpeg.exe",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    RedirectStandardError = false,
                    RedirectStandardOutput = false,
                    UseShellExecute = false                    
                }
            };
            process.Exited += (sender, args) => {
                DownloadState state;
                if (process.ExitCode == 0) {
                    Tag(audioFile.ToString());
                    state = DownloadState.Ready;
                } else state = DownloadState.Error;
                if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, state, 100.0);
            };
            process.Start();
        }


        #region TagLib

        private void Tag(string filename)
        {
            var file = File.Create(filename);
            if (file == null) return;
            var tag = GetId3Tag();
            tag.CopyTo(file.Tag, true);
            file.Tag.Pictures = tag.Pictures;
            file.Save();
        }

        private Tag GetId3Tag()
        {
            var uri =
                new Uri(String.Format("https://gdata.youtube.com/feeds/api/videos/{0}?v=2", _youtubeEntry.YoutubeUrl.Id));
            var tag = new Tag { Title = _youtubeEntry.Title, Album = _youtubeEntry.ChannelName };
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
                    tag.Album = _youtubeEntry.ChannelName;
                    tag.Composers = new[] {
                        "MS.Video.Downloader", "Youtube",
                        GetText(xml, "root:link[@rel=\"alternate\"]/@href", manager),
                        GetText(xml, "root:author/root:name", manager),
                        GetText(xml, "root:author/root:uri", manager)
                    };
                    var urlNodes = xml.DocumentElement.SelectNodes("media:group/media:thumbnail", manager);
                    var webClient = new WebClient();
                    var pics = new List<IPicture>();
                    if (urlNodes != null && urlNodes.Count > 0) {
                        foreach (XmlNode urlNode in urlNodes) {
                            var attributes = urlNode.Attributes;
                            if (attributes == null || attributes.Count <= 0) continue;
                            var url = attributes["url"];
                            if (url == null || String.IsNullOrEmpty(url.Value)) continue;
                            var data = webClient.DownloadData(url.Value);
                            IPicture pic = new Picture(new ByteVector(data));
                            pics.Add(pic);
                        }
                    }
                    tag.Pictures = pics.ToArray();
                }
            } catch { }
            return tag;
        }

        private static string GetText(XmlDocument xml, string xpath, XmlNamespaceManager manager)
        {
            if (xml.DocumentElement != null) {
                var node = xml.DocumentElement.SelectSingleNode(xpath, manager);
                return node == null ? "" : node.InnerText;
            }
            return "";
        }

        #endregion

    }
}