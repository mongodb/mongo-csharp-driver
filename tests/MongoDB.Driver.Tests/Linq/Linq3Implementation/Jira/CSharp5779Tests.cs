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

public class CSharp5779Tests : LinqIntegrationTest<CSharp5779Tests.ClassFixture>
{
    public CSharp5779Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("b");
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp' in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } } , _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'v', cond : { $eq : ['$$v.v', 2] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(2);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$DictionaryAsArrayOfDocuments.k', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$DictionaryAsArrayOfDocuments.k', as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("b");
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$DictionaryAsArrayOfDocuments', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryAsArrayOfDocuments', as : 'v', cond : { $eq : ['$$v.v', 2] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(2);
    }

    [Fact]
    public void DictionaryAsDocument_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void DictionaryAsDocument_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("b");
    }

    [Fact]
    public void DictionaryAsDocument_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $objectToArray : '$DictionaryAsDocument' }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocument_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', cond : { $eq : ['$$v.v', 2] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(2);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("b");
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } , as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(2);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$IDictionaryAsArrayOfDocuments.k', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$IDictionaryAsArrayOfDocuments.k', as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("b");
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$IDictionaryAsArrayOfDocuments.v', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(2);
    }

    [Fact]
    public void IDictionaryAsDocument_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void IDictionaryAsDocument_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("b");
    }

    [Fact]
    public void IDictionaryAsDocument_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be(2);
    }

    public class C
    {
        public int Id { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)] public Dictionary<string, int> DictionaryAsArrayOfArrays { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)] public Dictionary<string, int> DictionaryAsArrayOfDocuments { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.Document)] public Dictionary<string, int> DictionaryAsDocument { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)] public IDictionary<string, int> IDictionaryAsArrayOfArrays { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)] public IDictionary<string, int> IDictionaryAsArrayOfDocuments { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.Document)] public IDictionary<string, int> IDictionaryAsDocument { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C
            {
                Id = 1,
                DictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                DictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                DictionaryAsDocument = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                IDictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                IDictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                IDictionaryAsDocument = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } }
            }
        ];
    }
}
