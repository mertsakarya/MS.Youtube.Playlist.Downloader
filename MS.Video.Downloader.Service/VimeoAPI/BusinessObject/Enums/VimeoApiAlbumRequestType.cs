namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums
{
    /// <summary>
    /// The feed types supported by the Vimeo simple API.
    /// </summary>
    /// <see cref="http://www.vimeo.com/api/simple-api"/>
    public enum VimeoApiAlbumRequestType
    {
        /// <summary>
        /// Videos added to that album
        /// </summary>
        videos,
        /// <summary>
        /// Album info for the specified album
        /// </summary>
        info
    }
}
