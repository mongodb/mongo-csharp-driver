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

using MongoDB.Bson.IO;

namespace MongoDB.Bson.DefaultSerializer.Conventions {
    public class InferTypeDiscriminatorConvention : IDiscriminatorConvention, System.Collections.IEnumerable {
        private IDictionary<string, Type> TypesByFieldName;

        public InferTypeDiscriminatorConvention()
            : this(new Dictionary<string, Type>()) {
        }

        public InferTypeDiscriminatorConvention(IEnumerable<KeyValuePair<string, Type>> typesByFieldName) {
            TypesByFieldName = new Dictionary<string, Type>();

            foreach (KeyValuePair<string, Type> kvp in typesByFieldName)
                TypesByFieldName[kvp.Key] = kvp.Value;
        }

        public void Add(string fieldName, Type type) {
            TypesByFieldName.Add(fieldName, type);
        }

        #region IDiscriminatorConvention

        public string ElementName { get { return null; } }

        public Type GetActualType(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var actualType = nominalType;
            var bookmark = bsonReader.GetBookmark();

            try {
                bsonReader.ReadStartDocument();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var name = bsonReader.ReadName();

                    if (TypesByFieldName.TryGetValue(name, out actualType)) {
                        break;
                    }
                    else {
                        bsonReader.SkipValue();
                    }
                }
            }
            finally {
                bsonReader.ReturnToBookmark(bookmark);
            }

            return actualType;
        }

        public BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType
        ) {
            return null;
        }

        #endregion

        public System.Collections.IEnumerator GetEnumerator() {
            return TypesByFieldName.GetEnumerator();
        }
    }
}
