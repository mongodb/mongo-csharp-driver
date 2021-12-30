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

namespace MongoDB.Driver
{
    internal class FilteredMongoCollection<TDocument> : FilteredMongoCollectionBase<TDocument>
    {
        // constructors
        public FilteredMongoCollection(
            IMongoCollection<TDocument> wrappedCollection,
            FilterDefinition<TDocument> filter)
            : base(wrappedCollection, filter)
        {
        }

        // public methods
        public override IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>()
        {
            var renderedFilter = Filter.Render(DocumentSerializer, Settings.SerializerRegistry, Database.Client.Settings.LinqProvider);
            var filter = new BsonDocumentFilterDefinition<TDerivedDocument>(renderedFilter);
            return WrappedCollection.OfType<TDerivedDocument>().WithFilter(filter);
        }

        public override IFilteredMongoCollection<TDocument> WithFilter(FilterDefinition<TDocument> filter)
        {
            return WrappedCollection.WithFilter(CombineFilters(filter));
        }

        public override IMongoCollection<TDocument> WithReadConcern(ReadConcern readConcern)
        {
            return new FilteredMongoCollection<TDocument>(WrappedCollection.WithReadConcern(readConcern), Filter);
        }

        public override IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference)
        {
            return new FilteredMongoCollection<TDocument>(WrappedCollection.WithReadPreference(readPreference), Filter);
        }

        public override IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern)
        {
            return new FilteredMongoCollection<TDocument>(WrappedCollection.WithWriteConcern(writeConcern), Filter);
        }
    }
}
