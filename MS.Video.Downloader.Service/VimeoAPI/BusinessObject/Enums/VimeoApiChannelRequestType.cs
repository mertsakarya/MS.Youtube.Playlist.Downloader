namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums
{
    /// <summary>
    /// The feed types supported by the Vimeo simple API.
    /// </summary>
    /// <see cref="http://www.vimeo.com/api/simple-api"/>
    public enum VimeoApiChannelRequestType
    {
        /// <summary>
        /// Videos added to that channel
        /// </summary>
        videos,
        /// <summary>
        /// Channel info for the specified channel
        /// </summary>
        info
    }
}
