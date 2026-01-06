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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4443DocumentTests : LinqIntegrationTest<CSharp4443DocumentTests.ClassFixture>
{
    private static readonly bool FilterLimitIsSupported = Feature.FilterLimit.IsSupported(CoreTestConfiguration.MaxWireVersion);

    public CSharp4443DocumentTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Select_DictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $gt : ['$$kvp.v', 100] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_DictionaryAsDocument_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$Dictionary.life' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsDocument_ContainsValue_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.ContainsValue(25));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [25, { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $objectToArray : '$Dictionary' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(3, 3, 2, 2);
    }

    [Fact]
    public void Select_DictionaryAsDocument_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Count(kvp => kvp.Value < 50));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(2, 2, 1, 0);
    }

    [Fact]
    public void Select_DictionaryAsDocument_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsDocument_FirstOrDefault_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_DictionaryAsDocument_IndexerAccess_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$Dictionary.age', _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsDocument_IntKey_IndexerAccess_should_throw()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryWithIntKeys[10]);

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("Document representation requires keys to serialize as strings");
    }

    [Fact]
    public void Select_DictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$Dictionary.life' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Select(kvp => kvp.Value).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $gt : ['$$kvp.v', 100] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$DictionaryInterface.life' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : { $objectToArray : '$DictionaryInterface' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(3, 3, 2, 2);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Count(kvp => kvp.Value < 50));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(2, 2, 1, 0);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_FirstOrDefault_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }
        else
        {
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");
        }

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_IndexerAccess_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$DictionaryInterface.age', _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$DictionaryInterface.life' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Select_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Select(kvp => kvp.Value).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Sum_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Where_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Where_DictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $allElementsTrue : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $gt : ['$$kvp.v', 100] } } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle().Which.Name.Should().Be("D");
    }

    [Fact]
    public void Where_DictionaryAsDocument_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $anyElementTrue : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Where_DictionaryAsDocument_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.life' : { $exists : true } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(doc => doc.Dictionary.ContainsKey("life"));
    }

    [Fact]
    public void Where_DictionaryAsDocument_ContainsValue_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsValue(25));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $in : [25, { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } }] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle()
            .Which.Name.Should().Be("A");
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 0)]
    public void Where_DictionaryAsDocument_Count_should_work(int threshold, int expectedCount)
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Count > threshold);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $$"""{ $match : { $expr : { $gt : [{ $size : { $objectToArray : '$Dictionary' } }, {{threshold}}] } } }""");

        var result = queryable.ToList();
        result.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void Where_DictionaryAsDocument_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Count(kvp => kvp.Value < 50) == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $sum : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, 2] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_DictionaryAsDocument_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }
        else
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_Equal_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.life' : 42 } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
        result[0].Dictionary["life"].Should().Be(42);
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_GreaterThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] > 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.age' : { $gt : 30 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("C", "D");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_GreaterThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] >= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.age' : { $gte : 30 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_LessThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] < 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.age' : { $lt : 30 } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_LessThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] <= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.age' : { $lte : 30 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_NotEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["age"] != 25);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.age' : { $ne : 25 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IntKey_IndexerAccess_should_throw()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryWithIntKeys[10] == "A");

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("Document representation requires keys to serialize as strings");
    }

    [Fact]
    public void Where_DictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Dictionary.life' : { $exists : true } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_DictionaryAsDocument_OrderBy_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderBy(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'Dictionary.age' : { $exists : true } } }",
            "{ $sort : { 'Dictionary.age' : 1 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsDocument_OrderByDescending_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderByDescending(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'Dictionary.age' : { $exists : true } } }",
            "{ $sort : { 'Dictionary.age' : -1 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $allElementsTrue : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $gt : ['$$kvp.v', 100] } } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle().Which.Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_Any_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $anyElementTrue : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Where_IDictionaryAsDocument_ContainsKey_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.life' : { $exists : true } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(doc => doc.DictionaryInterface.ContainsKey("life"));
    }

    [Fact]
    public void Where_IDictionaryAsDocument_Count_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Count == 3);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $size : { $objectToArray : '$DictionaryInterface' } }, 3] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Name).Should().Equal("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_CountWithPredicate_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Count(kvp => kvp.Value < 50) == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $sum : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $cond : { if : { $lt : ['$$kvp.v', 50] }, then : 1, else : 0 } } } } }, 2] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_IDictionaryAsDocument_First_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);

        if (FilterLimitIsSupported)
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }
        else
        {
            AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");
        }

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_Equal_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.life' : 42 } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
        result[0].DictionaryInterface["life"].Should().Be(42);
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_GreaterThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] > 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.age' : { $gt : 30 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("C", "D");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_GreaterThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] >= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.age' : { $gte : 30 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_LessThan_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] < 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.age' : { $lt : 30 } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_LessThanOrEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] <= 30);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.age' : { $lte : 30 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_NotEqual_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["age"] != 25);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.age' : { $ne : 25 } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo("B", "C", "D");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'DictionaryInterface.life' : { $exists : true } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_IDictionaryAsDocument_OrderBy_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderBy(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryInterface.age' : { $exists : true } } }",
            "{ $sort : { 'DictionaryInterface.age' : 1 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_OrderByDescending_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderByDescending(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { 'DictionaryInterface.age' : { $exists : true } } }",
            "{ $sort : { 'DictionaryInterface.age' : -1 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    public class DocumentRepresentation
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public Dictionary<string, int> Dictionary { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public IDictionary<string, int> DictionaryInterface { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public Dictionary<int, string> DictionaryWithIntKeys { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<DocumentRepresentation> Collection { get; private set; }

        protected override void InitializeFixture()
        {
            Collection = Database.GetCollection<DocumentRepresentation>("test_document");
            SeedTestData();
        }

        private void SeedTestData()
        {
            Collection.DeleteMany(FilterDefinition<DocumentRepresentation>.Empty);

            var testData = new List<DocumentRepresentation>
            {
                new()
                {
                    Name = "A",
                    Dictionary = new Dictionary<string, int> { { "life", 42 }, { "age", 25 }, { "score", 100 } },
                    DictionaryInterface = new Dictionary<string, int> { { "life", 42 }, { "age", 25 }, { "score", 100 } }
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
