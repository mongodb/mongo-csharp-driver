/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson
{
    // this class is a wrapper for an object that we intend to serialize as a BsonDocument
    // it is a subclass of BsonDocument so that it may be used where a BsonDocument is expected
    // this class is mostly used by MongoCollection and MongoCursor when supporting generic query objects

    // if all that ever happens with this wrapped object is that it gets serialized then the BsonDocument is never materialized

    /// <summary>
    /// Represents a BsonDocument wrapper.
    /// </summary>
    public class BsonDocumentWrapper : MaterializedOnDemandBsonDocument
    {
        // private fields
        private readonly IElementNameValidator _elementNameValidator;
        private readonly object _wrapped;
        private readonly IBsonSerializer _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonDocumentWrapper(object value)
            : this(value, UndiscriminatedActualTypeSerializer<object>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        public BsonDocumentWrapper(object value, IBsonSerializer serializer)
            : this(value, serializer, NoOpElementNameValidator.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="elementNameValidator">The element name validator.</param>
        /// <exception cref="System.ArgumentNullException">serializer</exception>
        public BsonDocumentWrapper(object value, IBsonSerializer serializer, IElementNameValidator elementNameValidator)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            _wrapped = value;
            _serializer = serializer;
            _elementNameValidator = elementNameValidator;
        }

        // public properties
        /// <summary>
        /// Gets the element name validator.
        /// </summary>
        public IElementNameValidator ElementNameValidator
        {
            get { return _elementNameValidator; }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <value>
        /// The serializer.
        /// </value>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public object Wrapped
        {
            get { return _wrapped; }
        }

        // public methods
        /// <summary>
        /// Creates a shallow clone of the document (see also DeepClone).
        /// </summary>
        /// <returns>
        /// A shallow clone of the document.
        /// </returns>
        public override BsonValue Clone()
        {
            if (IsMaterialized)
            {
                return base.Clone();
            }
            else
            {
                return new BsonDocumentWrapper(
                    _wrapped,
                    _serializer,
                    _elementNameValidator);
            }
        }

        // protected methods
        /// <summary>
        /// Materializes the BsonDocument.
        /// </summary>
        /// <returns>The materialized elements.</returns>
        protected override IEnumerable<BsonElement> Materialize()
        {
            var bsonDocument = new BsonDocument();
            var writerSettings = BsonDocumentWriterSettings.Defaults;
            using (var bsonWriter = new BsonDocumentWriter(bsonDocument, writerSettings))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter, _serializer.ValueType);
                _serializer.Serialize(context, _wrapped);
            }

            return bsonDocument.Elements;
        }

        /// <summary>
        /// Informs subclasses that the Materialize process completed so they can free any resources related to the unmaterialized state.
        /// </summary>
        protected override void MaterializeCompleted()
        {
        }
    }
}
