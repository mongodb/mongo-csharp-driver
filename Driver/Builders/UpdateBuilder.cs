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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Builders {
    public static class Update {
        #region public static methods
        public static UpdateBuilder AddToSet(
            string name,
            BsonValue value
        ) {
            return new UpdateBuilder().AddToSet(name, value);
        }

        public static UpdateBuilder AddToSetEach(
            string name,
            params BsonValue[] values
        ) {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        public static UpdateBuilder Inc(
            string name,
            double value
        ) {
            return new UpdateBuilder().Inc(name, value);
        }

        public static UpdateBuilder Inc(
            string name,
            int value
        ) {
            return new UpdateBuilder().Inc(name, value);
        }

        public static UpdateBuilder Inc(
            string name,
            long value
        ) {
            return new UpdateBuilder().Inc(name, value);
        }

        public static UpdateBuilder PopFirst(
            string name
        ) {
            return new UpdateBuilder().PopFirst(name);
        }

        public static UpdateBuilder PopLast(
            string name
        ) {
            return new UpdateBuilder().PopLast(name);
        }

        public static UpdateBuilder Pull(
            string name,
            BsonValue value
        ) {
            return new UpdateBuilder().Pull(name, value);
        }

        public static UpdateBuilder PullAll(
            string name,
            params BsonValue[] values
        ) {
            return new UpdateBuilder().PullAll(name, values);
        }

        public static UpdateBuilder Push(
            string name,
            BsonValue value
        ) {
            return new UpdateBuilder().Push(name, value);
        }

        public static UpdateBuilder PushAll(
            string name,
            params BsonValue[] values
        ) {
            return new UpdateBuilder().PushAll(name, values);
        }

        public static UpdateBuilder Set(
            string name,
            BsonValue value
        ) {
            return new UpdateBuilder().Set(name, value);
        }

        public static UpdateBuilder Unset(
            string name
        ) {
            return new UpdateBuilder().Unset(name);
        }
        #endregion
    }

    [Serializable]
    public class UpdateBuilder : BuilderBase {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public UpdateBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public UpdateBuilder AddToSet(
            string name,
            BsonValue value
        ) {
            BsonElement element;
            if (document.TryGetElement("$addToSet", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$addToSet", new BsonDocument(name, value));
            }
            return this;
        }

        public UpdateBuilder AddToSetEach(
            string name,
            params BsonValue[] values
        ) {
            var arg = new BsonDocument("$each", new BsonArray((IEnumerable<BsonValue>) values));
            BsonElement element;
            if (document.TryGetElement("$addToSet", out element)) {
                element.Value.AsBsonDocument.Add(name, arg);
            } else {
                document.Add("$addToSet", new BsonDocument(name, arg));
            }
            return this;
        }

        public UpdateBuilder Inc(
            string name,
            double value
        ) {
            BsonElement element;
            if (document.TryGetElement("$inc", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$inc", new BsonDocument(name, value));
            }
            return this;
        }

        public UpdateBuilder Inc(
            string name,
            int value
        ) {
            BsonElement element;
            if (document.TryGetElement("$inc", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$inc", new BsonDocument(name, value));
            }
            return this;
        }

        public UpdateBuilder Inc(
            string name,
            long value
        ) {
            BsonElement element;
            if (document.TryGetElement("$inc", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$inc", new BsonDocument(name, value));
            }
            return this;
        }

        public UpdateBuilder PopFirst(
            string name
        ) {
            BsonElement element;
            if (document.TryGetElement("$pop", out element)) {
                element.Value.AsBsonDocument.Add(name, 1);
            } else {
                document.Add("$pop", new BsonDocument(name, 1));
            }
            return this;
        }

        public UpdateBuilder PopLast(
            string name
        ) {
            BsonElement element;
            if (document.TryGetElement("$pop", out element)) {
                element.Value.AsBsonDocument.Add(name, -1);
            } else {
                document.Add("$pop", new BsonDocument(name, -1));
            }
            return this;
        }

        public UpdateBuilder Pull(
            string name,
            BsonValue value
        ) {
            BsonElement element;
            if (document.TryGetElement("$pull", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$pull", new BsonDocument(name, value));
            }
            return this;
        }

        public UpdateBuilder PullAll(
            string name,
            params BsonValue[] values
        ) {
            var array = new BsonArray((IEnumerable<BsonValue>) values);
            BsonElement element;
            if (document.TryGetElement("$pullAll", out element)) {
                element.Value.AsBsonDocument.Add(name, array);
            } else {
                document.Add("$pullAll", new BsonDocument(name, array));
            }
            return this;
        }

        public UpdateBuilder Push(
            string name,
            BsonValue value
        ) {
            BsonElement element;
            if (document.TryGetElement("$push", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$push", new BsonDocument(name, value));
            }
            return this;
        }

        public UpdateBuilder PushAll(
            string name,
            params BsonValue[] values
        ) {
            var array = new BsonArray((IEnumerable<BsonValue>) values);
            BsonElement element;
            if (document.TryGetElement("$pushAll", out element)) {
                element.Value.AsBsonDocument.Add(name, array);
            } else {
                document.Add("$pushAll", new BsonDocument(name, array));
            }
            return this;
        }

        public UpdateBuilder Set(
            string name,
            BsonValue value
        ) {
            BsonElement element;
            if (document.TryGetElement("$set", out element)) {
                element.Value.AsBsonDocument.Add(name, value);
            } else {
                document.Add("$set", new BsonDocument(name, value));
            }
            return this;
        }

        public override BsonDocument ToBsonDocument() {
            return document;
        }

        public UpdateBuilder Unset(
            string name
        ) {
            BsonElement element;
            if (document.TryGetElement("$unset", out element)) {
                element.Value.AsBsonDocument.Add(name, 1);
            } else {
                document.Add("$unset", new BsonDocument(name, 1));
            }
            return this;
        }
        #endregion

        #region protected methods
        protected override void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            bool serializeIdFirst
        ) {
            document.SerializeDocument(bsonWriter, nominalType, serializeIdFirst);
        }

        protected override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name
        ) {
            document.SerializeElement(bsonWriter, nominalType, name);
        }
        #endregion
    }
}
