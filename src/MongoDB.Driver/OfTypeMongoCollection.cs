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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    internal class OfTypeMongoCollection<TRootDocument, TDerivedDocument> : FilteredMongoCollectionBase<TDerivedDocument>
        where TDerivedDocument : TRootDocument
    {
        // private fields
        private readonly IMongoCollection<TRootDocument> _rootDocumentCollection;
        private readonly FilterDefinition<TDerivedDocument> _ofTypeFilter;
        private readonly FilterDefinition<TDerivedDocument> _additionalFilter;

        // constructors
        public OfTypeMongoCollection(
            IMongoCollection<TRootDocument> rootDocumentCollection,
            IMongoCollection<TDerivedDocument> derivedDocumentCollection,
            FilterDefinition<TDerivedDocument> ofTypeFilter,
            FilterDefinition<TDerivedDocument> additionalFilter = null)
            : base(derivedDocumentCollection, additionalFilter == null ? ofTypeFilter : ofTypeFilter & additionalFilter)
        {
            _rootDocumentCollection = rootDocumentCollection;
            _ofTypeFilter = ofTypeFilter;
            _additionalFilter = additionalFilter;
        }

        // public methods
        public override IFilteredMongoCollection<TMoreDerivedDocument> OfType<TMoreDerivedDocument>()
        {
            var ofTypeCollection = _rootDocumentCollection.OfType<TMoreDerivedDocument>();
            if (_additionalFilter == null)
            {
                return ofTypeCollection;
            }

            var renderedAdditionalFilter = _additionalFilter.Render(DocumentSerializer, Settings.SerializerRegistry, Database.Client.Settings.LinqProvider);
            var additionalFilter = new BsonDocumentFilterDefinition<TMoreDerivedDocument>(renderedAdditionalFilter);
            return ofTypeCollection.WithFilter(additionalFilter);
        }

        public override IFilteredMongoCollection<TDerivedDocument> WithFilter(FilterDefinition<TDerivedDocument> filter)
        {
            if (_additionalFilter != null)
            {
                filter = _additionalFilter & filter;
            }

            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection, _ofTypeFilter, filter);
        }

        public override IMongoCollection<TDerivedDocument> WithReadConcern(ReadConcern readConcern)
        {
            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection.WithReadConcern(readConcern), _ofTypeFilter, _additionalFilter);
        }

        public override IMongoCollection<TDerivedDocument> WithReadPreference(ReadPreference readPreference)
        {
            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection.WithReadPreference(readPreference), _ofTypeFilter, _additionalFilter);
        }

        public override IMongoCollection<TDerivedDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            return new OfTypeMongoCollection<TRootDocument, TDerivedDocument>(_rootDocumentCollection, WrappedCollection.WithWriteConcern(writeConcern), _ofTypeFilter, _additionalFilter);
        }

        protected override UpdateDefinition<TDerivedDocument> AdjustUpdateDefinition(UpdateDefinition<TDerivedDocument> updateDefinition, bool isUpsert)
        {
            var result = base.AdjustUpdateDefinition(updateDefinition, isUpsert);

            if (isUpsert)
            {
                var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(TDerivedDocument));
                var discriminatorValue = discriminatorConvention.GetDiscriminator(typeof(TRootDocument), typeof(TDerivedDocument));

                var builder = new UpdateDefinitionBuilder<TDerivedDocument>();
                var setOnInsertDiscriminator = builder.SetOnInsert(discriminatorConvention.ElementName, discriminatorValue);
                result = builder.Combine(result, setOnInsertDiscriminator);
            }

            return result;
        }
    }
}
