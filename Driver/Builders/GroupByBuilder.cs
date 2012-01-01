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
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for specifying what the GroupBy command should group by.
    /// </summary>
    public static class GroupBy
    {
        // public static methods
        /// <summary>
        /// Sets a key function.
        /// </summary>
        /// <param name="keyFunction">The key function.</param>
        /// <returns>A BsonJavaScript.</returns>
        public static BsonJavaScript Function(BsonJavaScript keyFunction)
        {
            return keyFunction;
        }

        /// <summary>
        /// Sets one or more key names.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GroupByBuilder Keys(params string[] names)
        {
            return new GroupByBuilder(names);
        }
    }

    /// <summary>
    /// A builder for specifying what the GroupBy command should group by.
    /// </summary>
    [Serializable]
    public class GroupByBuilder : BuilderBase, IMongoGroupBy
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the GroupByBuilder class.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        public GroupByBuilder(string[] names)
        {
            _document = new BsonDocument();
            foreach (var name in names)
            {
                _document.Add(name, 1);
            }
        }

        // public methods
        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // explicit interface implementations
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected override void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            _document.Serialize(bsonWriter, nominalType, options);
        }
    }
}
