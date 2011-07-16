namespace HtmlParserMajestic
{
    using System;
    using System.Text;

    /// <summary>
    /// Class for fast dynamic string building - it is faster than StringBuilder
    /// </summary>
    ///<exclude/>
    internal class DynaString : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// CRITICAL: that much capacity will be allocated (once) for this object -- for performance reasons
        /// we do NOT have range checks because we make reasonably safe assumption that accumulated string will
        /// fit into the buffer. If you have very abnormal strings then you should increase buffer accordingly.
        /// </summary>
        public static int TEXT_CAPACITY = 1024 * 256 - 1;

        public byte[] bBuffer;

        public int iBufPos;

        /// <summary>
        /// Finalised text will be available in this string
        /// </summary>
        public string sText;

        private bool bDisposed;

        private int iLength;

        private Encoding oEnc = Encoding.Default;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="sString">Initial string</param>
        internal DynaString(string sString)
        {
            this.sText = sString;
            this.iBufPos = 0;
            this.bBuffer = new byte[TEXT_CAPACITY + 1];
            this.iLength = sString.Length;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Appends proper char with smart handling of Unicode chars
        /// </summary>
        /// <param name="cChar">Char to append</param>
        public void Append(char cChar)
        {
            if (cChar <= 127)
            {
                this.bBuffer[this.iBufPos++] = (byte)cChar;
            }
            else
            {
                // unicode character - this is really bad way of doing it, but 
                // it seems to be called almost never
                byte[] bBytes = this.oEnc.GetBytes(cChar.ToString());

                // 16/09/07 Possible bug reported by Martin Bächtold: 
                // test case: 
                // <meta http-equiv="Content-Category" content="text/html; charset=windows-1251">
                // &#1329;&#1378;&#1400;&#1406;&#1397;&#1377;&#1398; &#1341;&#1377;&#1401;&#1377;&#1407;&#1400;&#1410;&#1408;

                // the problem is that some unicode chars might not be mapped to bytes by specified encoding
                // in the HTML itself, this means we will get single byte ? - this will look like failed conversion
                // Not good situation that we need to deal with :(
                if (bBytes.Length == 1 && bBytes[0] == '?')
                {
                    // TODO: 

                    for (int i = 0; i < bBytes.Length; i++)
                    {
                        this.bBuffer[this.iBufPos++] = bBytes[i];
                    }
                }
                else
                {
                    for (int i = 0; i < bBytes.Length; i++)
                    {
                        this.bBuffer[this.iBufPos++] = bBytes[i];
                    }
                }
            }
        }

        /// <summary>
        /// Resets object to zero length string
        /// </summary>
        public void Clear()
        {
            this.sText = "";
            this.iLength = 0;
            this.iBufPos = 0;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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

        /*
        /// <summary>
        /// Appends a "char" to the buffer
        /// </summary>
        /// <param name="cChar">Appends char (byte really)</param>
        public void Append(byte cChar)
        {
            // Length++;

            if(iBufPos>=TEXT_CAPACITY)
            {
                if(sText.Length==0)
                {
                    sText=oEnc.GetString(bBuffer,0,iBufPos);
                }
                else
                    //sText+=new string(bBuffer,0,iBufPos);
                    sText+=oEnc.GetString(bBuffer,0,iBufPos);

                iLength+=iBufPos;

                iBufPos=0;
            }

            bBuffer[iBufPos++]=cChar;
        }
        */

        #region Methods

        /// <summary>
        /// Creates string from buffer using set encoder
        /// </summary>
        internal string SetToString()
        {
            if (this.iBufPos > 0)
            {
                if (this.sText.Length == 0)
                {
                    this.sText = this.oEnc.GetString(this.bBuffer, 0, this.iBufPos);
                }
                else
                {
                    //sText+=new string(bBuffer,0,iBufPos);
                    this.sText += this.oEnc.GetString(this.bBuffer, 0, this.iBufPos);
                }

                this.iLength += this.iBufPos;
                this.iBufPos = 0;
            }

            return this.sText;
        }

        /// <summary>
        /// Creates string from buffer using default encoder
        /// </summary>
        internal string SetToStringASCII()
        {
            if (this.iBufPos > 0)
            {
                if (this.sText.Length == 0)
                {
                    this.sText = Encoding.Default.GetString(this.bBuffer, 0, this.iBufPos);
                }
                else
                {
                    //sText+=new string(bBuffer,0,iBufPos);
                    this.sText += Encoding.Default.GetString(this.bBuffer, 0, this.iBufPos);
                }

                this.iLength += this.iBufPos;
                this.iBufPos = 0;
            }

            return this.sText;
        }

        private void Dispose(bool bDisposing)
        {
            if (!this.bDisposed)
            {
                this.bBuffer = null;
            }

            this.bDisposed = true;
        }

        #endregion
    }
}