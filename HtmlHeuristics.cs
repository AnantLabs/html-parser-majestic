namespace HtmlParserMajestic
{
    using System;
    using System.Collections;
    using System.Text;

    /// <summary>
    /// This class will control HTML tag heuristics that will allow faster matching of tags
    /// to avoid long cycles as well as creation of same strings over and over again.
    /// 
    /// This is effectively a fancy hash lookup table with attributes being hashed in context of tag
    /// </summary>
    ///<exclude/>
    internal class HtmlHeuristics : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Maximum number of chars to be taken into account
        /// </summary>
        private const int MAX_CHARS = byte.MaxValue;

        /// <summary>
        /// Maximum number of strings allowed to be set (all lower-cased)
        /// </summary>
        private const int MAX_STRINGS = 1024;

        /// <summary>
        /// This array will contain all double char strings 
        /// </summary>
        private static readonly string[,] sAllTwoCharStrings;

        /// <summary>
        /// Binary data represending attribute strings is here: case sensitive: lower case for even even value, and odd for each odd
        /// for the same string
        /// </summary>
        private readonly byte[][] bAttrData = new byte[MAX_STRINGS * 2][];

        /// <summary>
        /// Hash that will contain single char mapping hash
        /// </summary>
        private readonly byte[][] bAttributes = new byte[MAX_STRINGS * 2][];

        /// <summary>
        /// List of added attributes to avoid dups
        /// </summary>
        private readonly Hashtable oAddedAttributes = new Hashtable();

        private readonly string[] sAttrs = new string[MAX_STRINGS];

        private bool bDisposed;

        /// <summary>
        /// Binary data represending tag strings is here: case sensitive: lower case for even even value, and odd for each odd
        /// for the same string
        /// </summary>
        private byte[][] bTagData = new byte[MAX_STRINGS * 2][];

        /// <summary>
        /// List of added tags to avoid dups
        /// </summary>
        private Hashtable oAddedTags = new Hashtable();

        /// <summary>
        /// Array in which we will keep char hints to quickly match	ID (if non-zero) of tag
        /// </summary>
        private short[,] sChars = new short[byte.MaxValue + 1,byte.MaxValue + 1];

        /// <summary>
        /// Strings used, once matched they will be returned to avoid creation of a brand new string
        /// and all associated costs with it
        /// </summary>
        private string[] sStrings = new string[MAX_STRINGS];

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static HtmlHeuristics()
        {
            sAllTwoCharStrings = new string[(MAX_CHARS + 1),(MAX_CHARS + 1)];

            // we will create all possible strings for two bytes combinations - this will allow
            // to cater for all two char combinations at cost of mere 256kb of RAM per instance
            for (int i = 0; i < sAllTwoCharStrings.Length; i++)
            {
                var bChar1 = (byte)(i >> 8);
                var bChar2 = (byte)(i & 0xFF);

                sAllTwoCharStrings[bChar1, bChar2] = ((((char)bChar1).ToString() + ((char)bChar2).ToString())).ToLower();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds tag to list of tracked tags (don't add too many, if you have got multiple same first
        /// 2 chars then duplicates won't be added, so make sure the first added tags are the MOST LIKELY to be found)
        /// </summary>
        /// <param name="sTag">Tag: strictly ASCII only</param>
        /// <param name="sAttributeNames">Comma delimited list of attributed</param>
        /// <param name="bAddClosed">If true then closed version of tag added</param>
        /// <returns>True if tag was added, false otherwise (it may already be added, or leads to hash clash)</returns>
        public bool AddTag(string p_sTag, string sAttributeNames)
        {
            string sTag = p_sTag.ToLower().Trim();

            if (sTag.Length == 0 || sTag.Length > 32 || this.oAddedTags.Contains(sTag))
            {
                return false;
            }

            if (this.oAddedTags.Count >= byte.MaxValue)
            {
                return false;
            }

            // ID should not be zero as it is an indicator of no match
            var usID = (short)(this.oAddedTags.Count + 1);

            this.oAddedTags[sTag] = usID;

            // remember tag string: it will be returned in case of matching
            this.sStrings[usID] = sTag;

            // add both lower and upper case tag values
            if (!this.AddTag(sTag, usID, (short)(usID * 2 + 0)))
            {
                return false;
            }

            if (!this.AddTag(sTag.ToUpper(), usID, (short)(usID * 2 + 1)))
            {
                return false;
            }

            // allocate memory for attribute hashes for this tag
            this.bAttrData[usID] = new byte[byte.MaxValue + 1];

            // now add attribute names
            foreach (string p_sAName in sAttributeNames.ToLower().Split(','))
            {
                string sAName = p_sAName.Trim();

                if (sAName.Length == 0)
                {
                    continue;
                }

                // only add attribute if we have not got it added for same first char of the same tag:
                if (this.bAttrData[usID][sAName[0]] > 0 || this.bAttrData[usID][char.ToUpper(sAName[0])] > 0)
                {
                    continue;
                }

                int iAttrID = this.oAddedAttributes.Count + 1;

                if (this.oAddedAttributes.Contains(sAName))
                {
                    iAttrID = (int)this.oAddedAttributes[sAName];
                }
                else
                {
                    this.oAddedAttributes[sAName] = iAttrID;

                    this.sAttrs[iAttrID] = sAName;
                }

                // add both lower and upper case tag values
                this.AddAttribute(sAName, usID, (short)(iAttrID * 2 + 0));

                this.AddAttribute(sAName.ToUpper(), usID, (short)(iAttrID * 2 + 1));
            }

            return true;
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string GetAttr(int iAttrID)
        {
            return this.sAttrs[(iAttrID >> 1)];
        }

        public byte[] GetAttrData(int iAttrID)
        {
            return this.bAttributes[iAttrID];
        }

        /// <summary>
        /// Returns string for ID returned by GetMatch
        /// </summary>
        /// <param name="iID">ID</param>
        /// <returns>string</returns>
        public string GetString(int iID)
        {
            return this.sStrings[(iID >> 1)];
        }

        public byte[] GetStringData(int iID)
        {
            return this.bTagData[iID];
        }

        public string GetTwoCharString(byte cChar1, byte cChar2)
        {
            return sAllTwoCharStrings[cChar1, cChar2];
        }

        public short MatchAttr(byte bChar, int iTagID)
        {
            return this.bAttrData[iTagID >> 1][bChar];
        }

        public short MatchTag(byte cChar1, byte cChar2)
        {
            return this.sChars[cChar1, cChar2];
        }

        #endregion

        #region Methods

        private void AddAttribute(string sAttr, short usID, short usAttrID)
        {
            if (sAttr.Length == 0)
            {
                return;
            }

            var bChar = (byte)sAttr[0];

            this.bAttributes[usAttrID] = Encoding.Default.GetBytes(sAttr);

            this.bAttrData[usID][bChar] = (byte)usAttrID;
        }

        private bool AddTag(string sTag, short usID, short usDataID)
        {
            if (sTag.Length == 0)
            {
                return false;
            }

            this.bTagData[usDataID] = Encoding.Default.GetBytes(sTag);

            if (sTag.Length == 1)
            {
                // ok just one char, in which case we will mark possible second char that can be
                // '>', ' ' and other whitespace
                // we will use negative ID to hint that this is single char hit
                if (!this.SetHash(sTag[0], ' ', (short)(-1 * usDataID)))
                {
                    return false;
                }

                if (!this.SetHash(sTag[0], '\t', (short)(-1 * usDataID)))
                {
                    return false;
                }

                if (!this.SetHash(sTag[0], '\r', (short)(-1 * usDataID)))
                {
                    return false;
                }

                if (!this.SetHash(sTag[0], '\n', (short)(-1 * usDataID)))
                {
                    return false;
                }

                if (!this.SetHash(sTag[0], '>', (short)(-1 * usDataID)))
                {
                    return false;
                }
            }
            else
            {
                if (!this.SetHash(sTag[0], sTag[1], usDataID))
                {
                    return false;
                }
            }

            return true;
        }

        private void Dispose(bool bDisposing)
        {
            if (!this.bDisposed)
            {
                this.sChars = null;
                this.oAddedTags = null;
                this.sStrings = null;
                this.bTagData = null;
            }

            this.bDisposed = true;
        }

        private bool SetHash(char cChar1, char cChar2, short usID)
        {
            // already exists
            if (this.sChars[(byte)cChar1, (byte)cChar2] != 0)
            {
                return false;
            }

            this.sChars[(byte)cChar1, (byte)cChar2] = usID;

            return true;
        }

        #endregion
    }
}