/* Copyright 2016 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for the options used when creating a view.
    /// </summary>
    public static class CreateViewOptions
    {
        /// <summary>
        /// Sets the collation.
        /// </summary>
        /// <param name="collation">The collation.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CreateViewOptionsBuilder SetCollation(Collation collation)
        {
            return new CreateViewOptionsBuilder().SetCollation(collation);
        }
    }

    /// <summary>
    /// A builder for the options used when creating a view.
    /// </summary>
    [BsonSerializer(typeof(CreateViewOptionsBuilder.Serializer))]
    public class CreateViewOptionsBuilder : BuilderBase, IMongoCreateViewOptions
    {
        private BsonDocument _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateViewOptionsBuilder"/> class.
        /// </summary>
        public CreateViewOptionsBuilder()
        {
            _document = new BsonDocument();
        }

        /// <summary>
        /// Sets the collation.
        /// </summary>
        /// <param name="collation">The collation.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CreateViewOptionsBuilder SetCollation(Collation collation)
        {
            if (collation == null)
            {
                _document.Remove("collation");
            }
            else
            {
                _document["collation"] = collation.ToBsonDocument();
            }

            return this;
        }

        /// <inheritdoc/>
         public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // nested classes
        new internal class Serializer : SerializerBase<CreateViewOptionsBuilder>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CreateViewOptionsBuilder value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._document);
            }
        }
    }
}
