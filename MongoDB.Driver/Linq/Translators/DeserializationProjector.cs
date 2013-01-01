/* Copyright 2010-2013 10gen Inc.
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

using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a projection that deserializes BsonValues.
    /// </summary>
    /// <typeparam name="TResult">The type of the result objects.</typeparam>
    public class DeserializationProjector<TResult> : IEnumerable<TResult>
    {
        // private fields
        private IEnumerable<BsonValue> _source;
        private BsonSerializationInfo _serializationInfo;

        // constructors
        /// <summary>
        /// Initializes a new instance of the DeserializationProjector class.
        /// </summary>
        /// <param name="source">The enumerable object that supplies the source objects.</param>
        /// <param name="serializationInfo">Serialization info for deserializing source objects into result objects.</param>
        public DeserializationProjector(IEnumerable<BsonValue> source, BsonSerializationInfo serializationInfo)
        {
            _source = source;
            _serializationInfo = serializationInfo;
        }

        // public methods
        /// <summary>
        /// Gets an enumerator for the result objects.
        /// </summary>
        /// <returns>An enumerator for the result objects.</returns>
        public IEnumerator<TResult> GetEnumerator()
        {
            foreach (var value in _source)
            {
                var document = new BsonDocument("_v", value);
                using (var bsonReader = BsonReader.Create(document))
                {
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadName("_v");
                    yield return (TResult)_serializationInfo.Serializer.Deserialize(bsonReader, _serializationInfo.NominalType, _serializationInfo.SerializationOptions);
                    bsonReader.ReadEndDocument();
                }
            }
        }

        // explicit interface implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
