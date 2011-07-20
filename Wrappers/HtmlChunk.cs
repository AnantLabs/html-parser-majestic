namespace HtmlParserMajestic.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text.RegularExpressions;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public struct HtmlChunk : IEquatable<HtmlChunk>
    {
        public HtmlChunkType Type { get; private set; }
        public string Html { get; private set; }
        public string Text { get; private set; }
        public string TagName { get; private set; }
        public HtmlTagType TagType { get; private set; }

        private Dictionary<string, string> parameters;


        public HtmlChunk(string tagName, HtmlTagType tagType, Dictionary<string, string> parameters = null)
            : this()
        {
            this.Type = HtmlChunkType.Tag;

            this.TagName = tagName;
            this.TagType = tagType;
            this.parameters = parameters;

            this.Html =
                '<' +
                (tagType == HtmlTagType.Close ? "/" : "") +
                tagName +
                (parameters.IsNull() ? "" : ' ' + parameters.Aggregate(kvp => "{0}=\"{1}\"".FormatWith(kvp.Key, kvp.Value), " ")) +
                (tagType == HtmlTagType.SelfClose ? "/" : "") +
                '>';
        }

        public HtmlChunk(string text)
            : this()
        {
            this.Type = HtmlChunkType.Text;

            this.Text = text;

            this.Html = text;
        }

        /// <summary>
        /// Creates new HtmlChunk wrapper. For internal use.
        /// </summary>
        internal HtmlChunk(HtmlParserMajestic.HtmlChunk chunk)
            : this()
        {
            Html = chunk.GenerateHtml();

            switch (chunk.oType)
            {
                case HtmlParserMajestic.HtmlChunkType.Text:
                    Type = HtmlChunkType.Text;
                    Text = chunk.oHTML ?? string.Empty;
                    Html = WebUtility.HtmlEncode(Text);
                    break;

                case HtmlParserMajestic.HtmlChunkType.OpenTag:
                    Type = HtmlChunkType.Tag;
                    TagName = chunk.sTag;
                    TagType = HtmlTagType.Open;
                    parameters = new Dictionary<string, string>(chunk.oParams);
                    break;

                case HtmlParserMajestic.HtmlChunkType.CloseTag:
                    Type = HtmlChunkType.Tag;
                    TagName = chunk.sTag;
                    TagType = chunk.bEndClosure ? HtmlTagType.SelfClose : HtmlTagType.Close;
                    parameters = new Dictionary<string, string>(chunk.oParams);
                    break;

                case HtmlParserMajestic.HtmlChunkType.Comment:
                    Type = HtmlChunkType.Comment;
                    break;
                case HtmlParserMajestic.HtmlChunkType.Script:
                    Type = HtmlChunkType.Script;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool HasParameter(string name)
        {
            return this.parameters != null && this.parameters.ContainsKey(name);
        }

        public string GetParameter(string name)
        {
            return this.HasParameter(name) ? this.parameters[name] : null;
        }

        public void AddParameter(string name, string value)
        {
            if (parameters == null) parameters = new Dictionary<string, string>();
            parameters.Add(name, value);
        }

        public void SetParameter(string name, string value)
        {
            if (parameters == null) parameters = new Dictionary<string, string>();
            parameters[name] = value;
        }

        public bool SafeSetParameter(string name, string value)
        {
            if (this.HasParameter(name))
            {
                this.SetParameter(name, value);
                return false;
            }
            else
            {
                this.AddParameter(name, value);
                return true;
            }
        }

        public void ClearParameters()
        {
            parameters = null;
        }

        public bool ParameterMatches(string name, string value)
        {
            return
                this.HasParameter(name) &&
                this.parameters[name] == value;
        }

        public bool ParameterMatches(string name, Regex regex)
        {
            return
                this.HasParameter(name) &&
                regex.IsMatch(this.parameters[name]);
        }

        /// <summary>
        /// Returns a string that represents the current HtmlChunk.
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case HtmlChunkType.Text:
                    return "Text (length = {0})".FormatWith(this.Text.Length);
                case HtmlChunkType.Tag:
                    return "Tag ({0}, name = '{1}', parameters: {2})".FormatWith(
                         TagType,
                         TagName,
                         parameters.IsNull() ? 0 : parameters.Count);
                default:
                    return Type.ToString();
            }
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(HtmlChunk other)
        {
            return Equals(other.Type, this.Type) && Equals(other.Text, this.Text) && Equals(other.TagName, this.TagName) && Equals(other.TagType, this.TagType) && Equals(other.parameters, this.parameters);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = this.Type.GetHashCode();
                result = (result * 397) ^ (this.Text != null ? this.Text.GetHashCode() : 0);
                result = (result * 397) ^ (this.TagName != null ? this.TagName.GetHashCode() : 0);
                result = (result * 397) ^ this.TagType.GetHashCode();
                result = (result * 397) ^ (this.parameters != null ? this.parameters.GetHashCode() : 0);
                return result;
            }
        }
    }
}
