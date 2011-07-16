/*

Copyright (c) Alex Chudnovsky, Majestic-12 Ltd (UK). 2005+ All rights reserved
Web:		http://www.majestic12.co.uk
E-mail:		alexc@majestic12.co.uk

Redistribution and use in source and binary forms, with or without modification, are 
permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions 
		and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
		and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the Majestic-12 nor the names of its contributors may be used to endorse or 
		promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

namespace HtmlParserMajestic
{
    using System;
    using System.Collections;

    /// <summary>
    /// FastHash: class provides fast look ups at the expense of memory (at least 128k per object).
    /// Its designed primarily for those hashes where majority of lookups are unsuccessful 
    /// (ie object is not present)
    /// 
    /// Status of this work is EXPERIMENTAL, do not make any untested assumptions.
    /// 
    /// History:	15/12/06 Added range check in GetXY
    ///				sometime in 2005: initial imlpementation
    /// 
    /// </summary>
    ///<exclude/>
    public class FastHash : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Maximum number of chars to be taken into account
        /// </summary>
        private const int MAX_CHARS = 256;

        /// <summary>
        /// Maximum number of keys to be stored
        /// </summary>
        private const int MAX_KEYS = 32 * 1024;

        /// <summary>
        /// Value indicating there are multiple keys stored in a given position
        /// </summary>
        private const ushort MULTIPLE_KEYS = ushort.MaxValue;

        /// <summary>
        /// Keys
        /// </summary>
        private readonly string[] sKeys = new string[MAX_KEYS];

        private bool bDisposed;

        /// <summary>
        /// Maximum key length
        /// </summary>
        private int iMaxLen = int.MinValue;

        /// <summary>
        /// Minimum key length 
        /// </summary>
        private int iMinLen = int.MaxValue;

        /// <summary>
        /// Values of keys
        /// </summary>
        private object[] iValues = new object[MAX_KEYS];

        /// <summary>
        /// Hash that will contain keys and will be used at the last resort as looksup are too slow
        /// </summary>
        private Hashtable oHash = new Hashtable();

        /// <summary>
        /// Array in which we will keep char hints
        /// </summary>
        private ushort[,] usChars = new ushort[MAX_CHARS,MAX_CHARS];

        /// <summary>
        /// Number of keys stored
        /// </summary>
        private ushort usCount;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets keys in this hash
        /// </summary>
        public ICollection Keys
        {
            get
            {
                return this.oHash.Keys;
            }
        }

        #endregion

        #region Public Indexers

        /// <summary>
        /// Access to values via indexer
        /// </summary>
        public object this[string sKey]
        {
            get
            {
                return this.GetValue(sKey);
            }
            set
            {
                if (!this.Contains(sKey))
                {
                    this.Add(sKey, value);
                }
            }
        }

        #endregion

        #region Public Methods

        public static void GetXY(string sKey, out int iX, out int iY)
        {
            // most likely scenario is that we have at least 2 chars
            if (sKey.Length >= 2)
            {
                iX = sKey[0];
                iY = sKey[1];
            }
            else
            {
                if (sKey.Length == 0)
                {
                    iX = MAX_CHARS + 1;
                    iY = MAX_CHARS + 1;
                    return;
                }

                iX = sKey[0];
                iY = 0;
            }

            //Console.WriteLine("{0}: {1}-{2}",sKey,iX,iY);
        }

        /// <summary>
        /// Adds key to the fast hash
        /// </summary>
        /// <param name="sKey">Key</param>
        public void Add(string sKey)
        {
            this.Add(sKey, 0);
        }

        /// <summary>
        /// Adds key and its value to the fast hash
        /// </summary>
        /// <param name="sKey">Key</param>
        /// <param name="iValue">Value</param>
        public void Add(string sKey, object iValue)
        {
            if (this.usCount >= ushort.MaxValue)
            {
                throw new Exception("Fast hash is full and can't add more keys!");
            }

            if (sKey.Length == 0)
            {
                return;
            }

            this.iMinLen = Math.Min(sKey.Length, this.iMinLen);
            this.iMaxLen = Math.Max(sKey.Length, this.iMaxLen);

            int iX, iY;

            GetXY(sKey, out iX, out iY);

            if (iX < MAX_CHARS && iY < MAX_CHARS)
            {
                ushort usCutPos = this.usChars[iX, iY];

                if (usCutPos == 0)
                {
                    this.usChars[iX, iY] = (ushort)(this.usCount + 1);

                    this.iValues[this.usCount] = iValue;
                    this.sKeys[this.usCount] = sKey;
                }
                else
                {
                    // mark this entry with maxvalue indicating that there is more than one key stored there
                    this.usChars[iX, iY] = MULTIPLE_KEYS;
                }

                this.usCount++;
            }

            this.oHash[sKey] = iValue;
        }

        /// <summary>
        /// Checks if given key is present in the hash
        /// </summary>
        /// <param name="sKey">Key</param>
        /// <returns>True if key is present</returns>
        public bool Contains(string sKey)
        {
            // if requested string is too short or too long then we can quickly return false
            // NOTE: seems to use too much CPU for the amount of useful work it does

            // NOTE 2: better do it than get nasty excepton...
            if (sKey.Length < this.iMinLen || sKey.Length > this.iMaxLen)
            {
                return false;
            }

            int iX, iY;

            GetXY(sKey, out iX, out iY);

            if (iX < MAX_CHARS && iY < MAX_CHARS)
            {
                ushort usPos = this.usChars[iX, iY];

                if (usPos == 0)
                {
                    return false;
                }

                // now check if its just one key
                if (usPos != MULTIPLE_KEYS && this.sKeys[usPos - 1] == sKey)
                {
                    return true;
                }
            }

            // finally we have no choice but to do a proper hash lookup
            return this.oHash[sKey] != null;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns value of a key that is VERY likely to be present - this avoids doing some checks that
        /// are most likely to be pointless thus making overall faster function
        /// </summary>
        /// <param name="sKey">Key</param>
        /// <returns>Null if no value or value itself</returns>
        public object GetLikelyPresentValue(string sKey)
        {
            if (sKey.Length < this.iMinLen || sKey.Length > this.iMaxLen)
            {
                return null;
            }

            int iX, iY;

            GetXY(sKey, out iX, out iY);

            if (iX < MAX_CHARS && iY < MAX_CHARS)
            {
                ushort usPos = this.usChars[iX, iY];

                if (usPos == 0)
                {
                    return null;
                }

                // now check string is the only one
                if (usPos != MULTIPLE_KEYS && this.sKeys[usPos - 1] == sKey)
                {
                    return this.iValues[usPos - 1];
                }
            }

            // finally we have no choice but to do a proper hash lookup
            return this.oHash[sKey];
        }

        /// <summary>
        /// Returns value for likely present keys using first chars (byte)
        /// </summary>
        /// <param name="iX">Byte 1 denoting char 1</param>
        /// <param name="iY">Byte 2 denoting char 2 (0 if not present)</param>
        /// <returns>Non-null value if it was found, or null if full search for key is required</returns>
        public object GetLikelyPresentValue(byte iX, byte iY)
        {
            ushort usPos = this.usChars[iX, iY];

            if (usPos != MULTIPLE_KEYS && usPos != 0)
            {
                return this.iValues[usPos - 1];
            }

            // finally we have no choice but to do a proper hash lookup
            return null;
        }

        /// <summary>
        /// Returns value associated with the key or null if key not present
        /// </summary>
        /// <param name="sKey">Key</param>
        /// <returns>Null or object convertable to integer as value</returns>
        public object GetValue(string sKey)
        {
            // if requested string is too short or too long then we can quickly return false
            if (sKey.Length < this.iMinLen || sKey.Length > this.iMaxLen)
            {
                return null;
            }

            int iX, iY;

            GetXY(sKey, out iX, out iY);

            if (iX < MAX_CHARS && iY < MAX_CHARS)
            {
                ushort usPos = this.usChars[iX, iY];

                if (usPos == 0)
                {
                    return null;
                }

                // now check string in list of keys
                if (usPos != MULTIPLE_KEYS && this.sKeys[usPos - 1] == sKey)
                {
                    return this.iValues[usPos - 1];
                }
            }

            // finally we have no choice but to do a proper hash lookup
            //Console.WriteLine("Have to use hash... :(");
            return this.oHash[sKey];
        }

        /// <summary>
        /// Quickly checks if given chars POSSIBLY refer to a stored key.
        /// </summary>
        /// <param name="cChar1">Char 1</param>
        /// <param name="cChar2">Char 2</param>
        /// <param name="iLength">Length of string</param>
        /// <returns>False is string is DEFINATELY NOT present, or true if it MAY be present</returns>
        public bool PossiblyContains(char cChar1, char cChar2, int iLength)
        {
            ushort usPos = this.usChars[cChar1, cChar2];

            if (usPos == 0)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Methods

        private void Dispose(bool bDisposing)
        {
            if (!this.bDisposed)
            {
                this.oHash = null;
                this.usChars = null;
                this.iValues = null;
            }
        }

        #endregion
    }
}