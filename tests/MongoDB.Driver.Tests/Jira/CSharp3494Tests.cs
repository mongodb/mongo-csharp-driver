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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3494Tests : Linq3IntegrationTest
    {
        //[BsonKnownTypes(typeof(DerivedDocument<int>), typeof(DerivedDocument<string>))]
        abstract class BaseDocument {}

        class DerivedDocument<T> : BaseDocument
        {
            [BsonId]
            public int Id { get; set; }

            public T Value { get; set; }
        }

        [Fact]
        public void Test1()
        {
            var doc1 = new DerivedDocument<int> { Id = 1, Value = 42 };
            var serialized1 = doc1.ToJson(typeof(BaseDocument));

            var baseCollection = GetCollection<BaseDocument>("AA","testBase");
            var untypedCollection = GetCollection<BsonDocument>("AA","testBase");

            baseCollection.DeleteMany(FilterDefinition<BaseDocument>.Empty);
            baseCollection.InsertOne(new DerivedDocument<int> { Id = 1, Value = 42 });
            baseCollection.InsertOne(new DerivedDocument<string> { Id = 2, Value = "42" });

            var filter = Builders<BsonDocument>.Filter.Eq("_id", 1);
            var filterInt = Builders<BaseDocument>.Filter.Eq("_id", 1);

            var untypedRetrievedDocument = untypedCollection.Find(filter).FirstOrDefault();
            var retrievedDocument = baseCollection.Find(filterInt).FirstOrDefault();

            retrievedDocument.Should().NotBeNull();
        }
    }
}