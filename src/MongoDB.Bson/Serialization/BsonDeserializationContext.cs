/* Copyright 2010-present MongoDB Inc.
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
using System.Net;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents all the contextual information needed by a serializer to deserialize a value.
    /// </summary>
    public class BsonDeserializationContext
    {
        // private fields
        private readonly bool _allowDuplicateElementNames;
        private readonly IBsonSerializer _dynamicArraySerializer;
        private readonly IBsonSerializer _dynamicDocumentSerializer;
        private readonly IBsonReader _reader;
        private readonly IBsonSerializationDomain _serializationDomain;

        // constructors
        private BsonDeserializationContext(
            IBsonReader reader,
            IBsonSerializationDomain serializationDomain,
            bool allowDuplicateElementNames,
            IBsonSerializer dynamicArraySerializer,
            IBsonSerializer dynamicDocumentSerializer)
        {
            _reader = reader;
            _allowDuplicateElementNames = allowDuplicateElementNames;
            _dynamicArraySerializer = dynamicArraySerializer;
            _dynamicDocumentSerializer = dynamicDocumentSerializer;

            _serializationDomain = serializationDomain; //FP Using this version to find error in an easier way for now
            //_serializationDomain = serializationDomain ?? BsonSerializer.DefaultSerializationDomain;

            _dynamicArraySerializer ??= _serializationDomain.BsonDefaults.DynamicArraySerializer;
            _dynamicDocumentSerializer ??= _serializationDomain.BsonDefaults.DynamicDocumentSerializer;
        }

        // public properties
        /// <summary>
        /// Gets a value indicating whether to allow duplicate element names.
        /// </summary>
        /// <value>
        /// <c>true</c> if duplicate element names shoud be allowed; otherwise, <c>false</c>.
        /// </value>
        public bool AllowDuplicateElementNames
        {
            get { return _allowDuplicateElementNames; }
        }

        /// <summary>
        /// //TODO
        /// </summary>
        internal IBsonSerializationDomain SerializationDomain => _serializationDomain;

        /// <summary>
        /// Gets the dynamic array serializer.
        /// </summary>
        /// <value>
        /// The dynamic array serializer.
        /// </value>
        public IBsonSerializer DynamicArraySerializer
        {
            get { return _dynamicArraySerializer; }
        }

        /// <summary>
        /// Gets the dynamic document serializer.
        /// </summary>
        /// <value>
        /// The dynamic document serializer.
        /// </value>
        public IBsonSerializer DynamicDocumentSerializer
        {
            get { return _dynamicDocumentSerializer; }
        }

        /// <summary>
        /// Gets the reader.
        /// </summary>
        /// <value>
        /// The reader.
        /// </value>
        public IBsonReader Reader
        {
            get { return _reader; }
        }

        // //DOMAIN-API We should remove this version of the CreateRoot method, and use the one that takes a serialization domain.
        // public static methods
        /// <summary>
        /// Creates a root context.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// A root context.
        /// </returns>
        public static BsonDeserializationContext CreateRoot(
            IBsonReader reader,
            Action<Builder> configurator = null)
            => CreateRoot(reader, BsonSerializer.DefaultSerializationDomain, configurator);

        internal static BsonDeserializationContext CreateRoot(
            IBsonReader reader,
            IBsonSerializationDomain serializationDomain,
            Action<Builder> configurator = null)
        {
            var builder = new Builder(null, reader, serializationDomain);
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        // public methods
        /// <summary>
        /// Creates a new context with some values changed.
        /// </summary>
        /// <param name="configurator">The configurator.</param>
        /// <returns>
        /// A new context.
        /// </returns>
        public BsonDeserializationContext With(
            Action<Builder> configurator = null)
        {
            var builder = new Builder(this, _reader, _serializationDomain);
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        // nested classes
        /// <summary>
        /// Represents a builder for a BsonDeserializationContext.
        /// </summary>
        public class Builder
        {
            // private fields
            private bool _allowDuplicateElementNames;
            private IBsonSerializer _dynamicArraySerializer;
            private IBsonSerializer _dynamicDocumentSerializer;
            private IBsonReader _reader;
            private IBsonSerializationDomain _serializationDomain;

            // constructors
            internal Builder(BsonDeserializationContext other, IBsonReader reader, IBsonSerializationDomain serializationDomain)
            {
                if (reader == null)
                {
                    throw new ArgumentNullException("reader");
                }

                _reader = reader;
                _serializationDomain = serializationDomain;
                if (other != null)
                {
                    _allowDuplicateElementNames = other.AllowDuplicateElementNames;
                    _dynamicArraySerializer = other.DynamicArraySerializer;
                    _dynamicDocumentSerializer = other.DynamicDocumentSerializer;
                }

                /* QUESTION I removed the part where we set the dynamic serializers from the BsonDefaults, and delay it until we have a serialization domain (when we build the DeserializationContext).
                 * This is technically changing the public behaviour, but it's in a builder, I do not thing it will affect anyone. Same done for the serialization context.
                 */
            }

            // properties
            /// <summary>
            /// Gets or sets a value indicating whether to allow duplicate element names.
            /// </summary>
            /// <value>
            /// <c>true</c> if duplicate element names should be allowed; otherwise, <c>false</c>.
            /// </value>
            public bool AllowDuplicateElementNames
            {
                get { return _allowDuplicateElementNames; }
                set { _allowDuplicateElementNames = value; }
            }

            /// <summary>
            /// Gets or sets the dynamic array serializer.
            /// </summary>
            /// <value>
            /// The dynamic array serializer.
            /// </value>
            public IBsonSerializer DynamicArraySerializer
            {
                get { return _dynamicArraySerializer; }
                set { _dynamicArraySerializer = value; }
            }

            /// <summary>
            /// Gets or sets the dynamic document serializer.
            /// </summary>
            /// <value>
            /// The dynamic document serializer.
            /// </value>
            public IBsonSerializer DynamicDocumentSerializer
            {
                get { return _dynamicDocumentSerializer; }
                set { _dynamicDocumentSerializer = value; }
            }

            /// <summary>
            /// Gets the reader.
            /// </summary>
            /// <value>
            /// The reader.
            /// </value>
            public IBsonReader Reader
            {
                get { return _reader; }
            }

            internal IBsonSerializationDomain SerializationDomain => _serializationDomain;

            // public methods
            /// <summary>
            /// Builds the BsonDeserializationContext instance.
            /// </summary>
            /// <returns>A BsonDeserializationContext.</returns>
            internal BsonDeserializationContext Build()
            {
                return new BsonDeserializationContext(_reader, _serializationDomain, _allowDuplicateElementNames, _dynamicArraySerializer, _dynamicDocumentSerializer);
            }
        }
    }
}
