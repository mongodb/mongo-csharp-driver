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
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        public static UpdateBuilder AddToSetEach(
            string name,
            params BsonValue[] values
        ) {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        public static UpdateBuilder AddToSetEachWrapped<T>(
            string name,
            IEnumerable<T> values
        ) {
            return new UpdateBuilder().AddToSetEachWrapped<T>(name, values);
        }

        public static UpdateBuilder AddToSetEachWrapped<T>(
            string name,
            params T[] values
        ) {
            return new UpdateBuilder().AddToSetEachWrapped<T>(name, values);
        }

        public static UpdateBuilder AddToSetWrapped<T>(
            string name,
            T value
        ) {
            return new UpdateBuilder().AddToSetWrapped<T>(name, value);
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

        public static UpdateBuilder Pull(
            string name,
            IMongoQuery query
        ) {
            return new UpdateBuilder().Pull(name, query);
        }

        public static UpdateBuilder PullAll(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().PullAll(name, values);
        }

        public static UpdateBuilder PullAll(
            string name,
            params BsonValue[] values
        ) {
            return new UpdateBuilder().PullAll(name, values);
        }

        public static UpdateBuilder PullAllWrapped<T>(
            string name,
            IEnumerable<T> values
        ) {
            return new UpdateBuilder().PullAllWrapped<T>(name, values);
        }

        public static UpdateBuilder PullAllWrapped<T>(
            string name,
            params T[] values
        ) {
            return new UpdateBuilder().PullAllWrapped<T>(name, values);
        }

        public static UpdateBuilder PullWrapped<T>(
            string name,
            T value
        ) {
            return new UpdateBuilder().PullWrapped<T>(name, value);
        }

        public static UpdateBuilder Push(
            string name,
            BsonValue value
        ) {
            return new UpdateBuilder().Push(name, value);
        }

        public static UpdateBuilder PushAll(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().PushAll(name, values);
        }

        public static UpdateBuilder PushAll(
            string name,
            params BsonValue[] values
        ) {
            return new UpdateBuilder().PushAll(name, values);
        }

        public static UpdateBuilder PushAllWrapped<T>(
            string name,
            IEnumerable<T> values
        ) {
            return new UpdateBuilder().PushAllWrapped<T>(name, values);
        }

        public static UpdateBuilder PushAllWrapped<T>(
            string name,
            params T[] values
        ) {
            return new UpdateBuilder().PushAllWrapped<T>(name, values);
        }

        public static UpdateBuilder PushWrapped<T>(
            string name,
            T value
        ) {
            return new UpdateBuilder().PushWrapped<T>(name, value);
        }

        public static IMongoUpdate Rename(
            string oldElementName,
            string newElementName
        ) {
            return new UpdateDocument("$rename", new BsonDocument(oldElementName, newElementName));
        }

        // similar to wrap but used when a full document replacement is wanted (<T> allows control over discriminator)
        public static IMongoUpdate Replace<T>(
            T document
        ) {
            return UpdateWrapper.Create<T>(document);
        }

        public static UpdateBuilder Set(
            string name,
            BsonValue value
        ) {
            return new UpdateBuilder().Set(name, value);
        }

        public static UpdateBuilder SetWrapped<T>(
            string name,
            T value
        ) {
            return new UpdateBuilder().SetWrapped<T>(name, value);
        }

        public static UpdateBuilder Unset(
            string name
        ) {
            return new UpdateBuilder().Unset(name);
        }

        // use Replace when update is a complete document replacement
        public static IMongoUpdate Wrap(
            object update
        ) {
            return UpdateWrapper.Create(update);
        }
        #endregion
    }

    [Serializable]
    public class UpdateBuilder : BuilderBase, IMongoUpdate {
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
            IEnumerable<BsonValue> values
        ) {
            var arg = new BsonDocument("$each", new BsonArray(values));
            BsonElement element;
            if (document.TryGetElement("$addToSet", out element)) {
                element.Value.AsBsonDocument.Add(name, arg);
            } else {
                document.Add("$addToSet", new BsonDocument(name, arg));
            }
            return this;
        }

        public UpdateBuilder AddToSetEach(
            string name,
            params BsonValue[] values
        ) {
            return AddToSetEach(name, (IEnumerable<BsonValue>) values);
        }

        public UpdateBuilder AddToSetEachWrapped<T>(
            string name,
            IEnumerable<T> values
        ) {
            var wrappedValues = BsonDocument.WrapMultiple<T>(values).Cast<BsonValue>(); // the cast to BsonValue is required
            return AddToSetEach(name, wrappedValues);
        }

        public UpdateBuilder AddToSetEachWrapped<T>(
            string name,
            params T[] values
        ) {
            return AddToSetEachWrapped(name, (IEnumerable<T>) values);
        }

        public UpdateBuilder AddToSetWrapped<T>(
            string name,
            T value
        ) {
            var wrappedValue = (BsonValue) BsonDocument.Wrap<T>(value); // the cast to BsonValue is required
            return AddToSet(name, wrappedValue);
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

        public UpdateBuilder Pull(
            string name,
            IMongoQuery query
        ) {
            BsonValue wrappedQuery = BsonDocument.Wrap(query);
            BsonElement element;
            if (document.TryGetElement("$pull", out element)) {
                element.Value.AsBsonDocument.Add(name, wrappedQuery);
            } else {
                document.Add("$pull", new BsonDocument(name, wrappedQuery));
            }
            return this;
        }

        public UpdateBuilder PullAll(
            string name,
            IEnumerable<BsonValue> values
        ) {
            var array = new BsonArray(values);
            BsonElement element;
            if (document.TryGetElement("$pullAll", out element)) {
                element.Value.AsBsonDocument.Add(name, array);
            } else {
                document.Add("$pullAll", new BsonDocument(name, array));
            }
            return this;
        }

        public UpdateBuilder PullAll(
            string name,
            params BsonValue[] values
        ) {
            return PullAll(name, (IEnumerable<BsonValue>) values);
        }

        public UpdateBuilder PullAllWrapped<T>(
            string name,
            IEnumerable<T> values
        ) {
            var wrappedValues = new BsonArray(BsonDocument.WrapMultiple(values).Cast<BsonValue>()); // the cast to BsonValue is required
            BsonElement element;
            if (document.TryGetElement("$pullAll", out element)) {
                element.Value.AsBsonDocument.Add(name, wrappedValues);
            } else {
                document.Add("$pullAll", new BsonDocument(name, wrappedValues));
            }
            return this;
        }

        public UpdateBuilder PullAllWrapped<T>(
            string name,
            params T[] values
        ) {
            return PullAllWrapped<T>(name, (IEnumerable<T>) values);
        }

        public UpdateBuilder PullWrapped<T>(
            string name,
            T value
        ) {
            var wrappedValue = BsonDocument.Wrap(value);
            BsonElement element;
            if (document.TryGetElement("$pull", out element)) {
                element.Value.AsBsonDocument.Add(name, wrappedValue);
            } else {
                document.Add("$pull", new BsonDocument(name, wrappedValue));
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
            IEnumerable<BsonValue> values
        ) {
            var array = new BsonArray(values);
            BsonElement element;
            if (document.TryGetElement("$pushAll", out element)) {
                element.Value.AsBsonDocument.Add(name, array);
            } else {
                document.Add("$pushAll", new BsonDocument(name, array));
            }
            return this;
        }

        public UpdateBuilder PushAll(
            string name,
            params BsonValue[] values
        ) {
            return PushAll(name, (IEnumerable<BsonValue>) values);
        }

        public UpdateBuilder PushAllWrapped<T>(
            string name,
            IEnumerable<T> values
        ) {
            var wrappedValues = new BsonArray(BsonDocument.WrapMultiple<T>(values).Cast<BsonValue>()); // the cast to BsonValue is required
            BsonElement element;
            if (document.TryGetElement("$pushAll", out element)) {
                element.Value.AsBsonDocument.Add(name, wrappedValues);
            } else {
                document.Add("$pushAll", new BsonDocument(name, wrappedValues));
            }
            return this;
        }

        public UpdateBuilder PushAllWrapped<T>(
            string name,
            params T[] values
        ) {
            return PushAllWrapped(name, (IEnumerable<T>) values);
        }

        public UpdateBuilder PushWrapped<T>(
            string name,
            T value
        ) {
            var wrappedValue = BsonDocument.Wrap<T>(value);
            BsonElement element;
            if (document.TryGetElement("$push", out element)) {
                element.Value.AsBsonDocument.Add(name, wrappedValue);
            } else {
                document.Add("$push", new BsonDocument(name, wrappedValue));
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

        public UpdateBuilder SetWrapped<T>(
            string name,
            T value
        ) {
            var wrappedValue = BsonDocument.Wrap<T>(value);
            BsonElement element;
            if (document.TryGetElement("$set", out element)) {
                element.Value.AsBsonDocument.Add(name, wrappedValue);
            } else {
                document.Add("$set", new BsonDocument(name, wrappedValue));
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
        protected override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            document.Serialize(bsonWriter, nominalType, options);
        }
        #endregion
    }
}
