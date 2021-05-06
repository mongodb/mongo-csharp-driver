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

using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Linq.Linq3Implementation;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests
{
    public class MongoQueryProviderTests
    {
        // TODO: implement MongoQueryProviderTests
    }

    internal static class MongoQueryProviderExtensions
    {
        public static IMongoCollection<TDocument> _collection<TDocument>(this MongoQueryProvider<TDocument> provider)
            => (IMongoCollection<TDocument>)Reflector.GetFieldValue(provider, nameof(_collection));

        public static AggregateOptions _options(this MongoQueryProvider provider)
            => (AggregateOptions)Reflector.GetFieldValue(provider, nameof(_options));

        public static IClientSessionHandle _session(this MongoQueryProvider provider)
            => (IClientSessionHandle)Reflector.GetFieldValue(provider, nameof(_session));
    }
}
