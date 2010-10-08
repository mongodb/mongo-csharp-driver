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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary {
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

        public static byte[] ToBson(
            object obj
        ) {
            return ToBson(obj, BsonBinaryWriterSettings.Defaults);
        }

        public static byte[] ToBson(
            object obj,
            BsonBinaryWriterSettings settings
        ) {
            using (var buffer = new BsonBuffer()) {
                using (var bsonWriter = BsonWriter.Create(buffer, settings)) {
                    BsonSerializer.Serialize(bsonWriter, obj, false); // don't serializeIdFirst
                }
                return buffer.ToArray();
            }
        }

        public static BsonDocument ToBsonDocument(
            object obj
        ) {
            if (obj == null) {
                return null;
            }

            var bsonDocument = obj as BsonDocument;
            if (bsonDocument != null) {
                return bsonDocument; // it's already a BsonDocument
            }

            var builder = obj as IBsonDocumentBuilder;
            if (builder != null) {
                return builder.ToBsonDocument(); // use the provided ToBsonDocument method
            }

            // otherwise serialize it and then deserialize it into a new BsonDocument
            using (var buffer = new BsonBuffer()) {
                using (var bsonWriter = BsonWriter.Create(buffer)) {
                    BsonSerializer.Serialize(bsonWriter, obj, false); // don't serializeIdFirst
                }
                buffer.Position = 0;
                using (var bsonReader = BsonReader.Create(buffer)) {
                    return BsonDocument.ReadFrom(bsonReader);
                }
            }
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

        public static string ToJson(
            object obj
        ) {
            return ToJson(obj, BsonJsonWriterSettings.Defaults);
        }

        public static string ToJson(
            object obj,
            BsonJsonWriterSettings settings
        ) {
            var stringWriter = new StringWriter();
            using (var bsonWriter = BsonWriter.Create(stringWriter, settings)) {
                BsonSerializer.Serialize(bsonWriter, obj, false); // don't serializeIdFirst
            }
            return stringWriter.ToString();
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
