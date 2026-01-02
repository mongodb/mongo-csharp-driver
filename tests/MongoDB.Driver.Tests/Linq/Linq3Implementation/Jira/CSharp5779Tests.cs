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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5779Tests : LinqIntegrationTest<CSharp5779Tests.ClassFixture>
{
    static CSharp5779Tests()
    {
        BsonClassMap.RegisterClassMap<C>(cm =>
        {
            cm.AutoMap();

            var innerDictionarySerializer = DictionarySerializer.Create(DictionaryRepresentation.ArrayOfArrays, StringSerializer.Instance, Int32Serializer.Instance);
            var outerDictionarySerializer = DictionarySerializer.Create(DictionaryRepresentation.Document, StringSerializer.Instance, innerDictionarySerializer);
            cm.MapMember(c => c.DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays).SetSerializer(outerDictionarySerializer);
        });
    }

    private static readonly bool FilterLimitIsSupported = Feature.FilterLimit.IsSupported(CoreTestConfiguration.MaxWireVersion);

    public CSharp5779Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Keys_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Keys.Contains("b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['b', { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Keys_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Keys.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $eq : ['$$k', 'b'] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(null, null, "b", "b");
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "b");
        results[3].Should().Equal("a", "b", "c");
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.All(v => v > 0));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $gt : ['$$v', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Any(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $eq : ['$$v', 2] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Any_without_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, true, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Average_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1.0, 1.5, 2.0);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Average_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Average(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10.0, 15.0, 20.0);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Contains(2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [2, { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Count_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Count(v => v > 1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $cond : { if : { $gt : ['$$v', 1] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', cond : { $eq : ['$$v', 2] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 2, 2);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Max_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Max_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Max(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Select(v => v * 10).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp' in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } } , _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 2);
        results[3].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 3, 6);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Sum_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Sum(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void DictionaryAsArrayOfArrays_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfArrays.Values.Where(v => v > 1).Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : { $map : { input : '$DictionaryAsArrayOfArrays', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'v', cond : { $gt : ['$$v.v', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Keys_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Keys.Contains("b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['b', '$DictionaryAsArrayOfDocuments.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Keys_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Keys.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryAsArrayOfDocuments.k' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$DictionaryAsArrayOfDocuments.k', as : 'k', cond : { $eq : ['$$k', 'b'] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$DictionaryAsArrayOfDocuments.k', as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(null, null, "b", "b");
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$DictionaryAsArrayOfDocuments.k', _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "b");
        results[3].Should().Equal("a", "b", "c");
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.All(v => v > 0));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $gt : ['$$v.v', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Any(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $eq : ['$$v.v', 2] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Any_without_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : '$DictionaryAsArrayOfDocuments' }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, true, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Average_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : '$DictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1.0, 1.5, 2.0);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Average_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Average(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10.0, 15.0, 20.0);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Contains(2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [2, '$DictionaryAsArrayOfDocuments.v'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryAsArrayOfDocuments' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryAsArrayOfDocuments' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Count_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Count(v => v > 1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $cond : { if : { $gt : ['$$v.v', 1] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryAsArrayOfDocuments', as : 'v', cond : { $eq : ['$$v.v', 2] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryAsArrayOfDocuments', as : 'v', cond : { $eq : ['$$v.v', 2] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 2, 2);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Max_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : '$DictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Max_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Max(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Select(v => v * 10).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$DictionaryAsArrayOfDocuments', _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 2);
        results[3].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$DictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 3, 6);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Sum_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Sum(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryAsArrayOfDocuments', as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void DictionaryAsArrayOfDocuments_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsArrayOfDocuments.Values.Where(v => v > 1).Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : '$DictionaryAsArrayOfDocuments', as : 'v', cond : { $gt : ['$$v.v', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void DictionaryAsDocument_Keys_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Keys.Contains("b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$DictionaryAsDocument.b' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsDocument_Keys_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Keys.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocument_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, as : 'k', cond : { $eq : ['$$k', 'b'] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(null, null, "b", "b");
    }

    [Fact]
    public void DictionaryAsDocument_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "b");
        results[3].Should().Equal("a", "b", "c");
    }

    [Fact]
    public void DictionaryAsDocument_Values_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.All(v => v > 0));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $gt : ['$$v.v', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Any(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $eq : ['$$v.v', 2] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Any_without_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $objectToArray : '$DictionaryAsDocument' } }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, true, true, true);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Average_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsDocument.Count > 0)
            .Select(x => x.DictionaryAsDocument.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1.0, 1.5, 2.0);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Average_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsDocument.Count > 0)
            .Select(x => x.DictionaryAsDocument.Values.Average(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10.0, 15.0, 20.0);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Contains(2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [2, { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $objectToArray : '$DictionaryAsDocument' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $objectToArray : '$DictionaryAsDocument' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Count_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Count(v => v > 1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $cond : { if : { $gt : ['$$v.v', 1] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void DictionaryAsDocument_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', cond : { $eq : ['$$v.v', 2] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', cond : { $eq : ['$$v.v', 2] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 2, 2);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Max_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsDocument.Count > 0)
            .Select(x => x.DictionaryAsDocument.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $max : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Max_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryAsDocument.Count > 0)
            .Select(x => x.DictionaryAsDocument.Values.Max(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $max : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Select(v => v * 10).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void DictionaryAsDocument_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $objectToArray : '$DictionaryAsDocument' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 2);
        results[3].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 3, 6);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Sum_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Sum(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', in : { $multiply : ['$$v.v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void DictionaryAsDocument_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocument.Values.Where(v => v > 1).Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : { $objectToArray : '$DictionaryAsDocument' }, as : 'v', cond : { $gt : ['$$v.v', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays_Values_SelectMany_Keys_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays.Values.SelectMany(n => n.Keys.Where(k => k != "a")));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays' }, as : 'n', in : { $filter : { input : { $map : { input : '$$n.v', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $ne : ['$$k', 'a'] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal();
        results[2].Should().Equal("b");
        results[3].Should().Equal("b", "b", "c");
    }

    [Fact]
    public void DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays_Values_SelectMany_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays.Values.SelectMany(n => n.Keys));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays' }, as : 'n', in : { $map : { input : '$$n.v', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }  } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } }  }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "a", "b");
        results[3].Should().Equal("a", "a", "b", "a", "b", "c");
    }

    [Fact]
    public void DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays_Values_SelectMany_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays.Values.SelectMany(n => n.Values.Where(v => v > 1)));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays' }, as : 'n', in : { $filter : { input : { $map : { input : '$$n.v', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'v', cond : { $gt : ['$$v.v', 1] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal();
        results[2].Should().Equal(2);
        results[3].Should().Equal(2, 2, 3);
    }

    [Fact]
    public void DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays_Values_SelectMany_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays.Values.SelectMany(n => n.Values));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : { $objectToArray : '$DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays' }, as : 'n', in : { $map : { input : '$$n.v', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 1, 2);
        results[3].Should().Equal(1, 1, 2, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Keys_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Keys.Contains("b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['b', { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Keys_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Keys.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $eq : ['$$k', 'b'] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(null, null, "b", "b");
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "b");
        results[3].Should().Equal("a", "b", "c");
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.All(v => v > 0));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $gt : ['$$v', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Any(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $eq : ['$$v', 2] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Any_without_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, true, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Average_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1.0, 1.5, 2.0);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Average_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Average(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10.0, 15.0, 20.0);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Contains(2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [2, { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Count_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Count(v => v > 1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $cond : { if : { $gt : ['$$v', 1] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', cond : { $eq : ['$$v', 2] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 2, 2);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Max_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Max_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfArrays.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Max(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfArrays.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Select(v => v * 10).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 2);
        results[3].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 3, 6);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Sum_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Sum(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void IDictionaryAsArrayOfArrays_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfArrays.Values.Where(v => v > 1).Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : { $map : { input : '$IDictionaryAsArrayOfArrays', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }, as : 'v', cond : { $gt : ['$$v', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Keys_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Keys.Contains("b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['b', '$IDictionaryAsArrayOfDocuments.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Keys_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Keys.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$IDictionaryAsArrayOfDocuments.k' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$IDictionaryAsArrayOfDocuments.k', as : 'k', cond : { $eq : ['$$k', 'b'] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$IDictionaryAsArrayOfDocuments.k', as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(null, null, "b", "b");
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$IDictionaryAsArrayOfDocuments.k', _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "b");
        results[3].Should().Equal("a", "b", "c");
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.All(v => v > 0));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $gt : ['$$v', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Any(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $eq : ['$$v', 2] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Any_without_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : '$IDictionaryAsArrayOfDocuments.v' }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, true, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Average_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : '$IDictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1.0, 1.5, 2.0);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Average_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Average(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $avg : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10.0, 15.0, 20.0);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Contains(2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [2, '$IDictionaryAsArrayOfDocuments.v'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$IDictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$IDictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Count_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Count(v => v > 1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $cond : { if : { $gt : ['$$v', 1] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', cond : { $eq : ['$$v', 2] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 2, 2);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Max_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : '$IDictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Max_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsArrayOfDocuments.Count > 0)
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Max(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'IDictionaryAsArrayOfDocuments.0' : { $exists : true } } }",
            "{ $project : { _v : { $max : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Select(v => v * 10).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$IDictionaryAsArrayOfDocuments.v', _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 2);
        results[3].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$IDictionaryAsArrayOfDocuments.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 3, 6);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Sum_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Sum(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void IDictionaryAsArrayOfDocuments_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsArrayOfDocuments.Values.Where(v => v > 1).Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : '$IDictionaryAsArrayOfDocuments.v', as : 'v', cond : { $gt : ['$$v', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void IDictionaryAsDocument_Keys_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Keys.Contains("b"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$IDictionaryAsDocument.b' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsDocument_Keys_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Keys.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsDocument_Keys_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Keys.First(k => k == "b"));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, as : 'k', cond : { $eq : ['$$k', 'b'] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, as : 'k', cond : { $eq : ['$$k', 'b'] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(null, null, "b", "b");
    }

    [Fact]
    public void IDictionaryAsDocument_Keys_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Keys);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.k' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal("a");
        results[2].Should().Equal("a", "b");
        results[3].Should().Equal("a", "b", "c");
    }

    [Fact]
    public void IDictionaryAsDocument_Values_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.All(v => v > 0));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $gt : ['$$v', 0] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Any(v => v == 2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $eq : ['$$v', 2] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Any_without_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } } }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, true, true, true);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Average_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsDocument.Count > 0)
            .Select(x => x.IDictionaryAsDocument.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { IDictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1.0, 1.5, 2.0);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Average_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsDocument.Count > 0)
            .Select(x => x.IDictionaryAsDocument.Values.Average(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { IDictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $avg : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10.0, 15.0, 20.0);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Contains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Contains(2));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [2, { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, true, true);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Count_property_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Count_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Count(v => v > 1));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $cond : { if : { $gt : ['$$v', 1] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_First_with_predicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.First(v => v == 2));

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', cond : { $eq : ['$$v', 2] }, limit : 1 } }, 0] }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', cond : { $eq : ['$$v', 2] } } }, 0] }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 2, 2);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Max_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsDocument.Count > 0)
            .Select(x => x.IDictionaryAsDocument.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { IDictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $max : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Max_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.IDictionaryAsDocument.Count > 0)
            .Select(x => x.IDictionaryAsDocument.Values.Max(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { IDictionaryAsDocument : { $ne : { } } } }",
            "{ $project : { _v : { $max : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Select(v => v * 10).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(4);
        results[0].Should().Equal();
        results[1].Should().Equal(1);
        results[2].Should().Equal(1, 2);
        results[3].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 1, 3, 6);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Sum_with_selector_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Sum(v => v * 10));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', in : { $multiply : ['$$v', 10] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 10, 30, 60);
    }

    [Fact]
    public void IDictionaryAsDocument_Values_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.IDictionaryAsDocument.Values.Where(v => v > 1).Count());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : { $map : { input : { $objectToArray : '$IDictionaryAsDocument' }, as : 'kvp', in : '$$kvp.v' } }, as : 'v', cond : { $gt : ['$$v', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(0, 0, 1, 2);
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
        public Dictionary<string, Dictionary<string, int>> DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C
            {
                Id = 1,
                DictionaryAsArrayOfArrays = new Dictionary<string, int>(),
                DictionaryAsArrayOfDocuments = new Dictionary<string, int>(),
                DictionaryAsDocument = new Dictionary<string, int>(),
                IDictionaryAsArrayOfArrays = new Dictionary<string, int>(),
                IDictionaryAsArrayOfDocuments = new Dictionary<string, int>(),
                IDictionaryAsDocument = new Dictionary<string, int>(),
                DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays = new Dictionary<string, Dictionary<string, int>>()
            },
            new C
            {
                Id = 2,
                DictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 } },
                DictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 } },
                DictionaryAsDocument = new Dictionary<string, int> { { "a", 1 } },
                IDictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 } },
                IDictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 } },
                IDictionaryAsDocument = new Dictionary<string, int> { { "a", 1 } },
                DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays = new Dictionary<string, Dictionary<string, int>>
                {
                    { "", new Dictionary<string, int>() },
                    { "a", new Dictionary<string, int> { { "a", 1 } } }
                }
            },
            new C
            {
                Id = 3,
                DictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 },  { "b", 2 } },
                DictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 },  { "b", 2 } },
                DictionaryAsDocument = new Dictionary<string, int> { { "a", 1 },  { "b", 2 } },
                IDictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 },  { "b", 2 } },
                IDictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 },  { "b", 2 } },
                IDictionaryAsDocument = new Dictionary<string, int> { { "a", 1 },  { "b", 2 }, },
                DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays = new Dictionary<string, Dictionary<string, int>>
                {
                    { "", new Dictionary<string, int>() },
                    { "a", new Dictionary<string, int> { { "a", 1 } } },
                    { "b", new Dictionary<string, int> { { "a", 1 },  { "b", 2 } } }
                }
            },
            new C
            {
                Id = 4,
                DictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                DictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                DictionaryAsDocument = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                IDictionaryAsArrayOfArrays = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                IDictionaryAsArrayOfDocuments = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                IDictionaryAsDocument = new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } },
                DictionaryAsDocumentOfNestedDictionaryAsArrayOfArrays = new Dictionary<string, Dictionary<string, int>>
                {
                    { "", new Dictionary<string, int>() },
                    { "a", new Dictionary<string, int> { { "a", 1 } } },
                    { "b", new Dictionary<string, int> { { "a", 1 },  { "b", 2 } } },
                    { "c", new Dictionary<string, int> { { "a", 1 },  { "b", 2 },  { "c", 3 } } }
                }
            },
        ];
    }
}
