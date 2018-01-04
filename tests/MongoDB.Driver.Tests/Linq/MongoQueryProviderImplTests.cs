/* Copyright 2017-present MongoDB Inc.
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

using System.Reflection;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver.Tests.Linq
{
    internal static class MongoQueryProviderImplReflector
    {
        public static IMongoCollection<TDocument> _collection<TDocument>(this MongoQueryProviderImpl<TDocument> obj)
        {
            var fieldInfo = typeof(MongoQueryProviderImpl<TDocument>).GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IMongoCollection<TDocument>)fieldInfo.GetValue(obj);
        }

        public static AggregateOptions _options<TDocument>(this MongoQueryProviderImpl<TDocument> obj)
        {
            var fieldInfo = typeof(MongoQueryProviderImpl<TDocument>).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
            return (AggregateOptions)fieldInfo.GetValue(obj);
        }
    }
}
