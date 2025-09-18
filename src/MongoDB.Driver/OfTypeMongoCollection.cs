/* Copyright 2015-present MongoDB Inc.
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

namespace MongoDB.Driver
{
    internal class OfTypeMongoCollection<TRootDocument, TDerivedDocument> : FilteredMongoCollectionBase<TDerivedDocument>
        where TDerivedDocument : TRootDocument
    {
        // private fields
        private readonly IMongoCollection<TRootDocument> _rootDocumentCollection;

        // constructors
        public OfTypeMongoCollection(
            IMongoCollection<TRootDocument> rootDocumentCollection,
            IMongoCollection<TDerivedDocument> derivedDocumentCollection,
            FilterDefinition<TDerivedDocument> ofTypeFilter)
            : base(derivedDocumentCollection, ofTypeFilter)
        {
            _rootDocumentCollection = rootDocumentCollection;
        }

        // public methods
        public override IFilteredMongoCollection<TMoreDerivedDocument> OfType<TMoreDerivedDocument>()
        {
            return _rootDocumentCollection.OfType<TMoreDerivedDocument>();
        }

        public override IMongoCollection<TDerivedDocument> WithReadConcern(ReadConcern readConcern)
        {
            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection.WithReadConcern(readConcern), Filter);
        }

        public override IMongoCollection<TDerivedDocument> WithReadPreference(ReadPreference readPreference)
        {
            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection.WithReadPreference(readPreference), Filter);
        }

        public override IMongoCollection<TDerivedDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection.WithWriteConcern(writeConcern), Filter);
        }

        protected override UpdateDefinition<TDerivedDocument> AdjustUpdateDefinition(UpdateDefinition<TDerivedDocument> updateDefinition, bool isUpsert)
        {
            var result = base.AdjustUpdateDefinition(updateDefinition, isUpsert);

            if (isUpsert)
            {
                var discriminatorConvention = _rootDocumentCollection.DocumentSerializer.GetDiscriminatorConvention();
                var discriminatorConventionElementName = discriminatorConvention.ElementName;
                var discriminator = discriminatorConvention.GetDiscriminator(typeof(TRootDocument), typeof(TDerivedDocument));

                if (result is PipelineUpdateDefinition<TDerivedDocument> pipeline)
                {
                    var setOnInsertStage = new BsonDocument()
                    {
                        {
                            "$set",
                            new BsonDocument
                            {
                                {
                                    discriminatorConventionElementName, // target field
                                    new BsonDocument // condition
                                    {
                                        {
                                            "$cond",
                                            new BsonArray
                                            {
                                                new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$_id"), "missing" }), // if "_id" is missed
                                                discriminator, // then set targetField to discriminatorValue
                                                $"${discriminatorConventionElementName}" // else set targetField from the value in the document
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };
                    result = pipeline.Pipeline.AppendStage<TDerivedDocument, TDerivedDocument, TDerivedDocument>(setOnInsertStage);
                }
                else
                {
                    var builder = new UpdateDefinitionBuilder<TDerivedDocument>();
                    var setOnInsertDiscriminator = builder.SetOnInsert(discriminatorConventionElementName, discriminator);
                    result = builder.Combine(result, setOnInsertDiscriminator);
                }
            }

            return result;
        }
    }
}
