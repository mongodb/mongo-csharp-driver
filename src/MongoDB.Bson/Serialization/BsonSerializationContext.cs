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
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents all the contextual information needed by a serializer to serialize a value.
    /// </summary>
    public class BsonSerializationContext
    {
        // private fields
        private readonly Func<Type, bool> _isDynamicType;
        private readonly IBsonWriter _writer;
        private readonly IBsonSerializationDomain _serializationDomain;

        // constructors
        private BsonSerializationContext(
            IBsonWriter writer,
            IBsonSerializationDomain serializationDomain,
            Func<Type, bool> isDynamicType)
        {
            _writer = writer;
            _isDynamicType = isDynamicType;

            _serializationDomain = serializationDomain; //FP Using this version to find error in an easier way for now
            //_serializationDomain = serializationDomain ?? BsonSerializer.DefaultSerializationDomain;

            _isDynamicType??= t =>
                (_serializationDomain.BsonDefaults.DynamicArraySerializer != null && t == _serializationDomain.BsonDefaults.DynamicArraySerializer.ValueType) ||
                (_serializationDomain.BsonDefaults.DynamicDocumentSerializer != null && t == _serializationDomain.BsonDefaults.DynamicDocumentSerializer.ValueType);
        }

        // public properties
        /// <summary>
        /// //TODO
        /// </summary>
        internal IBsonSerializationDomain SerializationDomain => _serializationDomain;

        /// <summary>
        /// Gets a function that, when executed, will indicate whether the type
        /// is a dynamic type.
        /// </summary>
        public Func<Type, bool> IsDynamicType
        {
            get { return _isDynamicType; }
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        /// <value>
        /// The writer.
        /// </value>
        public IBsonWriter Writer
        {
            get { return _writer; }
        }

        //DOMAIN-API This method should be probably removed.
        // public static methods
        /// <summary>
        /// Creates a root context.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <returns>
        /// A root context.
        /// </returns>
        public static BsonSerializationContext CreateRoot(
            IBsonWriter writer,
            Action<Builder> configurator = null)
            => CreateRoot(writer, BsonSerializer.DefaultSerializationDomain, configurator);

        internal static BsonSerializationContext CreateRoot(
            IBsonWriter writer,
            IBsonSerializationDomain serializationDomain,
            Action<Builder> configurator = null)
        {
            var builder = new Builder(null, writer, serializationDomain);
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        /// <summary>
        /// Creates a new context with some values changed.
        /// </summary>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <returns>
        /// A new context.
        /// </returns>
        public BsonSerializationContext With(
            Action<Builder> configurator = null)
        {
            var builder = new Builder(this, _writer, _serializationDomain);
            if (configurator != null)
            {
                configurator(builder);
            }
            return builder.Build();
        }

        // nested classes
        /// <summary>
        /// Represents a builder for a BsonSerializationContext.
        /// </summary>
        public class Builder
        {
            // private fields
            private Func<Type, bool> _isDynamicType;
            private IBsonWriter _writer;
            private IBsonSerializationDomain _serializationDomain;

            // constructors
            internal Builder(BsonSerializationContext other, IBsonWriter writer, IBsonSerializationDomain serializationDomain)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException("writer");
                }

                _writer = writer;
                _serializationDomain = serializationDomain;
                if (other != null)
                {
                    _isDynamicType = other._isDynamicType;
                }

                /* QUESTION I removed the part where we set _isDynamicType from the BsonDefaults, and delay it until we have a serialization domain (when we build the SerializationContext).
                 * This is technically changing the public behaviour, but it's in a builder, I do not thing it will affect anyone. Same done for the deserialization context.
                 */
            }

            // properties
            /// <summary>
            /// Gets or sets the function used to determine if a type is a dynamic type.
            /// </summary>
            public Func<Type, bool> IsDynamicType
            {
                get { return _isDynamicType; }
                set { _isDynamicType = value; }
            }

            /// <summary>
            /// Gets the writer.
            /// </summary>
            /// <value>
            /// The writer.
            /// </value>
            public IBsonWriter Writer
            {
                get { return _writer; }
            }

            // public methods
            /// <summary>
            /// Builds the BsonSerializationContext instance.
            /// </summary>
            /// <returns>A BsonSerializationContext.</returns>
            internal BsonSerializationContext Build()
            {
                return new BsonSerializationContext(_writer, _serializationDomain, _isDynamicType);
            }
        }
    }
}
