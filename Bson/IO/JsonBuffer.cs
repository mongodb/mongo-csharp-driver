/* Copyright 2010 10gen Inc.
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
    public class JsonBuffer {
        #region private fields
        private string buffer;
        private int position;
        #endregion

        #region constructors
        public JsonBuffer(
            string buffer
        ) {
            this.buffer = buffer;
            this.position = 0;
        }
        #endregion

        #region internal properties
        public int Length {
            get { return buffer.Length; }
        }

        public int Position {
            get { return position; }
            set { position = value; }
        }
        #endregion

        #region public methods
        public int Read() {
            return (position >= buffer.Length) ? -1 : buffer[position++];
        }

        public string Substring(
            int start
        ) {
            return buffer.Substring(start);
        }

        public string Substring(
            int start,
            int count
        ) {
            return buffer.Substring(start, count);
        }

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
