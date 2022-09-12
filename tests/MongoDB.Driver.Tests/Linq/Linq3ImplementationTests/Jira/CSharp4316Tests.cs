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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4316Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Value_and_HasValue_should_work_when_properties_on_Nullable_type()
        {
            var collection = CreateCollection();
            var matchStage = "{ $match : { 'ActualNullable' : { $ne : null } } }";
            var projectStage = "{ $project : { Id : '$_id', Value : '$ActualNullable', HasValue : { $ne : ['$ActualNullable', null] }, _id : 0 } }";

            var queryable = collection.AsQueryable()
                .Where(x => x.ActualNullable.HasValue)
                .Select(x => new { x.Id, x.ActualNullable.Value, x.ActualNullable.HasValue });

            var stages = Translate(collection, queryable);
            AssertStages(stages, matchStage, projectStage);

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(2);
        }

        [Fact]
        public void Value_and_HasValue_should_work_when_properties_not_on_Nullable_type()
        {
            var collection = CreateCollection();
            var matchStage = "{ $match : { 'OnlyLooksLikeNullable.HasValue' : true } }";
            var projectStage = "{ $project : { Id : '$_id', Value : '$OnlyLooksLikeNullable.Value', HasValue : '$OnlyLooksLikeNullable.HasValue', _id : 0 } }";

            var queryable = collection
                .AsQueryable()
                .Where(x => x.OnlyLooksLikeNullable.HasValue)
                .Select(x => new { x.Id, Value = x.OnlyLooksLikeNullable.Value, HasValue = x.OnlyLooksLikeNullable.HasValue });

            var stages = Translate(collection, queryable);
            AssertStages(stages, matchStage, projectStage);

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(1);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, OnlyLooksLikeNullable = new OnlyLooksLikeNullable { Value = "SomeValue", HasValue = true }, ActualNullable = null },
                new C { Id = 2, OnlyLooksLikeNullable = new OnlyLooksLikeNullable { Value = null, HasValue = false }, ActualNullable = true });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public OnlyLooksLikeNullable OnlyLooksLikeNullable { get; set; }
            public bool? ActualNullable { get; set; }
        }

        private class OnlyLooksLikeNullable
        {
            public string Value { get; set; }
            public bool HasValue { get; set; }
        }
    }
}
