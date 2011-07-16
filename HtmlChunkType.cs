namespace HtmlParserMajestic
{
    /// <summary>
    /// Type of parsed HTML chunk (token), each non-null returned chunk from HtmlParser will have oType set to 
    /// one of these values
    /// </summary>
    internal enum HtmlChunkType
    {
        /// <summary>
        /// Text data from HTML
        /// </summary>
        Text = 0,

        /// <summary>
        /// Open tag, possibly with attributes
        /// </summary>
        OpenTag = 1,

        /// <summary>
        /// Closed tag (it may still have attributes)
        /// </summary>
        CloseTag = 2,

        /// <summary>
        /// Comment tag (<!-- -->)depending on HtmlParser boolean flags you may have:
        /// a) nothing to oHTML variable - for faster performance, call SetRawHTML function in parser
        /// b) data BETWEEN tags (but not including comment tags themselves) - DEFAULT
        /// c) complete RAW HTML representing data between tags and tags themselves (same as you get in a) when
        /// you call SetRawHTML function)
        /// 
        /// Note: this can also be CDATA part of XML document - see sTag value to determine if its proper comment
        /// or CDATA or (in the future) something else
        /// </summary>
        Comment = 3,

        /// <summary>
        /// Script tag (<!-- -->) depending on HtmlParser boolean flags
        /// a) nothing to oHTML variable - for faster performance, call SetRawHTML function in parser
        /// b) data BETWEEN tags (but not including comment tags themselves) - DEFAULT
        /// c) complete RAW HTML representing data between tags and tags themselves (same as you get in a) when
        /// you call SetRawHTML function)
        /// </summary>
        Script = 4,
    };
}