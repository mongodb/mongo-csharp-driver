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

using MongoDB.BsonLibrary;
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;
using MongoDB.CSharpDriver;

namespace MongoDB.CSharpDriver.Builders {
    public static class GroupBy {
        #region public static methods
        public static BsonJavaScript Function(
            BsonJavaScript keyFunction
        ) {
            return keyFunction;
        }

        public static GroupByBuilder Keys(
            params string[] names
        ) {
            return new GroupByBuilder(names);
        }
        #endregion
    }

    [Serializable]
    public class GroupByBuilder : BuilderBase, IConvertibleToBsonDocument, IBsonSerializable {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public GroupByBuilder(
            string[] names
        ) {
            document = new BsonDocument();
            foreach (var name in names) {
                document.Add(name, 1);
            }
        }
        #endregion

        #region public methods
        public BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region explicit interface implementations
        object IBsonSerializable.DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException("Deserialize is not supported for GroupByBuilder");
        }

        object IBsonSerializable.DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            throw new InvalidOperationException("Deserialize is not supported for GroupByBuilder");
        }

        void IBsonSerializable.SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            bool serializeIdFirst
        ) {
            document.SerializeDocument(bsonWriter, nominalType, serializeIdFirst);
        }

        void IBsonSerializable.SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            bool useCompactRepresentation
        ) {
            document.SerializeElement(bsonWriter, nominalType, name, useCompactRepresentation);
        }
        #endregion
    }
}
