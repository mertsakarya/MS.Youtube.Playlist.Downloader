namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums
{
    /// <summary>
    /// The feed types supported by the Vimeo simple API.
    /// </summary>
    /// <see cref="http://www.vimeo.com/api/simple-api"/>
    public enum VimeoApiUserRequestType
    {
        /// <summary>
        /// User info for the specified user
        /// </summary>
        info,
        /// <summary>
        /// Videos created by user
        /// </summary>
        videos,
        /// <summary>
        /// Videos the user likes
        /// </summary>
        likes,
        /// <summary>
        /// Videos that the user appears in
        /// </summary>
        appears_in,
        /// <summary>
        /// Videos that the user appears in and created
        /// </summary>
        all_videos,
        /// <summary>
        /// Videos the user is subscribed to
        /// </summary>
        subscriptions,
        /// <summary>
        /// Albums the user has created
        /// </summary>
        albums,
        /// <summary>
        /// Channels the user has created and subscribed to
        /// </summary>
        channels,
        /// <summary>
        /// Groups the user has created and joined
        /// </summary>
        groups,
        /// <summary>
        /// Videos that the user's contacts created
        /// </summary>
        contacts_videos,
        /// <summary>
        /// Videos that the user's contacts like
        /// </summary>
        contacts_like
    }
}
