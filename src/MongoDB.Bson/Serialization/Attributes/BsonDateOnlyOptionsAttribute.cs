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
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Attributes
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// Specifies the external representation and related options for a DateOnly field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonDateOnlyOptionsAttribute : BsonSerializationOptionsAttribute
    {
        // private fields
        private BsonType _representation;
        private DateOnlyDocumentFormat _documentFormat;

        // constructors

        /// <summary>
        /// Initializes a new instance of the BsonDateOnlyOptionsAttribute class.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        /// <param name="documentDocumentFormat">The format to use with document representation.</param>
        public BsonDateOnlyOptionsAttribute(BsonType representation, DateOnlyDocumentFormat documentDocumentFormat = DateOnlyDocumentFormat.DateTimeTicks)
        {
            _representation = representation;
            _documentFormat = documentDocumentFormat;
        }

        // public properties
        /// <summary>
        /// Gets the external representation.
        /// </summary>
        public BsonType Representation => _representation;

        /// <summary>
        /// Gets the document format.
        /// </summary>
        public DateOnlyDocumentFormat DocumentFormat => _documentFormat;

        /// <summary>
        /// Reconfigures the specified serializer by applying this attribute to it.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>A reconfigured serializer.</returns>
        protected override IBsonSerializer Apply(IBsonSerializer serializer)
        {
            if (serializer is DateOnlySerializer dateOnlySerializer)
            {
                return dateOnlySerializer.WithRepresentation(_representation, _documentFormat);
            }

            return base.Apply(serializer);
        }
    }
#endif
}
