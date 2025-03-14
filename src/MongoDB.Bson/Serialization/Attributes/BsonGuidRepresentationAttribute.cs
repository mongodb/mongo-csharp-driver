﻿/* Copyright 2019-present MongoDB Inc.
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

using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Bson.Serialization.Attributes
{
    /// <summary>
    /// Specifies the Guid representation to use with the GuidSerializer for this member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonGuidRepresentationAttribute : BsonSerializationOptionsAttribute
    {
        // private fields
        private readonly GuidRepresentation _guidRepresentation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonGuidRepresentationAttribute class.
        /// </summary>
        /// <param name="guidRepresentation">The Guid representation.</param>
        public BsonGuidRepresentationAttribute(GuidRepresentation guidRepresentation)
        {
            _guidRepresentation = guidRepresentation;
        }

        // public properties
        /// <summary>
        /// Gets the Guid representation.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
        }

        // protected methods
        /// <inheritdoc/>
        protected override IBsonSerializer Apply(IBsonSerializer serializer)
        {
            if (serializer is GuidSerializer guidSerializer)
            {
                return guidSerializer.WithGuidRepresentation(_guidRepresentation);
            }

            return base.Apply(serializer);
        }
    }
}
