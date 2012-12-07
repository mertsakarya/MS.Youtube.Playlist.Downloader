namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums
{
    /// <summary>
    /// Error codes returned from the Vimeo advanced API.
    /// </summary>
    /// <see cref = "http://vimeo.com/api/docs/advanced-api"/>
    public enum VimeoApiErrorCode
    {
        /// <summary>
        /// The item was either not valid or not provided.
        /// </summary>
        NotFound = 1,
        /// <summary>
        /// The api sig passed was not valid.
        /// </summary>
        InvalidApiSignature = 96,
        /// <summary>
        /// A signature was not passed.
        /// </summary>
        MissingSignature = 97,
        /// <summary>
        /// The login details or auth token passed were invalid.
        /// </summary>
        LoginFailedOrInvalidAuthToken = 98,
        /// <summary>
        /// The API key passed was not valid.
        /// </summary>
        InvalidApiKey = 100,
        /// <summary>
        /// The requested service is temporarily unavailable.
        /// </summary>
        ServiceCurrentlyUnavailable = 105,
        /// <summary>
        /// The requested response format was not found.
        /// </summary>
        FormatNotFound = 111,
        /// <summary>
        /// The requested method was not found.
        /// </summary>
        MethodNotFound = 112,
        /// <summary>
        /// The consumer key passed was not valid.
        /// </summary>
        InvalidConsumerKey = 301,
        /// <summary>
        /// The oauth token passed was either not valid or has expired.
        /// </summary>
        InvalidOrExpiredToken = 302,
        /// <summary>
        /// The oauth signature passed was not valid.
        /// </summary>
        InvalidOAuthSignatureA = 303,
        /// <summary>
        /// The oauth nonce passed has already been used.
        /// </summary>
        InvalidNonce = 304,
        /// <summary>
        /// The oauth signature passed was not valid.
        /// </summary>
        InvalidOAuthSignatureB = 305,
        /// <summary>
        /// We do not support that signature method.
        /// </summary>
        UnsupportedSignatureMethod = 306,
        /// <summary>
        /// A required parameter was missing.
        /// </summary>
        MissingRequiredParameter = 307,
        /// <summary>
        /// An OAuth protocol parameter was duplicated.
        /// </summary>
        DuplicateParameter = 308,
        /// <summary>
        /// The search text cannot be empty.
        /// </summary>
        EmptySearch = 901,
        /// <summary>
        /// Please wait a few minutes before trying again.
        /// </summary>
        RateLimitExceeded = 999
    }
}
