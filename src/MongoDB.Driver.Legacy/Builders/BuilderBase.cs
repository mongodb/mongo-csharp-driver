/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// Abstract base class for the builders.
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(BuilderBase.Serializer))]
    public abstract class BuilderBase : IConvertibleToBsonDocument
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the BuilderBase class.
        /// </summary>
        protected BuilderBase()
        {
        }

        // public methods
        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public abstract BsonDocument ToBsonDocument();

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            return this.ToJson(); // "this." required to access extension method
        }

        // explicit interface implementations
        BsonDocument IConvertibleToBsonDocument.ToBsonDocument()
        {
            return ToBsonDocument();
        }

        // nested classes
        internal class Serializer : UndiscriminatedActualTypeSerializer<BuilderBase>
        {
        }
    }
}
