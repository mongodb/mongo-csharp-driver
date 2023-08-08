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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4744Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .GroupBy(x => x.FooName, (x, y) => new Summary()
                {
                    FooName = x,
                    Count = y.Count(x => x.State == State.Running)
                });

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(
                    stages,
                    "{ $group: { _id : '$FooName', Count : { $sum : { $cond : [{ $eq : ['$State', 1] }, 1, 0] } } } }"); // note: 1 instead of "Running" is an error
            }
            else
            {
                AssertStages(
                    stages,
                    "{ $group: { _id : '$FooName', __agg0 : { $sum : { $cond : { if : { $eq : ['$State', 'Running'] }, then : 1, else : 0 } } } } }",
                    "{ $project : { FooName : '$_id', Count : '$__agg0', _id : 0 } }");
            }
        }

        private IMongoCollection<Foo> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<Foo>("test", linqProvider);
            CreateCollection(collection);
            return collection;
        }

        public enum State
        {
            Started,
            Running,
            Complete
        }

        public class Foo
        {
            public string FooName;
            [BsonRepresentation(BsonType.String)]
            public State State;
        }

        public class Summary
        {
            public string FooName;
            public int Count;
        }
    }
}
