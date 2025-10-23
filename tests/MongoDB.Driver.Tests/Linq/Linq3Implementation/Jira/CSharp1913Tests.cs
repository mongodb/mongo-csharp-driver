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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp1913Tests : LinqIntegrationTest<CSharp1913Tests.ClassFixture>
{
    public CSharp1913Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Nested__SelectMany_with_ArrayOfArrays_representation_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>()
            .Where(to => to.Name == "TestName")
            .Select(to => to.DictionaryWithArrayOfArraysRepresentation.SelectMany(kvp => new KeyValuePair<string, string>[] { kvp }));

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Name : 'TestName' } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : '$DictionaryWithArrayOfArraysRepresentation', as : 'kvp', in : ['$$kvp'] } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Select(kvp => kvp.Key).Should().Equal("A", "B", "C");
    }

    [Fact]
    public void Nested__SelectMany_with_ArrayOfDocuments_representation_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>()
            .Where(to => to.Name == "TestName")
            .Select(to => to.DictionaryWithArrayOfDocumentsRepresentation.SelectMany(kvp => new KeyValuePair<string, string>[] { kvp }));

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Name : 'TestName' } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : '$DictionaryWithArrayOfDocumentsRepresentation', as : 'kvp', in : ['$$kvp'] } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Select(kvp => kvp.Key).Should().Equal("A", "B", "C");
    }

    [Fact]
    public void Nested_SelectMany_with_Document_representation_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>()
            .Where(to => to.Name == "TestName")
            .Select(to => to.DictionaryWithDocumentRepresentation.SelectMany(kvp => new KeyValuePair<string, string>[] { kvp }));

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Name : 'TestName' } }",
            "{ $project : { _v : { $reduce : { input : { $map : { input : { $objectToArray : '$DictionaryWithDocumentRepresentation' }, as : 'kvp', in : ['$$kvp'] } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Select(kvp => kvp.Key).Should().Equal("A", "B", "C");
    }

    [Fact]
    public void Top_level_SelectMany_with_ArrayOfArrays_representation_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>()
            .Where(to => to.Name == "TestName")
            .SelectMany(to => to.DictionaryWithArrayOfArraysRepresentation);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Name : 'TestName' } }",
            "{ $project : { _v : '$DictionaryWithArrayOfArraysRepresentation', _id : 0 } }",
            "{ $unwind : '$_v' }");

        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Key.Should().Be("A");
        results[0].Value.Should().Be("a");
        results[1].Key.Should().Be("B");
        results[1].Value.Should().Be("b");
        results[2].Key.Should().Be("C");
        results[2].Value.Should().Be("c");
    }

    [Fact]
    public void Top_level_SelectMany_with_ArrayOfDocuments_representation_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>()
            .Where(to => to.Name == "TestName")
            .SelectMany(to => to.DictionaryWithArrayOfDocumentsRepresentation);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Name : 'TestName' } }",
            "{ $project : { _v : '$DictionaryWithArrayOfDocumentsRepresentation', _id : 0 } }",
            "{ $unwind : '$_v' }");

        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Key.Should().Be("A");
        results[0].Value.Should().Be("a");
        results[1].Key.Should().Be("B");
        results[1].Value.Should().Be("b");
        results[2].Key.Should().Be("C");
        results[2].Value.Should().Be("c");
    }

    [Fact]
    public void Top_level_SelectMany_with_Document_representation_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>()
            .Where(to => to.Name == "TestName")
            .SelectMany(to => to.DictionaryWithDocumentRepresentation);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Name : 'TestName' } }",
            "{ $project : { _v : { $objectToArray : '$DictionaryWithDocumentRepresentation' }, _id : 0 } }",
            "{ $unwind : '$_v' }");

        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Key.Should().Be("A");
        results[0].Value.Should().Be("a");
        results[1].Key.Should().Be("B");
        results[1].Value.Should().Be("b");
        results[2].Key.Should().Be("C");
        results[2].Value.Should().Be("c");
    }

    public class C
    {
        public int Id {  get; set; }
        public string Name {get;set;}

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string,string> DictionaryWithArrayOfArraysRepresentation { get; set; } = new Dictionary<string, string>();


        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string,string> DictionaryWithArrayOfDocumentsRepresentation { get; set; } = new Dictionary<string, string>();

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public Dictionary<string,string> DictionaryWithDocumentRepresentation { get; set; } = new Dictionary<string, string>();
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C
            {
                Id = 1,
                Name = "TestName",
                DictionaryWithArrayOfArraysRepresentation = new Dictionary<string, string> { { "A", "a" },  { "B", "b" }, {  "C", "c" } },
                DictionaryWithArrayOfDocumentsRepresentation = new Dictionary<string, string> { { "A", "a" },  { "B", "b" }, {  "C", "c" } },
                DictionaryWithDocumentRepresentation = new Dictionary<string, string> { { "A", "a" },  { "B", "b" }, {  "C", "c" } },
            }
        ];
    }
}
