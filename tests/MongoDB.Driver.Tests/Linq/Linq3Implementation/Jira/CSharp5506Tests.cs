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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5506Tests : LinqIntegrationTest<CSharp5506Tests.ClassFixture>
{
    static CSharp5506Tests()
    {
        var keySerializer = new GuidSerializer(BsonType.String);
        var valueSerializer = Int32Serializer.Instance;
        var dictionarySerializer = new DictionaryInterfaceImplementerSerializer<Dictionary<Guid, int>>(
            DictionaryRepresentation.Document,
            keySerializer,
            valueSerializer);

        BsonClassMap.RegisterClassMap<C>(cm =>
        {
            cm.AutoMap();
            cm.MapMember(x => x.Dictionary).SetSerializer(dictionarySerializer);
        });
    }

    public CSharp5506Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Where_Dictionary_item_equals_value_should_work()
    {
        var collection = Fixture.Collection;
        var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

        var queryable = collection.AsQueryable().Where(x => x.Dictionary[guid] == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.01020304-0506-0708-090a-0b0c0d0e0f10' : 2 } }");

        var result = Queryable.Single(queryable);
        result.Id.Should().Be(1);
    }

    [Fact]
    public void Select_Dictionary_item_equals_value_should_work()
    {
        var collection = Fixture.Collection;
        var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary[guid]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$Dictionary.01020304-0506-0708-090a-0b0c0d0e0f10', _id : 0 } }");

        var result = Queryable.Single(queryable);
        result.Should().Be(2);
    }

    public class C
    {
        public int Id { get; set; }
        public Dictionary<Guid, int> Dictionary { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Dictionary = new Dictionary<Guid, int> { [Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10")] = 2 } },
        ];
    }
}
