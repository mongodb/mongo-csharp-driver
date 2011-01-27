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
    public static class BsonExtensionMethods {
        public static byte[] ToBson<T>(
            this T obj
        ) {
            return ToBson(obj, BsonBinaryWriterSettings.Defaults);
        }

        public static byte[] ToBson<T>(
            this T obj,
            IBsonSerializationOptions options
        ) {
            return ToBson(obj, options, BsonBinaryWriterSettings.Defaults);
        }

        public static byte[] ToBson<T>(
            this T obj,
            IBsonSerializationOptions options,
            BsonBinaryWriterSettings settings
        ) {
            using (var buffer = new BsonBuffer()) {
                using (var bsonWriter = BsonWriter.Create(buffer, settings)) {
                    BsonSerializer.Serialize<T>(bsonWriter, obj, options);
                }
                return buffer.ToByteArray();
            }
        }

        public static byte[] ToBson<T>(
            this T obj,
            BsonBinaryWriterSettings settings
        ) {
            return ToBson(obj, null, settings);
        }

        public static BsonDocument ToBsonDocument<T>(
            this T obj
        ) {
            return obj.ToBsonDocument(null);
        }

        public static BsonDocument ToBsonDocument<T>(
            this T obj,
            IBsonSerializationOptions options
        ) {
            if (obj == null) {
                return null;
            }

            var bsonDocument = obj as BsonDocument;
            if (bsonDocument != null) {
                return bsonDocument; // it's already a BsonDocument
            }

            var convertibleToBsonDocument = obj as IConvertibleToBsonDocument;
            if (convertibleToBsonDocument != null) {
                return convertibleToBsonDocument.ToBsonDocument(); // use the provided ToBsonDocument method
            }

            // otherwise serialize into a new BsonDocument
            var document = new BsonDocument();
            using (var writer = BsonWriter.Create(document)) {
                BsonSerializer.Serialize<T>(writer, obj, options);
            }
            return document;
        }

        public static string ToJson<T>(
            this T obj
        ) {
            return ToJson(obj, JsonWriterSettings.Defaults);
        }

        public static string ToJson<T>(
            this T obj,
            IBsonSerializationOptions options
        ) {
            return ToJson(obj, options, JsonWriterSettings.Defaults);
        }

        public static string ToJson<T>(
            this T obj,
            IBsonSerializationOptions options,
            JsonWriterSettings settings
        ) {
            using (var stringWriter = new StringWriter()) {
                using (var bsonWriter = BsonWriter.Create(stringWriter, settings)) {
                    BsonSerializer.Serialize<T>(bsonWriter, obj, options);
                }
                return stringWriter.ToString();
            }
        }

        public static string ToJson<T>(
            this T obj,
            JsonWriterSettings settings
        ) {
            return ToJson(obj, null, settings);
        }
    }
}
