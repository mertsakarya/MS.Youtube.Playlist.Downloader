namespace MS.Video.Downloader.Service.VimeoAPI.BusinessObject.Enums
{
    /// <summary>
    /// The feed types supported by the Vimeo simple API.
    /// </summary>
    /// <see cref="http://www.vimeo.com/api/simple-api"/>
    public enum VimeoApiActivityRequestType
    {
        /// <summary>
        /// Activity by the user
        /// </summary>
        user_did,
        /// <summary>
        /// Activity on the user
        /// </summary>
        happened_to_user,
        /// <summary>
        /// Activity by the user's contacts
        /// </summary>
        contacts_did,
        /// <summary>
        /// Activity on the user's contacts
        /// </summary>
        happened_to_contacts,
        /// <summary>
        /// Activity by everyone
        /// </summary>
        everyone_did
    }
}
