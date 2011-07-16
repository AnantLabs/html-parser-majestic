namespace HtmlParserMajestic.Wrappers
{
    /// <summary>
    /// Type of parsed HTML chunk (token).
    /// </summary>
    public enum HtmlChunkType
    {
        /// <summary>
        /// Text data from HTML
        /// </summary>
        Text,

        /// <summary>
        /// Open tag, possibly with attributes
        /// </summary>
        Tag,

        /// <summary>
        /// TODO: summary
        /// </summary>
        Comment,

        /// <summary>
        /// TODO: summary
        /// </summary>
        Script,
    };
}