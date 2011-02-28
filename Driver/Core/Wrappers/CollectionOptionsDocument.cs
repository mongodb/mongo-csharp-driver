﻿/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    public class CollectionOptionsDocument : BsonDocument, IMongoCollectionOptions {
        #region constructors
        public CollectionOptionsDocument() {
        }

        public CollectionOptionsDocument(
            bool allowDuplicateNames
        )
            : base(allowDuplicateNames) {
        }

        public CollectionOptionsDocument(
            BsonElement element
        )
            : base(element) {
        }

        public CollectionOptionsDocument(
            IDictionary<string, object> dictionary
        )
            : base(dictionary) {
        }

        public CollectionOptionsDocument(
            IDictionary<string, object> dictionary,
            IEnumerable<string> keys
        )
            : base(dictionary, keys) {
        }

        public CollectionOptionsDocument(
            IEnumerable<BsonElement> elements
        )
            : base(elements) {
        }

        public CollectionOptionsDocument(
            params BsonElement[] elements
        )
            : base(elements) {
        }

        public CollectionOptionsDocument(
            string name,
            BsonValue value
        )
            : base(name, value) {
        }
        #endregion
    }
}
