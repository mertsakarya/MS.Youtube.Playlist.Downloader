namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums
{
    /// <summary>
    /// The feed types supported by the Vimeo simple API.
    /// </summary>
    /// <see cref="http://www.vimeo.com/api/simple-api"/>
    public enum VimeoApiGroupRequestType
    {
        /// <summary>
        /// Videos added to that group
        /// </summary>
        videos,
        /// <summary>
        /// Users who have joined the group
        /// </summary>
        users,
        /// <summary>
        /// Group info for the specified group
        /// </summary>
        info
    }
}
