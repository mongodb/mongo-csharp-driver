/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a cached delay-resolved IBsonSerializer.
    /// </summary>
    internal sealed class SerializerHolder
    {
        // private fields
        private readonly Type _serializerType;
        private volatile IBsonSerializer _cachedSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the SerializerHolder structure.
        /// </summary>
        /// <param name="serializerType">The serializer type.</param>
        public SerializerHolder(Type serializerType)
        {
            _serializerType = serializerType;
            _cachedSerializer = null;
        }

        // public properties
        public IBsonSerializer Value
        {
            get
            {
                var serializer = _cachedSerializer;
                if (serializer == null)
                {
                    // it's possible but harmless for multiple threads to do the initial lookup at the same time
                    serializer = DiscriminatorSerializer.Create(_serializerType);
                    _cachedSerializer = serializer;
                }
                return serializer;
            }
        }
    }
}
