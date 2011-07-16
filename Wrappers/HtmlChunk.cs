namespace HtmlParserMajestic.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class HtmlChunk : IEquatable<HtmlChunk>
    {
        public HtmlChunkType Type { get; set; }
        public string Html { get; set; }
        public string Text { get; set; }
        public string TagName { get; set; }
        public HtmlTagType TagType { get; set; }

        private Dictionary<string, string> parameters;

        public HtmlChunk(string tagName, HtmlTagType tagType, Dictionary<string, string> parameters = null)
        {
            this.Type = HtmlChunkType.Tag;

            this.TagName = tagName;
            this.TagType = tagType;
            this.parameters = parameters;
        }

        public HtmlChunk(string text)
        {
            this.Type = HtmlChunkType.Text;

            this.Text = text;
        }

        /// <summary>
        /// Creates new HtmlChunk wrapper. For internal use.
        /// </summary>
        internal HtmlChunk(HtmlParserMajestic.HtmlChunk chunk)
        {
            Html = chunk.GenerateHtml();

            switch (chunk.oType)
            {
                case HtmlParserMajestic.HtmlChunkType.Text:
                    Text = chunk.oHTML;
                    break;

                case HtmlParserMajestic.HtmlChunkType.OpenTag:
                    TagName = chunk.sTag;
                    TagType = HtmlTagType.Open;
                    parameters = chunk.oParams;
                    break;

                case HtmlParserMajestic.HtmlChunkType.CloseTag:
                    TagName = chunk.sTag;
                    TagType = chunk.bEndClosure ? HtmlTagType.SelfClose : HtmlTagType.Close;
                    parameters = chunk.oParams;
                    break;

                case HtmlParserMajestic.HtmlChunkType.Comment:
                case HtmlParserMajestic.HtmlChunkType.Script:
                    // TODO: support of comments and scripts
                    throw new NotImplementedException();
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
            return Type.ToString();
        }

        public bool Equals(HtmlChunk other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return other.Type == this.Type && other.Html == this.Html;
        }

        /// <summary>
        /// Returns hash code for the current HtmlChunk.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Type.GetHashCode() * 397) ^ (this.Html != null ? this.Html.GetHashCode() : 0);
            }
        }
    }
}
