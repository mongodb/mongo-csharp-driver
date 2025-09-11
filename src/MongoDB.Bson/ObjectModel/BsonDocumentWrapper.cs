﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private readonly object _wrapped;
        private readonly IBsonSerializer _serializer;
        private readonly IBsonSerializationDomain _serializationDomain;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonDocumentWrapper(object value)
            : this(value, UndiscriminatedActualTypeSerializer<object>.Instance, BsonSerializer.DefaultSerializationDomain)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        public BsonDocumentWrapper(object value, IBsonSerializer serializer)
            : this(value, serializer, BsonSerializer.DefaultSerializationDomain)
        {
        }

        internal BsonDocumentWrapper(object value, IBsonSerializer serializer, IBsonSerializationDomain serializationDomain)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serializationDomain = serializationDomain;
            _wrapped = value;
        }

        // public properties
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

        // DOMAIN-API All the various Create methods are used only in testing, the version without the domain should be removed.
        // public static methods
        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped object.</typeparam>
        /// <param name="value">The wrapped object.</param>
        /// <returns>A BsonDocumentWrapper.</returns>
        public static BsonDocumentWrapper Create<TNominalType>(TNominalType value) =>
            Create(value, BsonSerializer.DefaultSerializationDomain);

        internal static BsonDocumentWrapper Create<TNominalType>(TNominalType value, IBsonSerializationDomain serializationDomain)
        {
            return Create(typeof(TNominalType), value, serializationDomain);
        }

        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="value">The wrapped object.</param>
        /// <returns>A BsonDocumentWrapper.</returns>
        public static BsonDocumentWrapper Create(Type nominalType, object value) =>
            Create(nominalType, value, BsonSerializer.DefaultSerializationDomain);

        internal static BsonDocumentWrapper Create(Type nominalType, object value, IBsonSerializationDomain domain)
        {
            var serializer = domain.LookupSerializer(nominalType);
            return new BsonDocumentWrapper(value, serializer, domain);
        }

        /// <summary>
        /// Creates a list of new instances of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped objects.</typeparam>
        /// <param name="values">A list of wrapped objects.</param>
        /// <returns>A list of BsonDocumentWrappers.</returns>
        public static IEnumerable<BsonDocumentWrapper> CreateMultiple<TNominalType>(IEnumerable<TNominalType> values) =>
            CreateMultiple(values, BsonSerializer.DefaultSerializationDomain);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="values"></param>
        /// <param name="domain"></param>
        /// <typeparam name="TNominalType"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static IEnumerable<BsonDocumentWrapper> CreateMultiple<TNominalType>(IEnumerable<TNominalType> values, IBsonSerializationDomain domain)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializer = domain.LookupSerializer(typeof(TNominalType));
            return values.Select(v => new BsonDocumentWrapper(v, serializer, domain));
        }

        /// <summary>
        /// Creates a list of new instances of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="values">A list of wrapped objects.</param>
        /// <returns>A list of BsonDocumentWrappers.</returns>
        public static IEnumerable<BsonDocumentWrapper> CreateMultiple(Type nominalType, IEnumerable values) =>
            CreateMultiple(nominalType, values, BsonSerializer.DefaultSerializationDomain);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="nominalType"></param>
        /// <param name="values"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static IEnumerable<BsonDocumentWrapper> CreateMultiple(Type nominalType, IEnumerable values, IBsonSerializationDomain domain)
        {
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializer = domain.LookupSerializer(nominalType);
            return values.Cast<object>().Select(v => new BsonDocumentWrapper(v, serializer, domain));
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
                    _serializationDomain);
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
                var context = BsonSerializationContext.CreateRoot(bsonWriter, _serializationDomain);
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
