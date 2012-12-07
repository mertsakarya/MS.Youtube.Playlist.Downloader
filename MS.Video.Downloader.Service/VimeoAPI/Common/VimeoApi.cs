using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using MS.Video.Downloader.Service.VimeoAPI.BusinessObject;
using MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums;

namespace MS.Video.Downloader.Service.VimeoAPI.Common
{
    public class VimeoAPI
    {
		/// <summary>
		/// Executes an HTTP GET command and retrieves the vimeoApiOutputFormatType.		
		/// </summary>
		/// <param name="url">The URL to perform the GET operation</param>
		/// <param name="userName">The username to use with the request</param>
		/// <param name="password">The password to use with the request</param>
		/// <returns>The response of the request, or null if we got 404 or nothing.</returns>
		protected static string ExecuteGetCommand(string url, string userName, 
            string password)
		{
			using (var wc = new WebClient())
			{
				if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
					wc.Credentials = new NetworkCredential(userName, password);

				try
				{
					using (Stream stream = wc.OpenRead(url))
					{
						using (var reader = new StreamReader(stream))
						{
							return reader.ReadToEnd();
						}
					}
				}
				catch (WebException ex)
				{
					// Handle HTTP 404 errors gracefully and return a null string 
                    // to indicate there is no content.
					if (ex.Response is HttpWebResponse)
						if ((ex.Response as HttpWebResponse).StatusCode 
                            == HttpStatusCode.NotFound)
							return null;

					throw;
				}
			}
		}

        /// <summary>
        /// Builds the OAuth API request URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        protected static string BuildOAuthApiRequestUrl(string url)
        {
            var oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();

            string normalizedUrl;
            string normalizedRequestParameters;

            var uri = new Uri(url);

            string sig = oAuth.GenerateSignature(uri, GlobalConstants.VimeoAPI.ConsumerKey, 
                    GlobalConstants.VimeoAPI.ConsumerSecret, 
                    string.Empty, string.Empty, "GET", timeStamp, nonce, 
                    OAuthSignatureType.HMACSHA1, 
                    out normalizedUrl, out normalizedRequestParameters);
            
            sig = HttpUtility.UrlEncode(sig);

            var sb = new StringBuilder(uri.ToString());
            sb.AppendFormat("&oauth_consumer_key={0}&", GlobalConstants.VimeoAPI.ConsumerKey);
            sb.AppendFormat("oauth_nonce={0}&", nonce);
            sb.AppendFormat("oauth_timestamp={0}&", timeStamp);
            sb.AppendFormat("oauth_signature_method={0}&", "HMAC-SHA1");
            sb.AppendFormat("oauth_version={0}&", "1.0");
            sb.AppendFormat("oauth_signature={0}", sig);
            
            return sb.ToString();
        }

        /// <summary>
        /// XML requests
        /// </summary>
        public class RequestInXML
        {
            /// <summary>
            /// User requests
            /// </summary>
            public class User
            {
                /// <summary>
                /// Gets the albums user has created.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetAlbumsUserHasCreated(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.albums, username);
                }

                /// <summary>
                /// Gets the videos user has appeared in and created.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosUserHasAppearedInAndCreated
                    (string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.all_videos, username);
                }

                /// <summary>
                /// Gets the videos user has appeared in.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosUserHasAppearedIn(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.appears_in, username);
                }

                /// <summary>
                /// Gets the channels user has created and is subscribed to.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetChannelsUserHasCreatedAndIsSubscribedTo
                    (string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.channels, username);
                }

                /// <summary>
                /// Gets the videos users contacts like.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosUsersContactsLike(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.contacts_like, username);
                }

                /// <summary>
                /// Gets the videos users contacts created.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosUsersContactsCreated(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.contacts_videos, 
                        username);
                }

                /// <summary>
                /// Gets the groups user has created and joined.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetGroupsUserHasCreatedAndJoined(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.groups, username);
                }

                /// <summary>
                /// Gets the user info.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetUserInfo(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.info, username);
                }

                /// <summary>
                /// Gets the videos user likes.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosUserLikes(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.likes, username);
                }

                /// <summary>
                /// Gets the videos user is subscribed to.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosUserIsSubscribedTo(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.subscriptions, username);
                }

                /// <summary>
                /// Gets the videos created by user.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetVideosCreatedByUser(string username)
                {
                    return DoUserRequestAsXML(VimeoApiUserRequestType.videos, username);
                }
            }

            /// <summary>
            /// Video requests
            /// </summary>
            public class Video
            {
                /// <summary>
                /// Gets the video.
                /// </summary>
                /// <param name="videoId">The video id.</param>
                /// <returns></returns>
                public static XmlDocument GetVideo(string videoId)
                {
                    return DoVideoRequestAsXML(videoId);
                }
            }

            /// <summary>
            /// Activity requests
            /// </summary>
            public class Activity
            {
                /// <summary>
                /// Gets the activity by the users contacts.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetActivityByTheUsersContacts(string username)
                {
                    return DoActivityRequestAsXML(VimeoApiActivityRequestType.contacts_did, 
                        username);
                }

                /// <summary>
                /// Gets the activity by everyone.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetActivityByEveryone(string username)
                {
                    return DoActivityRequestAsXML(VimeoApiActivityRequestType.everyone_did, 
                        username);
                }

                /// <summary>
                /// Gets the activity on the users contacts.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetActivityOnTheUsersContacts(string username)
                {
                    return DoActivityRequestAsXML(VimeoApiActivityRequestType.happened_to_contacts, username);
                }

                /// <summary>
                /// Gets the activity on the user.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetActivityOnTheUser(string username)
                {
                    return DoActivityRequestAsXML
                        (VimeoApiActivityRequestType.happened_to_user, username);
                }

                /// <summary>
                /// Gets the activity by the user.
                /// </summary>
                /// <returns></returns>
                public static XmlDocument GetActivityByTheUser(string username)
                {
                    return DoActivityRequestAsXML
                        (VimeoApiActivityRequestType.user_did, username);
                }
            }

            /// <summary>
            /// Album requests
            /// </summary>
            public class Album
            {
                /// <summary>
                /// Gets the album info.
                /// </summary>
                /// <param name="albumId">The album id.</param>
                /// <returns></returns>
                public static XmlDocument GetAlbumInfo(string albumId)
                {
                    return DoAlbumRequestAsXML(VimeoApiAlbumRequestType.info, albumId);
                }

                /// <summary>
                /// Gets the videos in album.
                /// </summary>
                /// <param name="albumId">The album id.</param>
                /// <returns></returns>
                public static XmlDocument GetVideosInAlbum(string albumId)
                {
                    return DoAlbumRequestAsXML(VimeoApiAlbumRequestType.videos, albumId);
                }
            }

            /// <summary>
            /// Channel requests
            /// </summary>
            public class Channel
            {
                /// <summary>
                /// Gets the channel info.
                /// </summary>
                /// <param name="channelName">Name of the channel.</param>
                /// <returns></returns>
                public static XmlDocument GetChannelInfo(string channelName)
                {
                    return DoChannelRequestAsXML(VimeoApiChannelRequestType.info, 
                        channelName);
                }

                /// <summary>
                /// Gets the videos in channel.
                /// </summary>
                /// <param name="channelName">Name of the channel.</param>
                /// <returns></returns>
                public static XmlDocument GetVideosInChannel(string channelName)
                {
                    return DoChannelRequestAsXML(VimeoApiChannelRequestType.videos, 
                        channelName);
                }
            }

            /// <summary>
            /// Group requests
            /// </summary>
            public class Group
            {
                /// <summary>
                /// Gets the group info.
                /// </summary>
                /// <param name="groupName">Name of the group.</param>
                /// <returns></returns>
                public static XmlDocument GetGroupInfo(string groupName)
                {
                    return DoGroupRequestAsXML(VimeoApiGroupRequestType.info, groupName);
                }

                /// <summary>
                /// Gets the users in group.
                /// </summary>
                /// <param name="groupName">Name of the group.</param>
                /// <returns></returns>
                public static XmlDocument GetUsersInGroup(string groupName)
                {
                    return DoGroupRequestAsXML(VimeoApiGroupRequestType.users, groupName);
                }

                /// <summary>
                /// Gets the videos in group.
                /// </summary>
                /// <param name="groupName">Name of the group.</param>
                /// <returns></returns>
                public static XmlDocument GetVideosInGroup(string groupName)
                {
                    return DoGroupRequestAsXML(VimeoApiGroupRequestType.videos, groupName);
                }
            }
        }

        #region Methods to perform the requests for each Vimeo request type
        /// <summary>
        /// Does the user request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="vimeoApiUserRequestType">Type of the vimeo API user request.</param>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        protected static string DoUserRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, 
            VimeoApiUserRequestType vimeoApiUserRequestType, string username)
        {
            string url = string.Format(GlobalConstants.VimeoAPI.UserRequestUrlFormat, 
                username, GetVimeoApiUserRequestTypeString(vimeoApiUserRequestType), 
                GetVimeoApiOutputFormatTypeString(vimeoApiOutputFormatType));
            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the user request as XML.
        /// </summary>
        /// <param name="vimeoApiUserRequestType">Type of the vimeo API user request.</param>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        protected static XmlDocument DoUserRequestAsXML
            (VimeoApiUserRequestType vimeoApiUserRequestType, string username)
        {
            string output = DoUserRequest(VimeoApiOutputFormatType.XML, 
                vimeoApiUserRequestType, username);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does the video request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        protected static string DoVideoRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, string videoId)
        {
            string url = string.Format(GlobalConstants.VimeoAPI.VideoRequestUrlFormat, 
                videoId, 
                GetVimeoApiOutputFormatTypeString(vimeoApiOutputFormatType));
            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the video request as XML.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        protected static XmlDocument DoVideoRequestAsXML(string videoId)
        {
            string output = DoVideoRequest(VimeoApiOutputFormatType.XML, videoId);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does the oembed request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        protected static string DoOembedRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, string videoId)
        {
            string url = GlobalConstants.VimeoAPI.OembedRequestUrlFormat + videoId
                         + "?maxwidth=" + GlobalConstants.VimeoAPI.VimeoPlayerWidth
                         + "&maxheight=" + GlobalConstants.VimeoAPI.VimeoPlayerHeight;

            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the oembed request as XML.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        protected static XmlDocument DoOembedRequestAsXML(string videoId)
        {
            string output = DoOembedRequest(VimeoApiOutputFormatType.XML, videoId);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does a search request.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>XmlDocument</returns>
        protected static XmlDocument DoSearchRequest(string query)
        {
            string output = ExecuteGetCommand
                (BuildOAuthApiRequestUrl(GlobalConstants.VimeoAPI.SearchUrl + query), 
                null, null);

            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does a get video thumbnails request.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        protected static XmlDocument DoGetVideoThumbnailsRequest(string videoId)
        {
            string output = ExecuteGetCommand
                (BuildOAuthApiRequestUrl(GlobalConstants.VimeoAPI.GetVideoThumbnailsUrl + videoId), 
                null, null);

            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does the activity request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="vimeoApiActivityRequestType">Type of the vimeo API activity request.</param>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        protected static string DoActivityRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, 
            VimeoApiActivityRequestType vimeoApiActivityRequestType, string username)
        {
            string url = string.Format(GlobalConstants.VimeoAPI.ActivityRequestUrlFormat, 
                username, GetVimeoApiActivityRequestTypeString(vimeoApiActivityRequestType), 
                GetVimeoApiOutputFormatTypeString(vimeoApiOutputFormatType));
            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the activity request as XML.
        /// </summary>
        /// <param name="vimeoApiActivityRequestType">Type of the vimeo API activity request.</param>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        protected static XmlDocument DoActivityRequestAsXML
            (VimeoApiActivityRequestType vimeoApiActivityRequestType, string username)
        {
            string output = DoActivityRequest
                (VimeoApiOutputFormatType.XML, vimeoApiActivityRequestType, username);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does the album request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="vimeoApiAlbumRequestType">Type of the vimeo API album request.</param>
        /// <param name="albumID">The album ID.</param>
        /// <returns></returns>
        protected static string DoAlbumRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, 
            VimeoApiAlbumRequestType vimeoApiAlbumRequestType, string albumID)
        {
            string url = string.Format(GlobalConstants.VimeoAPI.AlbumRequestUrlFormat, albumID,
            GetVimeoApiAlbumRequestTypeString(vimeoApiAlbumRequestType), 
            GetVimeoApiOutputFormatTypeString(vimeoApiOutputFormatType));
            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the album request as XML.
        /// </summary>
        /// <param name="vimeoApiAlbumRequestType">Type of the vimeo API album request.</param>
        /// <param name="albumId">The album id.</param>
        /// <returns></returns>
        protected static XmlDocument DoAlbumRequestAsXML
            (VimeoApiAlbumRequestType vimeoApiAlbumRequestType, string albumId)
        {
            string output = DoAlbumRequest(VimeoApiOutputFormatType.XML, 
                vimeoApiAlbumRequestType, albumId);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does the channel request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="vimeoApiChannelRequestType">Type of the vimeo API channel request.</param>
        /// <param name="channelName">The channel ID.</param>
        /// <returns></returns>
        protected static string DoChannelRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, 
            VimeoApiChannelRequestType vimeoApiChannelRequestType, string channelName)
        {
            string url = string.Format(GlobalConstants.VimeoAPI.ChannelRequestUrlFormat, channelName,
            GetVimeoApiChannelRequestTypeString(vimeoApiChannelRequestType), 
            GetVimeoApiOutputFormatTypeString(vimeoApiOutputFormatType));
            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the channel request as XML.
        /// </summary>
        /// <param name="vimeoApiChannelRequestType">Type of the vimeo API channel request.</param>
        /// <param name="channelName">The channel id.</param>
        /// <returns></returns>
        protected static XmlDocument DoChannelRequestAsXML
            (VimeoApiChannelRequestType vimeoApiChannelRequestType, string channelName)
        {
            string output = DoChannelRequest(VimeoApiOutputFormatType.XML, 
                vimeoApiChannelRequestType, channelName);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }

        /// <summary>
        /// Does the group request.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.</param>
        /// <param name="vimeoApiGroupRequestType">Type of the vimeo API group request.</param>
        /// <param name="groupName">The group ID.</param>
        /// <returns></returns>
        protected static string DoGroupRequest
            (VimeoApiOutputFormatType vimeoApiOutputFormatType, 
            VimeoApiGroupRequestType vimeoApiGroupRequestType, string groupName)
        {
            string url = string.Format(GlobalConstants.VimeoAPI.GroupRequestUrlFormat, groupName,
            GetVimeoApiGroupRequestTypeString(vimeoApiGroupRequestType), 
            GetVimeoApiOutputFormatTypeString(vimeoApiOutputFormatType));
            return ExecuteGetCommand(url, null, null);
        }

        /// <summary>
        /// Does the group request as XML.
        /// </summary>
        /// <param name="vimeoApiGroupRequestType">Type of the vimeo API group request.</param>
        /// <param name="groupName">The group id.</param>
        /// <returns></returns>
        protected static XmlDocument DoGroupRequestAsXML
            (VimeoApiGroupRequestType vimeoApiGroupRequestType, string groupName)
        {
            string output = DoGroupRequest(VimeoApiOutputFormatType.XML, 
                vimeoApiGroupRequestType, groupName);
            if (!string.IsNullOrEmpty(output))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(output);

                return xmlDocument;
            }

            return null;
        }
        #endregion

        #region Methods to convert vimeo enums to strings
        /// <summary>
        /// Gets the vimeo API activity request type string.
        /// </summary>
        /// <param name="vimeoApiActivityRequestType">Type of the vimeo API activity request.
        /// </param>
        /// <returns></returns>
        protected static string GetVimeoApiActivityRequestTypeString
            (VimeoApiActivityRequestType vimeoApiActivityRequestType)
        {
            return vimeoApiActivityRequestType.ToString().ToLower();
        }

        /// <summary>
        /// Gets the vimeo API album request type string.
        /// </summary>
        /// <param name="vimeoApiAlbumRequestType">Type of the vimeo API album request.
        /// </param>
        /// <returns></returns>
        protected static string GetVimeoApiAlbumRequestTypeString
            (VimeoApiAlbumRequestType vimeoApiAlbumRequestType)
        {
            return vimeoApiAlbumRequestType.ToString().ToLower();
        }

        /// <summary>
        /// Gets the vimeo API channel request type string.
        /// </summary>
        /// <param name="vimeoApiChannelRequestType">Type of the vimeo API channel request.
        /// </param>
        /// <returns></returns>
        protected static string GetVimeoApiChannelRequestTypeString
            (VimeoApiChannelRequestType vimeoApiChannelRequestType)
        {
            return vimeoApiChannelRequestType.ToString().ToLower();
        }

        /// <summary>
        /// Gets the vimeo API group request type string.
        /// </summary>
        /// <param name="vimeoApiGroupRequestType">Type of the vimeo API group request.
        /// </param>
        /// <returns></returns>
        protected static string GetVimeoApiGroupRequestTypeString
            (VimeoApiGroupRequestType vimeoApiGroupRequestType)
        {
            return vimeoApiGroupRequestType.ToString().ToLower();
        }

        /// <summary>
        /// Gets the vimeo API user request type string.
        /// </summary>
        /// <param name="vimeoApiUserRequestType">Type of the vimeo API user request.
        /// </param>
        /// <returns></returns>
        protected static string GetVimeoApiUserRequestTypeString
            (VimeoApiUserRequestType vimeoApiUserRequestType)
        {
            return vimeoApiUserRequestType.ToString().ToLower();
        }

        /// <summary>
        /// Gets the vimeo API output format type string.
        /// </summary>
        /// <param name="vimeoApiOutputFormatType">Type of the vimeo API output format.
        /// </param>
        /// <returns></returns>
        protected static string GetVimeoApiOutputFormatTypeString
            (VimeoApiOutputFormatType vimeoApiOutputFormatType)
        {
            return vimeoApiOutputFormatType.ToString().ToLower();
        }
        #endregion

        /// <summary>
        /// Gets the video title from vimeo.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        public static string GetVideoTitleFromVimeo(string videoId)
        {
            var video = GetVideoUsingVideoId(videoId);

            return video == null ? string.Empty : video.title;
        }

        /// <summary>
        /// Gets the embedded player HTML.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        public static string GetEmbeddedPlayerHTML(string videoId)
        {
            var doc = DoOembedRequestAsXML(videoId);

            var serializer = new XmlSerializer(typeof(OEmbed));

            var stringReader = new StringReader(doc.OuterXml);
            var xmlReader = new XmlTextReader(stringReader);

            var oembed = (OEmbed)serializer.Deserialize(xmlReader);

            return oembed.html;
        }

        /// <summary>
        /// Gets the vimeo video id from URL.
        /// </summary>
        /// <param name="videoURL">The video URL.</param>
        /// <returns>The video Id</returns>
        public static string GetVimeoVideoIdFromURL(string videoURL)
        {
            if (videoURL.Contains("/"))
            {
                var splitter = videoURL.Split(new[] {'/'});

                return splitter[splitter.Length - 1];
            }
            
            int v;
            return int.TryParse(videoURL, out v) ? videoURL : string.Empty;
        }

        /// <summary>
        /// Gets the video using video id.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns>VimeoVideo object</returns>
        public static VimeoVideo GetVideoUsingVideoId(string videoId)
        {
            var xmldoc = RequestInXML.Video.GetVideo(videoId);

            var serializer = new XmlSerializer(typeof(VimeoVideoWrapper));

            var stringReader = new StringReader(xmldoc.OuterXml);
            var xmlReader = new XmlTextReader(stringReader);

            var video = (VimeoVideoWrapper)serializer.Deserialize(xmlReader);

            if (video.Videos == null || video.Videos.Length == 0)
                return null;

            return video.Videos[0];
        }

        /// <summary>
        /// Builds the vimeo search response.
        /// </summary>
        /// <param name="xmldoc">The xmldoc.</param>
        /// <returns></returns>
        public static VimeoSearchResponse BuildVimeoSearchResponse(XmlDocument xmldoc)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(VimeoSearchResponse));

                var stringReader = new StringReader(xmldoc.OuterXml);
                var xmlReader = new XmlTextReader(stringReader);

                return (VimeoSearchResponse) serializer.Deserialize(xmlReader);
            }
            catch {}

            return null;
        }

        /// <summary>
        /// Builds the vimeo video thumbnails response.
        /// </summary>
        /// <param name="xmldoc">The xmldoc.</param>
        /// <returns></returns>
        public static VimeoVideoThumbnailsResponse BuildVimeoVideoThumbnailsResponse
            (XmlDocument xmldoc)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(VimeoVideoThumbnailsResponse));

                var stringReader = new StringReader(xmldoc.OuterXml);
                var xmlReader = new XmlTextReader(stringReader);

                return (VimeoVideoThumbnailsResponse)serializer.Deserialize(xmlReader);
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Builds the vimeo error response.
        /// </summary>
        /// <param name="xmldoc">The xmldoc.</param>
        /// <returns></returns>
        public static VimeoErrorResponse BuildVimeoErrorResponse(XmlDocument xmldoc)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(VimeoErrorResponse));

                var stringReader = new StringReader(xmldoc.OuterXml);
                var xmlReader = new XmlTextReader(stringReader);

                return (VimeoErrorResponse)serializer.Deserialize(xmlReader);
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Searches the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static XmlDocument Search(string query)
        {
            try
            {
                return DoSearchRequest(query);
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Gets the video thumbnails.
        /// </summary>
        /// <param name="videoId">The video id.</param>
        /// <returns></returns>
        public static XmlDocument GetVideoThumbnails(string videoId)
        {
            try
            {
                return DoGetVideoThumbnailsRequest(videoId);
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Checks the query worked ok.
        /// </summary>
        /// <param name="xmldoc">The xmldoc.</param>
        /// <returns></returns>
        public static bool QueryIsOk(XmlDocument xmldoc)
        {
            try
            {
                if (xmldoc.DocumentElement != null)
                    return xmldoc.DocumentElement.GetAttribute("stat").Equals("ok");
            }
            catch { }

            return false;
        }
    }
}