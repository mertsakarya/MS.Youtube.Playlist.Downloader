using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using System.Net;
using System.Xml.Linq;
#if WINDOWS
using System.Web;
#endif
#if VFW
using VFW2;
using System.Windows.Forms;
#endif
using System.IO;
using System.Diagnostics;

namespace Vimeo.API
{
#if WINDOWS
    public partial class VimeoClient
    {
        /// <summary>
        /// Calculates the number of chunks for a file. Default chunk size is 1MB.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="chunk_size">Size of each chunk in bytes</param>
        /// <returns></returns>
        public int GetChunksCount(string path, int chunk_size = 1048576)
        {
            var fi = new FileInfo(path);
            return (int)Math.Ceiling((float)fi.Length / (float)chunk_size);
        }

        /// <summary>
        /// Calculates the number of chunks for a file based on the size of the file in bytes. Default chunk size is 1MB.
        /// </summary>
        /// <param name="fileSize">Size of the file in bytes.</param>
        /// <param name="chunk_size">Size of each chunk in bytes</param>
        /// <returns></returns>
        public int GetChunksCount(long fileSize, int chunk_size = 1048576)
        {
            if (chunk_size < 0) return 1;
            if (chunk_size == 0) return 0;
            return (int)Math.Ceiling((double)fileSize / (double)chunk_size);
        }

        /// <summary>
        /// Calculates the number of chunks for a file based on the chunk ID. Default chunk size is 1MB.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index">Index of chunk or chunk_id starting with 0.</param>
        /// <param name="size">Maximum size of chunk in bytes</param>
        /// <returns></returns>
        public int GetChunkSize(string path, int index, int size = 1048576)
        {
            return GetChunkSize(new FileInfo(path).Length, index, size);
        }

        /// <summary>
        /// Calculates the number of chunks for a file based on the size of the file in bytes and chunk ID. Default chunk size is 1MB.
        /// </summary>
        /// <param name="fileSize">Size of video file in bytes.</param>
        /// <param name="index">Index of chunk or chunk_id starting with 0.</param>
        /// <param name="size">Maximum size of chunk in bytes</param>
        /// <returns></returns>
        public int GetChunkSize(long fileSize, int index, int size = 1048576)
        {
            var startbyte = index * size;
            if (size > 0)
            {
                if (size + startbyte > fileSize)
                {
                    size = (int)(fileSize - startbyte);
                }
                return size;
            }
            else
            {
                return (int)fileSize;// (int)(fileSize - (long)startbyte);
            }
        }

#if VFW
        public List<int> GetMissingChunks(string TicketId, string Path, int ChunkSize = 1048576)
        {
            var fi = new FileInfo(Path);
            var count = GetChunksCount(fi.Length, ChunkSize);
            var c = vimeo_videos_upload_verifyChunks(TicketId);
            List<int> chunks = new List<int>();
            if (ChunkSize < 0)
            {
                if (c == null || c.Items.Count == 0) return chunks;
                chunks.Add(0);
                return chunks;
            }
            for (int i = 0; i < count; i++)
            {
                chunks.Add(i);
            }
            if (c == null)
            {   
                return chunks;
            }
            foreach (var item in c.Items)
            {
                if (item.size == GetChunkSize(fi.Length, item.id, ChunkSize))
                    chunks.Remove(item.id);
            }
            return chunks;
        }
#endif

        /// <summary>
        /// Uploads a video file and initiates transcoding. The quickest way to upload a video.
        /// Use UploadInChunks for large files. otherwise you'll get out of memory errors.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>video_id if successful, null on error</returns>
        public string Upload(string path)
        {
            Debug.WriteLine("Upload(" + path + ") called. getting a ticket...", "VimeoClient");
            var t = vimeo_videos_upload_getTicket();
            if (t == null)
            {
                Debug.WriteLine("Error in getTicket. Cancelling upload.", "VimeoClient");
                return null;
            }
            
            Debug.WriteLine("Ticket acquired. Posting video...", "VimeoClient");
            PostVideo(t, path);

            Debug.WriteLine("Completing upload...", "VimeoClient");
            var result = vimeo_videos_upload_complete(new FileInfo(path).Name, t.id);

            Debug.WriteLine("Upload completed. Result: " + result, "VimeoClient");
            return result;
        }

        /// <summary>
        /// Resumes an upload, uploads in chunks, and stops after a limited number of chunks.
        /// Verifies chunks and calls complete() if there are no missing chunks left.
        /// </summary>
        /// <param name="path">Path of file</param>
        /// <param name="t">Upload Ticket</param>
        /// <param name="chunk_size">Size of chunks in bytes, default is 1MB</param>
        /// <param name="max_chunks">Maximum number of chunks that can be uploaded in one call. Negative indicates there's no limit</param>
        /// <returns>video_id if upload is successful, empty string if upload limit is reached, null on error</returns>
        public string ResumeUploadInChunks(string path, Ticket t, int chunk_size = 1048576, int max_chunks = -1)
        {
            return ResumeUploadInChunks(path, t, chunk_size, max_chunks, false);
        }

        string ResumeUploadInChunks(string path, Ticket t, int chunk_size, int max_chunks, bool firstTime)
        {
            const int maxFailedAttempts = 5;

            if (!firstTime)
            {
                // Check the ticket
                Debug.WriteLine("ResumeUploadInChunks(" + path + ") Checking ticket...", "VimeoClient");
                var ticket = vimeo_videos_upload_checkTicket(t.id);
                if (ticket == null || ticket.id != t.id)
                {
                    Debug.WriteLine("Error in checking ticket. aborting.", "VimeoClient");
                    return null;
                }
                t = ticket;
            }

            //Load the file, calculate the number of chunks
            var file = new FileInfo(path);
            int chunksCount = GetChunksCount(file.Length, chunk_size);
            Debug.WriteLine("Will upload in " + chunksCount + " chunks", "VimeoClient");

            //Queue chunks for upload
            List<int> missingChunks = new List<int>(chunksCount);
            for (int i = 0; i < chunksCount; i++) missingChunks.Add(i);
            
            int failedAttempts = 0;
            int counter = 0;
            while (failedAttempts <= maxFailedAttempts)
            {
                if (firstTime)
                {
                    firstTime = false;
                }
                else
                {
                    //Verify and remove the successfully uploaded chunks
                    Debug.WriteLine("Verifying chunks...", "VimeoClient");
                    var verify = vimeo_videos_upload_verifyChunks(t.id);
                    Debug.WriteLine(verify.Items.Count.ToString() + "/" + chunksCount + " chunks uploaded successfully.", "VimeoClient");

                    missingChunks.Clear();
                    for (int i = 0; i < chunksCount; i++) missingChunks.Add(i);
                    foreach (var item in verify.Items)
                    {
                        if (missingChunks.Contains(item.id) && item.size == GetChunkSize(file.Length, item.id, chunk_size))
                            missingChunks.Remove(item.id);
                    }
                }

                //If there are no chunks left or the limit is reached stop.
                if (missingChunks.Count == 0 || (max_chunks > 0 && counter >= max_chunks)) 
                    break;

                //Post chunks
                while (missingChunks.Count > 0)
                {
                    //If there are no chunks left or the limit is reached stop.
                    if (missingChunks.Count == 0 || (max_chunks > 0 && counter >= max_chunks)) 
                        break;

                    if (failedAttempts > maxFailedAttempts) break;

                    int chunkId = missingChunks[0];
                    missingChunks.RemoveAt(0);

                    Debug.WriteLine("Posting chunk " + chunkId + ". " + missingChunks.Count + " chunks left.", "VimeoClient");
                    try
                    {
                        counter++;
                        PostVideo(t, chunkId, path, chunk_size);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e, "VimeoClient");
                        failedAttempts++;
                    }
                }
            }

            if (missingChunks.Count == 0)
            {
                //All chunks are uploaded
                return vimeo_videos_upload_complete(file.Name, t.id);
            }

            if ((max_chunks > 0 && counter >= max_chunks))
            {
                //Max limit is reached
                return string.Empty;
            }

            //Error
            return null;
        }

        /// <summary>
        /// Uploads a video in chunks. Use for large videos. 
        /// </summary>
        /// <param name="path">Path to video file</param>
        /// <param name="chunk_size">Size of each chunk in bytes, default is 1MB</param>
        /// <returns>video_id of uploaded file, null on error</returns>
        public string UploadInChunks(string path, int chunk_size = 1048576)
        {
            var t = new Ticket();
            return UploadInChunks(path, out t, chunk_size);
        }

        /// <summary>
        /// Uploads a video in chunks. Use for large videos.
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <param name="ticket">A ticket that can be used to resume the process.</param>
        /// <param name="chunk_size">Size of each chunk in bytes, default is 1MB</param>
        /// <param name="max_chunks">Maximum number of chunks that can be uploaded in this call. Negative means there's no limit.</param>
        /// <returns>video_id of uploaded file, null on error</returns>
        public string UploadInChunks(string path, out Ticket ticket, int chunk_size = 1048576, int max_chunks=-1)
        {
            Debug.WriteLine("Upload(" + path + ") called. getting a ticket...", "VimeoClient");
            ticket = vimeo_videos_upload_getTicket();

            if (ticket == null)
            {
                Debug.WriteLine("Error in getTicket. Cancelling upload.", "VimeoClient");
                return null;
            }

            return ResumeUploadInChunks(path, ticket, chunk_size, max_chunks, true);
        }

        /// <summary>
        /// Uploads a video file.
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string PostVideo(Ticket ticket, string path)
        {
            return PostVideo(ticket, 0, path, 0, -1);
        }

        /// <summary>
        /// Uploads a video chunk. Default chunk size is 1MB.
        /// </summary>
        /// <param name="ticket">Upload ticket</param>
        /// <param name="index">Chunk ID</param>
        /// <param name="path"></param>
        /// <param name="chunk_size">Size of chunks in bytes</param>
        /// <returns></returns>
        public string PostVideo(Ticket ticket, int index, string path, int chunk_size = 1048576)
        {
            return PostVideo(ticket, index, path, index * chunk_size, chunk_size);
        }

        /// <summary>
        /// Uploads a part of a file.
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="chunk_id">The parameter to send to vimeo.</param>
        /// <param name="path">File path</param>
        /// <param name="startbyte">Starting byte</param>
        /// <param name="psize">Bytes to upload. Use null to upload all bytes starting from startbyte</param>
        /// <returns></returns>
        public string PostVideo(Ticket ticket, int chunk_id, string path, long startbyte, int? psize)
        {
            int size = psize.HasValue ? psize.Value : -1;
            var fi = new FileInfo(path);
            FileStream sr = fi.OpenRead();
            sr.Seek(startbyte, SeekOrigin.Begin);
            byte[] data;
            if (size > 0)
            {
                if (size + startbyte > fi.Length)
                {
                    size = (int)(fi.Length - startbyte);
                }
                data = new byte[size];
                sr.Read(data, 0, size);
            }
            else
            {
                data = new byte[fi.Length - startbyte];
                sr.Read(data, 0, (int)(fi.Length - startbyte));
            }
            sr.Close();
            return PostVideo(ticket, chunk_id, Path.GetFileName(path), data);
        }

        public bool UseProxyForPost = true;

        /// <summary>
        /// Posts an array of bytes to vimeo. This is the core of the POST process.
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="chunk_id"></param>
        /// <param name="file_name"></param>
        /// <param name="file_data"></param>
        /// <returns></returns>
        public string PostVideo(Ticket ticket, int chunk_id, string file_name, byte[] file_data)
        {
            Debug.WriteLine("PostVideo called with chunk_id=" + chunk_id + " file_name=" + file_name + " length="+file_data.Length+"B", "VimeoClient");
            
            Dictionary<string, string> parameters = new Dictionary<string, string> {
                /*{"ticket_id", ticket.id},*/ {"chunk_id", chunk_id.ToString()}};
            Dictionary<string, string> oauth_parameters;
            var url = GetRequestUrl(ticket.endpoint, null, parameters, out oauth_parameters, "POST");
            
            var endpoint = ticket.endpoint + "&chunk_id=" + chunk_id;
            foreach (var item in parameters)
            {
                oauth_parameters[item.Key] = item.Value;
            }
            oauth_parameters.Add("file_data", "");
            HttpWebRequest req = WebRequest.Create(endpoint) as HttpWebRequest;
            if (Proxy != null && UseProxyForPost) req.Proxy = Proxy;

            // get a boundary string - used to separate parts of the form data
            string boundary = String.Format("----------{0}", Guid.NewGuid());

            req.ContentType = String.Format("multipart/form-data; boundary={0}", boundary);
            req.Method = "POST";
            req.KeepAlive = true;
            req.Credentials = System.Net.CredentialCache.DefaultCredentials;

            StringBuilder header = new StringBuilder();
            StringBuilder header2 = new StringBuilder();
            
            foreach (var item in oauth_parameters)
            {
                header.AppendFormat("--{0}\r\n", boundary);
                if (item.Key == "file_data")
                {
                    header.AppendFormat("Content-Disposition: form-data; name=\"file_data\"; filename=\"{0}\"\r\n",
                        file_name);
                    header.Append("Content-Type: application/octet-stream\r\n\r\n");
                    header2 = header;
                    header = new StringBuilder();
                    header.Append("\r\n");
                }
                else
                {
                    header.AppendFormat("Content-Disposition: form-data; name=\"" + item.Key + "\"\r\n\r\n{0}\r\n",
                        item.Value);
                }
            }
            
            // get the header as bytes
            byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
            byte[] header2Bytes = Encoding.UTF8.GetBytes(header2.ToString());
            
            // get the footer bytes
            byte[] footerBytes = Encoding.UTF8.GetBytes(String.Format("\r\n--{0}--", boundary));

            // get the complete set of bytes
            byte[] data = header2Bytes
            .Concat(file_data)
            //.Concat(headerBytes)
            .Concat(footerBytes)
            .ToArray();

            // set the content length
            req.ContentLength = data.Length;

            var t = DateTime.Now.TimeOfDay;
            Debug.WriteLine("Uploading stream...", "VimeoClient");
            // write the bytes to the request stream
            using (Stream s = req.GetRequestStream())
            {
                s.Write(data, 0, data.Length);
            }
            
            // get the response
            HttpWebResponse response = req.GetResponse() as HttpWebResponse;

            Stream responseStream = response.GetResponseStream();
            StreamReader responseReader = new StreamReader(responseStream);
            var result = responseReader.ReadToEnd();

            Debug.WriteLine("Upload completed. [" + data.Length + "B] Total Time: " + (DateTime.Now.TimeOfDay - t).TotalSeconds + "s.", "VimeoClient");
            Debug.WriteLine("PostVideo finished. Result: " + result, "VimeoClient");
            return result;
        }

    }
#endif
}