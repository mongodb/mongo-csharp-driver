/* Copyright 2010-2011 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// This class represents a JSON string buffer.
    /// </summary>
    public class JsonBuffer {
        #region private fields
        private string buffer;
        private int position;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonBuffer class.
        /// </summary>
        /// <param name="buffer">The string.</param>
        public JsonBuffer(
            string buffer
        ) {
            this.buffer = buffer;
            this.position = 0;
        }
        #endregion

        #region internal properties
        /// <summary>
        /// Gets the length of the JSON string.
        /// </summary>
        public int Length {
            get { return buffer.Length; }
        }

        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        public int Position {
            get { return position; }
            set { position = value; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Reads a character from the buffer.
        /// </summary>
        /// <returns>The next character (or -1 if at the end of the buffer).</returns>
        public int Read() {
            return (position >= buffer.Length) ? -1 : buffer[position++];
        }

        /// <summary>
        /// Reads a substring from the buffer.
        /// </summary>
        /// <param name="start">The zero based index of the start of the substring.</param>
        /// <returns>The substring.</returns>
        public string Substring(
            int start
        ) {
            return buffer.Substring(start);
        }

        /// <summary>
        /// Reads a substring from the buffer.
        /// </summary>
        /// <param name="start">The zero based index of the start of the substring.</param>
        /// <param name="count">The number of characters in the substring.</param>
        /// <returns>The substring.</returns>
        public string Substring(
            int start,
            int count
        ) {
            return buffer.Substring(start, count);
        }

        /// <summary>
        /// Returns one character to the buffer (if the character matches the one at the current position the current position is moved back by one).
        /// </summary>
        /// <param name="c">The character to return.</param>
        public void UnRead(
            int c
        ) {
            if (c != -1 && buffer[position - 1] == c) {
                position -= 1;
            }
        }
        #endregion
    }
}
