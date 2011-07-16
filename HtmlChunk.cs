namespace HtmlParserMajestic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Parsed HTML token that is either text, comment, script, open or closed tag as indicated by the oType variable.
    /// </summary>
    internal class HtmlChunk : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Maximum number of parameters in a tag - should be high enough to fit most sensible cases
        /// </summary>
        /// <exclude/>
        private const int MAX_PARAMS = 256;

        /// <summary>
        /// If true then it must be closed tag
        /// </summary>
        /// <exclude/>
        public bool bClosure;

        /// <summary>
        /// If true then it must be comments tag
        /// </summary>
        /// <exclude/>
        public bool bComments;

        /// <summary>
        /// If true then it must be closed tag and closure sign / was at the END of tag, ie this is a SOLO
        /// tag 
        /// </summary>
        /// <exclude/>
        public bool bEndClosure;

        /// <summary>
        /// True if entities were present (and transformed) in the original HTML
        /// </summary>
        /// <exclude/>
        public bool bEntities;

        /// <summary>
        /// Set to true if &lt; entity (tag start) was found 
        /// </summary>
        /// <exclude/>
        public bool bLtEntity;

        /// <summary>
        /// Character used to quote param's value: it is taken actually from parsed HTML
        /// </summary>
        public byte[] cParamChars = new byte[MAX_PARAMS];

        /// <summary>
        /// Length of the chunk in bHTML data array
        /// </summary>
        public int iChunkLength;

        /// <summary>
        /// Offset in bHTML data array at which this chunk starts
        /// </summary>
        public int iChunkOffset;

        /// <summary>
        /// Number of parameters and values stored in sParams array, OR in oParams hashtable if
        /// bHashMode is true
        /// </summary>
        public int iParams;

        /// <summary>
        /// Encoder to be used for conversion of binary data into strings, Encoding.Default is used by default,
        /// but it can be changed if top level user of the parser detects that encoding was different
        /// </summary>
        public Encoding oEnc = Encoding.Default;

        /// <summary>
        /// For TAGS: it stores raw HTML that was parsed to generate thus chunk will be here UNLESS
        /// HtmlParser was configured not to store it there as it can improve performance
        /// <p>
        /// For TEXT or COMMENTS: actual text or comments - you MUST call Finalise(); first.
        /// </p>
        /// </summary>
        public string oHTML = "";

        /// <summary>
        /// Hashtable with tag parameters: keys are param names and values are param values.
        /// ONLY used if bHashMode is set to TRUE.
        /// </summary>
        public Dictionary<string, string> oParams;

        /// <summary>
        /// Chunk type showing whether its text, open or close tag, comments or script.
        /// WARNING: if type is comments or script then you have to manually call Finalise(); method
        /// in order to have actual text of comments/scripts in oHTML variable
        /// </summary>
        public HtmlChunkType oType;

        /// <summary>
        /// If its open/close tag type then this is where lowercased Tag will be kept
        /// </summary>
        public string sTag = "";

        private bool bDisposed;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initialises new HtmlChunk
        /// </summary>
        public HtmlChunk()
        {
            oParams = new Dictionary<string, string>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Makes parameter value safe to be used in param - this will check for any conflicting quote chars,
        /// but not full entity-encoding
        /// </summary>
        /// <param name="sLine">Line of text</param>
        /// <param name="cQuoteChar">Quote char used in param - any such chars in text will be entity-encoded</param>
        /// <returns>Safe text to be used as param's value</returns>
        public static string MakeSafeParamValue(string sLine, char cQuoteChar)
        {
            // we speculatievly expect that in most cases we don't actually need to entity-encode string,

            for (int i = 0; i < sLine.Length; i++)
            {
                if (sLine[i] == cQuoteChar)
                {
                    // have to restart here
                    var oSB = new StringBuilder(sLine.Length + 10);

                    oSB.Append(sLine.Substring(0, i));

                    for (int j = i; j < sLine.Length; j++)
                    {
                        char cChar = sLine[j];

                        if (cChar == cQuoteChar)
                        {
                            oSB.Append("&#" + ((int)cChar).ToString() + ";");
                        }
                        else
                        {
                            oSB.Append(cChar);
                        }
                    }

                    return oSB.ToString();
                }
            }

            return sLine;
        }

        /// <summary>
        /// Adds tag parameter to the chunk
        /// </summary>
        /// <param name="sParam">Parameter name (ie color)</param>
        /// <param name="sValue">Value of the parameter (ie white)</param>
        public void AddParam(string sParam, string sValue, byte cParamChar)
        {
            this.iParams++;
            this.oParams.Add(sParam, sValue);
        }

        /// <summary>
        /// Clears chunk preparing it for 
        /// </summary>
        public void Clear()
        {
            this.sTag = this.oHTML = "";
            this.bLtEntity = this.bEntities = this.bComments = this.bClosure = this.bEndClosure = false;

            this.iParams = 0;

            if (this.oParams == null)
            {
                this.oParams = new Dictionary<string, string>();
            }
            else
            {
                this.oParams.Clear();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Generates HTML based on current chunk's data 
        /// Note: this is not a high performance method and if you want ORIGINAL HTML that was parsed to create
        /// this chunk then use relevant HtmlParser method to obtain such HTML then you should use
        /// function of parser: SetRawHTML
        /// 
        /// </summary>
        /// <returns>HTML equivalent of this chunk</returns>
        public string GenerateHtml()
        {
            string sHTML = "";

            switch (this.oType)
            {
                // matched open tag, ie <a href="">
                case HtmlChunkType.OpenTag:
                    sHTML += "<" + this.sTag;

                    if (this.iParams > 0)
                    {
                        sHTML += " " + this.GenerateParamsHtml();
                    }

                    sHTML += ">";

                    break;

                // matched close tag, ie </a>
                case HtmlChunkType.CloseTag:

                    if (this.iParams > 0 || this.bEndClosure)
                    {
                        sHTML += "<" + this.sTag;

                        if (this.iParams > 0)
                        {
                            sHTML += " " + this.GenerateParamsHtml();
                        }

                        sHTML += "/>";
                    }
                    else
                    {
                        sHTML += "</" + this.sTag + ">";
                    }
                    break;

                case HtmlChunkType.Script:

                    if (this.oHTML.Length == 0)
                    {
                        sHTML = "<script>n/a</script>";
                    }
                    else
                    {
                        sHTML = this.oHTML;
                    }

                    break;

                case HtmlChunkType.Comment:

                    // note: we might have CDATA here that we treat as comments

                    if (this.sTag == "!--")
                    {
                        if (this.oHTML.Length == 0)
                        {
                            sHTML = "<!-- n/a -->";
                        }
                        else
                        {
                            sHTML = "<!--" + this.oHTML + "-->";
                        }
                    }
                    else
                    {
                        // ref: http://www.w3schools.com/xml/xml_cdata.asp
                        if (this.sTag == "![CDATA[")
                        {
                            if (this.oHTML.Length == 0)
                            {
                                sHTML = "<![CDATA[ n/a \n]]>";
                            }
                            else
                            {
                                sHTML = "<![CDATA[" + this.oHTML + "]]>";
                            }
                        }
                    }

                    break;

                // matched normal text
                case HtmlChunkType.Text:

                    return this.oHTML;
            }
            ;

            return sHTML;
        }

        /// <summary>
        /// Generates HTML for param/value pair
        /// </summary>
        /// <param name="sParam">Param</param>
        /// <param name="sValue">Value (empty if not specified)</param>
        /// <returns>String with HTML</returns>
        public static string GenerateParamHtml(string sParam, string sValue, char cParamChar)
        {
            if (sValue.Length > 0)
            {
                // check param's value for whitespace or quote chars, if they are not present, then
                // we can save 2 bytes by not generating quotes
                if (sValue.Length > 20)
                {
                    return sParam + "=" + cParamChar + MakeSafeParamValue(sValue, cParamChar) + cParamChar;
                }

                foreach (char ch in sValue)
                {
                    switch (ch)
                    {
                        case ' ':
                        case '\t':
                        case '\'':
                        case '\"':
                        case '\n':
                        case '\r':
                            return sParam + "='" + MakeSafeParamValue(sValue, '\'') + "'";
                    }
                }

                return sParam + "=" + sValue;
            }
            else
            {
                return sParam;
            }
        }

        /// <summary>
        /// Generates HTML for params in this chunk
        /// </summary>
        /// <returns>String with HTML corresponding to params</returns>
        public string GenerateParamsHtml()
        {
            string sParamHTML = "";

            if (this.oParams.Count > 0)
            {
                foreach (string sParam in this.oParams.Keys)
                {
                    var sValue = (string)this.oParams[sParam];

                    if (sParamHTML.Length > 0)
                    {
                        sParamHTML += " ";
                    }

                    // FIXIT: this is really not correct as we do not use same char used
                    sParamHTML += GenerateParamHtml(sParam, sValue, '\'');
                }
            }

            return sParamHTML;
        }

        /// <summary>
        /// Returns value of a parameter
        /// </summary>
        /// <param name="sParam">Parameter</param>
        /// <returns>Parameter value or empty string</returns>
        public string GetParamValue(string sParam)
        {
            return this.oParams.ContainsKey(sParam) ?
                this.oParams[sParam] :
                string.Empty;
        }

        /// <summary>
        /// Sets encoding to be used for conversion of binary data into string
        /// </summary>
        /// <param name="p_oEnc">Encoding object</param>
        public void SetEncoding(Encoding p_oEnc)
        {
            this.oEnc = p_oEnc;
        }

        #endregion

        #region Methods

        private void Dispose(bool bDisposing)
        {
            if (!this.bDisposed)
            {
                this.oParams = null;
            }

            this.bDisposed = true;
        }

        #endregion



        /// <summary>
        /// The clone.
        /// </summary>
        public HtmlChunk Clone()
        {
            var result = (HtmlChunk)this.MemberwiseClone();
            if (this.cParamChars != null)
            {
                result.cParamChars = (byte[])this.cParamChars.Clone();
            }

            if (this.oEnc != null)
            {
                result.oEnc = (Encoding)this.oEnc.Clone();
            }

            if (this.oParams != null)
            {
                result.oParams = new Dictionary<string, string>(this.oParams);
            }
            return result;
        }

        public bool Equals(HtmlChunk other)
        {
            return this.GenerateHtml() == other.GenerateHtml();
        }
    }
}