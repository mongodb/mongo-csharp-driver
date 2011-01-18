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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson {
    public static class BsonUtils {
        #region public static methods
        public static byte[] ParseHexString(
            string s
        ) {
            byte[] bytes;
            if (!TryParseHexString(s, out bytes)) {
                throw new FormatException("Not a valid hex string");
            }
            return bytes;
        }

        public static string ToHexString(
            byte[] bytes
        ) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        public static bool TryParseHexString(
            string s,
            out byte[] bytes
        ) {
            if ((s.Length & 1) != 0) { s = "0" + s; } // make length of s even
            bytes = new byte[s.Length / 2];
            for (int i = 0; i < bytes.Length; i++) {
                string hex = s.Substring(2 * i, 2);
                try {
                    byte b = Convert.ToByte(hex, 16);
                    bytes[i] = b;
                } catch (FormatException) {
                    bytes = null;
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}