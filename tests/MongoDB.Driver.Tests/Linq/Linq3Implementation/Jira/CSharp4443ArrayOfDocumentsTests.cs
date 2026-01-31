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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4443ArrayOfDocumentsTests : LinqIntegrationTest<CSharp4443ArrayOfDocumentsTests.ClassFixture>
{
    private static readonly bool FilterLimitIsSupported = Feature.FilterLimit.IsSupported(CoreTestConfiguration.MaxWireVersion);

    public CSharp4443ArrayOfDocumentsTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : '$Dictionary', as : 'kvp', in : { $gt : ['$$kvp.v', 100] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$Dictionary', as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', '$Dictionary.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_ContainsValue_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.ContainsValue(25));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [25, '$Dictionary.v'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$Dictionary' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(3, 3, 2, 2);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Count(kvp => kvp.Value < 50));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Dictionary', as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(2, 2, 1, 0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_FirstOrDefault_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_IndexerAccess_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_IntKey_IndexerAccess_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryWithIntKeys[10]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryWithIntKeys', as : 'kvp', cond : { $eq : ['$$kvp.k', 10] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryWithIntKeys', as : 'kvp', cond : { $eq : ['$$kvp.k', 10] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var result = queryable.First();
        result.Should().Be("A");
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', '$Dictionary.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Select(kvp => kvp.Value).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$Dictionary.v' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$Dictionary.v' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $gt : ['$$kvp.v', 100] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', '$DictionaryInterface.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryInterface' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(3, 3, 2, 2);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Count(kvp => kvp.Value < 50));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(2, 2, 1, 0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_FirstOrDefault_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_IndexerAccess_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', '$DictionaryInterface.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Select(kvp => kvp.Value).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$DictionaryInterface.v' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$DictionaryInterface.v' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $not : { $elemMatch : { v : { $not : { $gt : 100 } } } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle().Which.Name.Should().Be("D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { v : { $gt : 90 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(doc => doc.Dictionary.ContainsKey("life"));
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_ContainsValue_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsValue(25));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { v : 25 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle()
            .Which.Name.Should().Be("A");
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 0)]
    public void Where_DictionaryAsArrayOfDocuments_Count_should_work(int threshold, int expectedCount)
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Count > threshold);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $$"""{ $match : { 'Dictionary.{{threshold}}' : { $exists : true } } }""");

        var result = queryable.ToList();
        result.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Count(kvp => kvp.Value < 50) == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $sum : { $map : { input : '$Dictionary', as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, 2] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }
        else
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_Equal_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'life', v : 42 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
        result[0].Dictionary["life"].Should().Be(42);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_GreaterThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] > 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'age', v : { $gt : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("C", "D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_GreaterThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] >= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'age', v : { $gte : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_LessThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] < 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'age', v : { $lt : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_LessThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] <= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'age', v : { $lte : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_NotEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] != 25);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'age', v : { $ne : 25 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IntKey_IndexerAccess_Equal_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryWithIntKeys[10] == "A");

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryWithIntKeys : { $elemMatch : { k : 10, v : 'A' } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { k : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_OrderBy_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderBy(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages,
                "{ $match : { Dictionary : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }
        else
        {
            AssertStages(stages,
                "{ $match : { Dictionary : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_OrderByDescending_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderByDescending(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages,
                "{ $match : { Dictionary : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }
        else
        {
            AssertStages(stages,
                "{ $match : { Dictionary : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $not : { $elemMatch : { v : { $not : { $gt : 100 } } } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle().Which.Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { v : { $gt : 90 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(doc => doc.DictionaryInterface.ContainsKey("life"));
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Count == 3);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $size : 3 } } }");

        var results = queryable.ToList();
        results.Select(x => x.Name).Should().Equal("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Count(kvp => kvp.Value < 50) == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $sum : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, 2] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }
        else
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_Equal_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'life', v : 42 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
        result[0].DictionaryInterface["life"].Should().Be(42);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_GreaterThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] > 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age', v : { $gt : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("C", "D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_GreaterThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] >= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age', v : { $gte : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_LessThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] < 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age', v : { $lt : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_LessThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] <= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age', v : { $lte : 30 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_NotEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] != 25);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age', v : { $ne : 25 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { k : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_OrderBy_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderBy(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages,
                "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }
        else
        {
            AssertStages(stages,
                "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_OrderByDescending_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderByDescending(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages,
                "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }
        else
        {
            AssertStages(stages,
                "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age' } } } }",
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } } } }",
                "{ $sort : { _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    public class ArrayOfDocumentsRepresentation
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> Dictionary { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public IDictionary<string, int> DictionaryInterface { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<int, string> DictionaryWithIntKeys { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<ArrayOfDocumentsRepresentation> Collection { get; private set; }

        protected override void InitializeFixture()
        {
            Collection = Database.GetCollection<ArrayOfDocumentsRepresentation>("test_array_of_docs");
            SeedTestData();
        }

        private void SeedTestData()
        {
            Collection.DeleteMany(FilterDefinition<ArrayOfDocumentsRepresentation>.Empty);

            var testData = new List<ArrayOfDocumentsRepresentation>
            {
                new()
                {
                    Name = "A",
                    Dictionary = new Dictionary<string, int> { { "life", 42 }, { "age", 25 }, { "score", 100 } },
                    DictionaryInterface = new Dictionary<string, int> { { "life", 42 }, { "age", 25 }, { "score", 100 } },
                    DictionaryWithIntKeys = new Dictionary<int, string> { { 10, "A" }, { 20, "B" }, { 30, "C" } }
                },
                new()
                {
                    Name = "B",
                    Dictionary = new Dictionary<string, int> { { "life", 41 }, { "age", 30 }, { "score", 85 } },
                    DictionaryInterface = new Dictionary<string, int> { { "life", 41 }, { "age", 30 }, { "score", 85 } }
                },
                new()
                {
                    Name = "C",
                    Dictionary = new Dictionary<string, int> { { "health", 100 }, { "age", 35 } },
                    DictionaryInterface = new Dictionary<string, int> { { "health", 100 }, { "age", 35 } }
                },
                new()
                {
                    Name = "D",
                    Dictionary = new Dictionary<string, int> { { "health", 200 }, { "age", 130 } },
                    DictionaryInterface = new Dictionary<string, int> { { "health", 200 }, { "age", 130 } }
                }
            };
            Collection.InsertMany(testData);
        }
    }
}
