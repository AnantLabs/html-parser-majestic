// -----------------------------------------------------------------------
// <copyright file="HtmlParser.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace HtmlParserMajestic.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class HtmlParser
    {
        /// <summary>
        /// Parses the HTML code into HtmlChunks optionally decoding entities.
        /// It uses deferred execution, so nothing is parsed actually when this method is called.
        /// </summary>
        public static IEnumerable<HtmlChunk> Parse(string htmlCode, bool decodeEntities = true)
        {
            if (htmlCode == null) throw new ArgumentNullException("htmlCode");

            // Note: couldn't find if any exception is thrown during HTML parsing
            using (var parser = new HtmlParserMajestic.HtmlParser(htmlCode) { bDecodeEntities = decodeEntities })
            {
                while (true)
                {
                    using (var chunk = parser.ParseNext())
                    {
                        // end of html reached
                        if (chunk == null) break;

                        yield return new HtmlChunk(chunk);
                    }
                }
            }
        }

        /// <summary>
        /// Parses HTML code from the StreamReader into HtmlChunks optionally decoding entities.
        /// </summary>
        public static IEnumerable<HtmlChunk> Parse(StreamReader reader, bool decodeEntities = true)
        {
            return Parse(reader.ReadToEnd(), decodeEntities);
        }

        /// <summary>
        /// Parses HTML code from the Stream into HtmlChunks optionally decoding entities.
        /// </summary>
        public static IEnumerable<HtmlChunk> Parse(Stream stream, bool decodeEntities = true)
        {
            return Parse(new StreamReader(stream), decodeEntities);
        }
    }
}