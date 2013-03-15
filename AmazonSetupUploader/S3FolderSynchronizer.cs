using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;

namespace AmazonSetupUploader
{
    public class S3EndPoint
    {
        public string Region { get; set; }
        public string EndPoint { get; set; }
        public string Location { get; set; }
        public override string ToString()
        {
 	         return Region;
        }

    }

    public class S3Status
    {
        public int DownloadingException { get; set; }
        public int DownloadingTotal { get; set; }
        public int Downloading { get; set; }
        public int UploadingException { get; set; }
        public int UploadingTotal { get; set; }
        public int Uploading { get; set; }
        public int LocalTotal { get; set; }
        public int CloudTotal { get; set; }
        public int MatchingFiles { get; set; }
        public string BucketName { get; set; }
        public int NotProcessedFiles { get; set; }

        public override string ToString()
        {
            return String.Format("BucketName {0}, LocalTotal {1}, CloudTotal {2}", BucketName, LocalTotal, CloudTotal);
            //return String.Format("BucketName {0}, LocalTotal {1}, CloudTotal {2}, MatchingFiles {3}, NotProcessedFiles {4}, Uploading {5}, UploadingTotal {6}, UploadingException {7}, Downloading {8}, DownloadingTotal {9}, DownloadingException {10}", BucketName, LocalTotal, CloudTotal, MatchingFiles, NotProcessedFiles, Uploading, UploadingTotal, UploadingException, Downloading, DownloadingTotal, DownloadingException);
        }
    }

    public delegate void S3StausChangedEventHandler(S3Status status);

    public class  S3FolderSynchronizer
    {
        private static readonly List<S3EndPoint> S3EndPoints = new List<S3EndPoint> {
            new S3EndPoint {Region="US Standard",  EndPoint = "s3.amazonaws.com", Location = ""},
            new S3EndPoint {Region="US West (Oregon)",  EndPoint = "s3-us-west-2.amazonaws.com", Location = "us-west-2"},
            new S3EndPoint {Region="US West (Northern California)",  EndPoint = "s3-us-west-1.amazonaws.com", Location = "us-west-1"},
            new S3EndPoint {Region="EU (Ireland)",  EndPoint = "s3-eu-west-1.amazonaws.com", Location = "EU"},
            new S3EndPoint {Region="Asia Pacific (Singapore)",  EndPoint = "s3-ap-southeast-1.amazonaws.com", Location = "ap-southeast-1"},
            new S3EndPoint {Region="Asia Pacific (Sydney)",  EndPoint = "s3-ap-southeast-2.amazonaws.com", Location = "ap-southeast-2"},
            new S3EndPoint {Region="Asia Pacific (Tokyo)",  EndPoint = "s3-ap-northeast-1.amazonaws.com", Location = "ap-northeast-1"},
            new S3EndPoint {Region="South America (Sao Paulo)",  EndPoint = "s3-sa-east-1.amazonaws.com", Location = "sa-east-1"},
        };
        private static readonly Dictionary<string, List<string>> BucketDictionary = new Dictionary<string, List<string>>();

        enum ProcessAction : byte { Empty = 0, Upload, Download, UploadingDone, UploadingException, Match, DownloadingDone, DownloadingException }
        private class ProcessItem
        {
            public string LocalPath { get; set; }
            public string S3Path { get; set; }
            public long S3Size { get; set; }
            public long LocalSize { get; set; }
            public ProcessAction Action { get; set; }
        }

        private readonly string _accessKey;
        private readonly string _secretAccessKey;
        private readonly string _serviceUrl;
        private readonly string _bucketName;
        private int _pos;
        private CancellationTokenSource _tokenSource;

        public  S3FolderSynchronizer(string accessKey, string secretAccessKey, string endPointName, string bucketName)
        {
            _accessKey = accessKey;
            _secretAccessKey = secretAccessKey;
            //_serviceUrl = endPointName;
            foreach (var endPoint in S3EndPoints)
                if (endPoint.Region == endPointName) {
                    _serviceUrl = endPoint.EndPoint;
                    break;
                }
            _bucketName = bucketName;
        }

        private AmazonS3 CreateAmazonS3Client()
        {
            var config = new AmazonS3Config { ServiceURL = _serviceUrl, CommunicationProtocol = Protocol.HTTPS };
            return Amazon.AWSClientFactory.CreateAmazonS3Client(_accessKey, _secretAccessKey, config);
        }

        public void Sync(string localRootFolder, string remoteFolder, S3StausChangedEventHandler eventHandler)
        {
            var files = GetFiles(localRootFolder, remoteFolder);
            var listRequest = new ListObjectsRequest().WithBucketName(_bucketName).WithPrefix(remoteFolder);
            var list = new Dictionary<string, S3Object>();
            UpdateStatus(eventHandler, files, list);
            bool isTruncated = false;
            bool hadException = false;
            using (var client = CreateAmazonS3Client()) {
                do {
                    try {
                        var listResponse = client.ListObjects(listRequest);
                        isTruncated = listResponse.IsTruncated;
                        var currentList = listResponse.S3Objects.Where(obj => obj.Size != 0).ToList();
                        foreach (var item in currentList) {

                            list.Add(item.Key, item);
                            if (files.ContainsKey(item.Key)) {
                                var file = files[item.Key];
                                if (file.LocalSize != item.Size) {
                                    file.S3Path = item.Key;
                                    file.S3Size = item.Size;
                                    file.Action = (file.LocalSize > file.S3Size) ? ProcessAction.Upload : ProcessAction.Download;
                                } else file.Action = ProcessAction.Match;
                            } else {
                                var file = new ProcessItem {S3Path = item.Key, S3Size = item.Size, Action = ProcessAction.Download};
                                files.Add(item.Key, file);
                            }
                        }
                        UpdateStatus(eventHandler, files, list);
                        listRequest.Marker = listResponse.NextMarker;
                    } catch (Exception) {
                        hadException = true;
                    }
                } while (isTruncated);
            }
            if (hadException) return;
            foreach (var file in files) {
                if (!list.ContainsKey(file.Key)) {
                    file.Value.S3Path = file.Key;
                    file.Value.Action = ProcessAction.Upload;
                }
            }
            UpdateStatus(eventHandler, files, list);
            //Task.Factory.StartNew(() => {
                foreach (var file in files) {
                    if (file.Value.Action == ProcessAction.Upload) {
                        Upload(file.Value);
                        UpdateStatus(eventHandler, files, list);
                    }
                }
            //});
            //Task.Factory.StartNew(() => {
                foreach (var file in files) {
                    if (file.Value.Action == ProcessAction.Download) {
                        Download(localRootFolder, file.Value);
                        UpdateStatus(eventHandler, files, list);
                    }
                }
            //});
        }

        private void UpdateStatus(S3StausChangedEventHandler eventHandler, Dictionary<string, ProcessItem> files, Dictionary<string, S3Object> list)
        {
            if (eventHandler == null) return;
            var uploading = 0;
            var uploadingTotal = 0;
            var uploadingException = 0;
            var downloading = 0;
            var downloadingTotal = 0;
            var downloadingException = 0;
            var matchingFiles = 0;
            var notProcessedFiles = 0;

            foreach (var file in files) {
                switch (file.Value.Action) {
                    case ProcessAction.Match:
                        matchingFiles++;
                        break;
                    case ProcessAction.Upload:
                        uploadingTotal++;
                        break;
                    case ProcessAction.Download:
                        downloadingTotal++;
                        break;
                    case ProcessAction.UploadingDone:
                        uploadingTotal++;
                        uploading++;
                        break;
                    case ProcessAction.UploadingException:
                        uploading++;
                        uploadingTotal++;
                        uploadingException++;
                        break;
                    case ProcessAction.DownloadingDone:
                        downloadingTotal++;
                        downloading++;
                        break;
                    case ProcessAction.DownloadingException:
                        downloading++;
                        downloadingTotal++;
                        downloadingException++;
                        break;
                    case ProcessAction.Empty:
                        notProcessedFiles++;
                        break;
                }
            }
            var status = new S3Status {
                BucketName = _bucketName, CloudTotal = list.Count + uploading - uploadingException, LocalTotal = files.Count + downloading - downloadingException, 
                Uploading = uploading, UploadingTotal = uploadingTotal, UploadingException = uploadingException, 
                Downloading = downloading, DownloadingTotal = downloadingTotal, DownloadingException = downloadingException, 
                MatchingFiles = matchingFiles, NotProcessedFiles = notProcessedFiles};
            eventHandler(status);
        }

        public List<string> GetBuckets()
        {
            var key = String.Format("{0}|{1}|{2}", _accessKey, _secretAccessKey, _serviceUrl);
            if (BucketDictionary.ContainsKey(key)) 
                return BucketDictionary[key];
            var list = new List<string>();
            using (var client = CreateAmazonS3Client()) {
                if (client == null) return list;
                var response = client.ListBuckets();
                foreach(var item in response.Buckets)
                    list.Add(item.BucketName);
                BucketDictionary.Add(key, list);
                return list;
            }
        }

        private void Upload(ProcessItem file)
        {
            using (var client = CreateAmazonS3Client()) {
                if (client == null) {
                    file.Action = ProcessAction.UploadingException;
                    return;
                }
                var localpath = HttpUtility.UrlEncode(file.LocalPath.Substring(_pos));
                var metadata = new NameValueCollection {
                    {"LocalSize", file.LocalSize.ToString(CultureInfo.InvariantCulture)}, 
                    {"LocalPath", localpath}, 
                };
                var request = new PutObjectRequest();
                try {
                    using (var fileStream = new FileStream(file.LocalPath, FileMode.Open)) {
                        request.WithBucketName(_bucketName)
                                .WithCannedACL(S3CannedACL.PublicRead).WithMetaData(metadata)
                                .WithKey(file.S3Path).InputStream = fileStream;
                        client.PutObject(request);
                        file.Action = ProcessAction.UploadingDone;
                    }
                } catch (Exception) {
                    file.Action = ProcessAction.UploadingException;
                }
            }
        }

        private void Download(string localFolderName, ProcessItem file)
        {
            using (var client = CreateAmazonS3Client()) {
                file.Action = ProcessAction.DownloadingException;
                if (client == null) return;
                try {
                    var request = new GetObjectRequest();
                    request.WithBucketName(_bucketName).WithKey(file.S3Path);
                    using (var response = client.GetObject(request)) {
                        var fileName = "";
                        if (response.Metadata != null && response.Metadata.Count > 0) {
                            var localPath = response.Metadata.Get("x-amz-meta-localpath");
                            if (String.IsNullOrWhiteSpace(localPath)) return;
                            localPath = HttpUtility.UrlDecode(localPath);
                            if (localPath != null) {
                                var localPathArr = localPath.Split('\\');
                                var rootDirectory = localFolderName;
                                for (var i = 1; i < localPathArr.Length - 1; i++) {
                                    rootDirectory = Path.Combine(rootDirectory, localPathArr[i]);
                                    if(!Directory.Exists(rootDirectory))
                                        Directory.CreateDirectory(rootDirectory);
                                }
                                fileName = Path.Combine(rootDirectory, localPathArr[localPathArr.Length - 1]);
                            }
                        }
                        if (String.IsNullOrWhiteSpace(fileName))  return;
                        using (var fileStream = new FileStream(fileName, FileMode.Create)) {
                            using (var stream = response.ResponseStream) {
                                var data = new byte[32768];
                                int bytesRead;
                                do {
                                    bytesRead = stream.Read(data, 0, data.Length);
                                    fileStream.Write(data, 0, bytesRead);
                                } while (bytesRead > 0);
                                fileStream.Flush();
                                file.Action = ProcessAction.DownloadingDone;
                            }
                        }
                    }
                } catch (Exception) {
                }
            }
        }

        private Dictionary<string, ProcessItem> GetFiles(string folderName, string rootFolder)
        {
            var dict = new Dictionary<string, ProcessItem>();
            _pos = folderName.IndexOf(rootFolder, StringComparison.Ordinal);
            foreach(var folder in new[] {folderName}) 
                foreach (var file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)) {
                    var filename = file.Substring(_pos).Replace('\\', '/');
                    var size = (new FileInfo(file)).Length;
                    dict.Add(filename, new ProcessItem {LocalPath = file, Action = ProcessAction.Empty, LocalSize = size});
                }
            return dict;
        }

    }

}
