namespace HtmlParserMajestic.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class HtmlUtils
    {
        /// <summary>
        /// Combines HTMLchunks to HTML code.
        /// </summary>
        public static string Combine(this IEnumerable<HtmlChunk> chunks)
        {
            if (chunks == null) throw new ArgumentNullException("chunks");
            return chunks.Aggregate(chunk => chunk.Html);
        }

        /// <summary>
        /// Returns chunks between the first open tag with the specified name and parameters and corresponding closing one.
        /// These open and close tags themselves are not included.<para/>
        /// Parameters are tuples where first element is parameter name and the second is its value.
        /// </summary>
        public static IEnumerable<HtmlChunk> TagContent(this IEnumerable<HtmlChunk> chunks, string tagName, params Tuple<string, string>[] parameters)
        {
            // skip until we find open tag with specified name and parameters
            chunks = chunks.SkipUntil(c => IsOpenTag(c, tagName, parameters));

            // skip one more item - it's the open tag itself
            chunks = chunks.Skip(1);

            // balance is difference between number of open and close tags with name == tagName
            // initial value is 1 because we've skipped first opening tag and will not count it
            int balance = 1;
            // now find the corresponding closing tag, returning everything before it
            foreach (var chunk in chunks)
            {
                // if tag has specified name, we have to update balance
                if (chunk.Type == HtmlChunkType.Tag && chunk.TagName == tagName)
                {
                    // update balance depending on the tag type
                    switch (chunk.TagType)
                    {
                        case HtmlTagType.Open:
                            balance++;
                            break;
                        case HtmlTagType.Close:
                            balance--;
                            break;
                    }

                    // if balance is zero, then we've found the corresponding closing tag, so break
                    if (balance == 0) yield break;
                }

                // just return the chunk while we haven't found corresponding closing tag
                yield return chunk;
            }
        }

        /// <summary>
        /// Returns true if this chunk represents open tag with specified name and parameters.
        /// </summary>
        public static bool IsOpenTag(this HtmlChunk chunk, string tagName, params Tuple<string, string>[] parameters)
        {
            return
                chunk.Type == HtmlChunkType.Tag &&
                chunk.TagType == HtmlTagType.Open &&
                chunk.TagName == tagName &&
                parameters.All(par => chunk.ParameterMatches(par.Item1, par.Item2));
        }
    }
}