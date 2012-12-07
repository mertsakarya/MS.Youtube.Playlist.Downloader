using System;
using System.Text;

namespace MS.Video.Downloader.Service.VimeoAPI.Common
{
    /// <summary>
    /// Global constants for the solution
    /// </summary>
    public class GlobalConstants
    {
        #region VimeoAPI
        /// <summary>
        /// Constants required to use the Vimeo API
        /// </summary>
        public class VimeoAPI
        {
            /// <summary>
            /// the vimeo api consumer key - your consumer key
            /// </summary>
            public const string ConsumerKey = "your consumer key";
            /// <summary>
            /// the vimeo api consumer secret - your consumer secret
            /// </summary>
            public const string ConsumerSecret = "your consumer secret";
            /// <summary>
            /// http://vimeo.com/api/rest/v2/
            /// </summary>
            public const string StandardAdvancedApiUrl = "http://vimeo.com/api/rest/v2";
            /// <summary>
            /// StandardAdvancedApiUrl + ?method=vimeo.videos.search&query=
            /// </summary>
            public const string SearchUrl = StandardAdvancedApiUrl 
                + "?method=vimeo.videos.search&per_page=15&query=";
            /// <summary>
            /// StandardAdvancedApiUrl + ?method=vimeo.videos.getThumbnailUrls&video_id=
            /// </summary>
            public const string GetVideoThumbnailsUrl = StandardAdvancedApiUrl 
                + "?method=vimeo.videos.getThumbnailUrls&video_id=";
            /// <summary>
            /// create all required Oauth parameters
            /// </summary>
            public static string OAuthParameters = BuildOAuthParameters();

            /// <summary>
            /// http://vimeo.com/api/oembed.xml?url=http%3A//vimeo.com/
            /// </summary>
            public const string OembedRequestUrlFormat = 
                "http://vimeo.com/api/oembed.xml?url=http%3A//vimeo.com/";
            /// <summary>
            /// http://vimeo.com/api/v2/
            /// </summary>
            /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string BaseVimeoUrl = "http://vimeo.com/api/v2/";
            /// <summary>
            /// http://vimeo.com/api/v2/username/request.output
            /// </summary>
            /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string UserRequestUrlFormat = BaseVimeoUrl + "{0}/{1}.{2}";
            /// <summary>
            /// http://vimeo.com/api/v2/video/video_id.output
            /// </summary>
            /// /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string VideoRequestUrlFormat = BaseVimeoUrl + "video/{0}.{1}";
            /// <summary>
            /// http://vimeo.com/api/v2/activity/username/request.output
            /// </summary>
            /// /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string ActivityRequestUrlFormat = BaseVimeoUrl 
                + "activity/{0}/{1}.{2}";
            /// <summary>
            /// http://vimeo.com/api/v2/group/groupname/request.output
            /// </summary>
            /// /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string GroupRequestUrlFormat = BaseVimeoUrl + "group/{0}/{1}.{2}";
            /// <summary>
            /// http://vimeo.com/api/v2/channel/channelname/request.output
            /// </summary>
            /// /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string ChannelRequestUrlFormat = BaseVimeoUrl 
                + "channel/{0}/{1}.{2}";
            /// <summary>
            /// http://vimeo.com/api/v2/album/album_id/request.output
            /// </summary>
            /// /// <see cref="http://www.vimeo.com/api/docs/simple-api"/>
            public const string AlbumRequestUrlFormat = BaseVimeoUrl + "album/{0}/{1}.{2}";
            /// <summary>
            /// Vimeo player width
            /// </summary>
            public const string VimeoPlayerWidth = "550";
            /// <summary>
            /// Vimeo player height
            /// </summary>
            public const string VimeoPlayerHeight = "425";

            /// <summary>
            /// Builds the Oauth parameters.
            /// </summary>
            /// <returns></returns>
            protected static string BuildOAuthParameters()
            {
                var sb = new StringBuilder();

                sb.Append("&oauth_consumer_key=");
                sb.Append(ConsumerKey);
                sb.Append("&oauth_nonce=");
                sb.Append(Guid.NewGuid());
                sb.Append("&oauth_signature_method=HMAC-SHA1");
                sb.Append("&oauth_timestamp=");

                TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));

                sb.Append(ts.TotalSeconds);
                sb.Append("&oauth_version=1.0");

                return sb.ToString();
            }
        }
        #endregion

        #region Links
        /// <summary>
        /// Page links
        /// </summary>
        public class Links
        {
            /// <summary>
            /// Default.aspx
            /// </summary>
            public const string Default = "/Default.aspx";
            /// <summary>
            /// ViewVideo.aspx?vid=
            /// </summary>
            public const string ViewVideo = "/ViewVideo.aspx?" + Querystring.Video + "=";
        }
        #endregion

        #region Querystring
        /// <summary>
        /// Querystring identifiers
        /// </summary>
        public class Querystring
        {
            /// <summary>
            /// vid
            /// </summary>
            public const string Video = "vid";
        }
        #endregion

        #region ErrorMessages
        /// <summary>
        /// Querystring identifiers
        /// </summary>
        public class ErrorMessages
        {
            /// <summary>
            /// Sorry, Vimeo's search service is temporarily unavailable. 
            /// Please try again in a few minutes.
            /// </summary>
            public const string SearchServiceUnavailable 
                = "Sorry, Vimeo's search service is temporarily unavailable. "
                +"Please try again in a few minutes.";
            /// <summary>
            /// Sorry, there was an error with your search. If the problem persists please #
            /// contact us and let us know about the issue, thanks.
            /// </summary>
            public const string SearchError 
                = "Sorry, there was an error with your search. If the problem persists please"
                + " contact us and let us know about the issue, thanks.";
            /// <summary>
            /// Sorry, your query returned no results
            /// </summary>
            public const string NoSearchResults = "Sorry, your query returned no results";
        }
        #endregion
    }
}