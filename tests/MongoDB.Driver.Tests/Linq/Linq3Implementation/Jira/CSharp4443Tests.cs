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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4443Tests : LinqIntegrationTest<CSharp4443Tests.ClassFixture>
{
    public CSharp4443Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_All_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : '$Dictionary', as : 'kvp', in : { $gt : [{ $arrayElemAt : ['$$kvp', 1] }, 100] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Any_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$Dictionary', as : 'kvp', in : { $gt : [{ $arrayElemAt : ['$$kvp', 1] }, 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Average_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : { $map : { input : '$Dictionary', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(55.666666666666664, 52.0, 67.5, 165.0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_ContainsKey_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', { $map : { input : '$Dictionary', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_ContainsValue_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.ContainsValue(25));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [25, { $map : { input : '$Dictionary', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Count_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$Dictionary' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(3, 3, 2, 2);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_CountWithPredicate_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Count(kvp => kvp.Value < 50));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Dictionary', as : 'kvp', in : { $cond : { if : { $lt : [{ $arrayElemAt : ['$$kvp', 1] }, 50] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(2, 2, 1, 0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_First_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] } } }, 0] }, 1] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_FirstOrDefault_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $let : { vars : { values : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : [{ $arrayElemAt : ['$$kvp', 0] }, 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : [null, 0], else : { $arrayElemAt : ['$$values', 0] } } } } }, 1] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] }, limit : 1 } }, 0] }, 1] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', { $map : { input : '$Dictionary', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Max_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : { $map : { input : '$Dictionary', as : 'kvp', in : { k : { $arrayElemAt : ['$$kvp', 0] }, v : { $arrayElemAt : ['$$kvp', 1] } } } }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(100, 85, 100, 200);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Select_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Select(kvp => kvp.Value).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Dictionary', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Sum_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$Dictionary', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [42, { $map : { input : '$Dictionary', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfArrays_Where_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 1] }, 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$Dictionary', as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Average_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $avg : '$Dictionary.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(55.666666666666664, 52.0, 67.5, 165.0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_ContainsKey_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_FirstOrDefault_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', '$Dictionary.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Max_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $max : '$Dictionary.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(100, 85, 100, 200);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Select_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$Dictionary.v' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [42, '$Dictionary.v'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_DictionaryAsArrayOfDocuments_Where_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_DictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Average_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(55.666666666666664, 52.0, 67.5, 165.0);
    }

    [Fact]
    public void Select_DictionaryAsDocument_ContainsKey_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsDocument_FirstOrDefault_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_DictionaryAsDocument_IndexerAccess_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : '$Dictionary.age', _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_DictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$Dictionary.life' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Max_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'item', in : '$$item.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(100, 85, 100, 200);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Select_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_DictionaryAsDocument_ValuesContains_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [42, { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_DictionaryAsDocument_Where_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Dictionary.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_All_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $allElementsTrue : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $gt : [{ $arrayElemAt : ['$$kvp', 1] }, 100] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Any_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $gt : [{ $arrayElemAt : ['$$kvp', 1] }, 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Average_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(55.666666666666664, 52.0, 67.5, 165.0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_ContainsKey_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Count_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Count);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $size : '$DictionaryInterface' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(3, 3, 2, 2);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_CountWithPredicate_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Count(kvp => kvp.Value < 50));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $cond : { if : { $lt : [{ $arrayElemAt : ['$$kvp', 1] }, 50] }, then : 1, else : 0 } } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(2, 2, 1, 0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_First_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] } } }, 0] }, 1] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_FirstOrDefault_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $let : { vars : { values : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : [{ $arrayElemAt : ['$$kvp', 0] }, 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : [null, 0], else : { $arrayElemAt : ['$$values', 0] } } } } }, 1] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] }, limit : 1 } }, 0] }, 1] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 0] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Max_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(100, 85, 100, 200);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Select_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Select(kvp => kvp.Value).Sum());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Sum_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [42, { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $arrayElemAt : ['$$kvp', 1] } } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfArrays_Where_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 1] }, 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Average_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $avg : '$DictionaryInterface.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(55.666666666666664, 52.0, 67.5, 165.0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_ContainsKey_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_FirstOrDefault_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : ['life', '$DictionaryInterface.k'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Max_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $max : '$DictionaryInterface.v' }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(100, 85, 100, 200);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Select_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : '$DictionaryInterface.v' }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [42, '$DictionaryInterface.v'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsArrayOfDocuments_Where_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : { $gt : ['$$kvp.v', 90] } } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, true, true);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Average_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Average());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $avg : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(55.666666666666664, 52.0, 67.5, 165.0);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_ContainsKey_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.First(kvp => kvp.Key == "age").Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] } } }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(25, 30, 35, 130);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_FirstOrDefault_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.FirstOrDefault(kvp => kvp.Key.StartsWith("l")).Value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $let : { vars : { values : { $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } } }, in : { $cond : { if : { $eq : [{ $size : '$$values' }, 0] }, then : { k : null, v : 0 }, else : { $arrayElemAt : ['$$values', 0] } } } } } }, in : '$$this.v' } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(42, 41, 0, 0);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_IndexerAccess_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$DictionaryInterface.life' }, 'missing'] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, true, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Max_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Max());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $max : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(100, 85, 100, 200);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Select_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Sum(kvp => kvp.Value));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $sum : { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } } }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.Should().Equal(167, 156, 135, 330);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_ValuesContains_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $in : [42, { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } }] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(true, false, false, false);
    }

    [Fact]
    public void Select_IDictionaryAsDocument_Where_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Select(x => x.DictionaryInterface.Where(kvp => kvp.Value == 35).Any());

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : { $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : ['$$kvp.v', 35] } } } }, 0] }, _id : 0 } }");

        var result = queryable.ToList();
        result.Should().Equal(false, false, true, false);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_All_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $not : { $elemMatch : { '1' : { $not : { $gt : 100 } } } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle().Which.Name.Should().Be("D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_Any_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { '1' : { $gt : 90 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_ContainsKey_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { '0' : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(doc => doc.Dictionary.ContainsKey("life"));
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_ContainsValue_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsValue(25));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { '1' : 25 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle()
            .Which.Name.Should().Be("A");
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 0)]
    public void Where_DictionaryAsArrayOfArrays_Count_should_work(int threshold, int expectedCount)
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Count > threshold);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $$"""{ $match : { 'Dictionary.{{threshold}}' : { $exists : true } } }""");

        var result = queryable.ToList();
        result.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_CountWithPredicate_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Count(kvp => kvp.Value < 50) == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $sum : { $map : { input : '$Dictionary', as : 'kvp', in : { $cond : { if : { $lt : [{ $arrayElemAt : ['$$kvp', 1] }, 50] }, then : 1, else : 0 } } } } }, 2] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_First_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gt : [{ $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : [{ $arrayElemAt : ['$$kvp', 0] }, 'l'] }, 0] } } }, 0] }, 1] }, 40] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'life'] }, limit : 1 } }, 0] }, 1] }, 42] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result.First().Name.Should().Be("A");
        result.First().Dictionary["life"].Should().Be(42);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { '0' : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_OrderBy_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderBy(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { Dictionary : { $elemMatch : { '0' : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] }, limit : 1 } }, 0] }, 1] } } }",
            "{ $sort : { _key1 : 1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_OrderByDescending_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderByDescending(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { Dictionary : { $elemMatch : { '0' : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] }, limit : 1 } }, 0] }, 1] } } }",
            "{ $sort : { _key1 : -1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfArrays_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { '1' : 42 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'life'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, 42] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
        result[0].Dictionary["life"].Should().Be(42);
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderBy(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { Dictionary : { $elemMatch : { k : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
            "{ $sort : { _key1 : 1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_OrderByDescending_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.ContainsKey("age"))
            .OrderByDescending(x => x.Dictionary["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { Dictionary : { $elemMatch : { k : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$Dictionary', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
            "{ $sort : { _key1 : -1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    [Fact]
    public void Where_DictionaryAsArrayOfDocuments_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Dictionary : { $elemMatch : { v : 42 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
    }

    [Fact]
    public void Where_DictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_DictionaryAsDocument_IndexerAccess_should_work()
    {
        var collection = Fixture.DocCollection;

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
    public void Where_DictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
    public void Where_DictionaryAsDocument_ValuesContains_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionary.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $in : [42, { $map : { input : { $objectToArray : '$Dictionary' }, as : 'kvp', in : '$$kvp.v' } }] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_All_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.All(kvp => kvp.Value > 100));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $not : { $elemMatch : { '1' : { $not : { $gt : 100 } } } } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle().Which.Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_Any_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Any(kvp => kvp.Value > 90));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { '1' : { $gt : 90 } } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_ContainsKey_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { '0' : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(doc => doc.DictionaryInterface.ContainsKey("life"));
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_Count_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Count == 3);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $size : 3 } } }");

        var results = queryable.ToList();
        results.Select(x => x.Name).Should().Equal("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_CountWithPredicate_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Count(kvp => kvp.Value < 50) == 2);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $sum : { $map : { input : '$DictionaryInterface', as : 'kvp', in : { $cond : { if : { $lt : [{ $arrayElemAt : ['$$kvp', 1] }, 50] }, then : 1, else : 0 } } } } }, 2] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_First_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gt : [{ $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : [{ $arrayElemAt : ['$$kvp', 0] }, 'l'] }, 0] } } }, 0] }, 1] }, 40] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'life'] }, limit : 1 } }, 0] }, 1] }, 42] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result.First().Name.Should().Be("A");
        result.First().DictionaryInterface["life"].Should().Be(42);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Keys.Contains("life"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { '0' : 'life' } } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_OrderBy_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderBy(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryInterface : { $elemMatch : { '0' : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] }, limit : 1 } }, 0] }, 1] } } }",
            "{ $sort : { _key1 : 1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_OrderByDescending_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderByDescending(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryInterface : { $elemMatch : { '0' : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $arrayElemAt : [{ $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $arrayElemAt : ['$$kvp', 0] }, 'age'] }, limit : 1 } }, 0] }, 1] } } }",
            "{ $sort : { _key1 : -1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfArrays_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfArraysCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { '1' : 42 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_All_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_IndexerAccess_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface["life"] == 42);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'life'] }, limit : 1 } }, 0] } }, in : '$$this.v' } }, 42] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
        result[0].DictionaryInterface["life"].Should().Be(42);
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_KeysContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

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
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderBy(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
            "{ $sort : { _key1 : 1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_OrderByDescending_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.ContainsKey("age"))
            .OrderByDescending(x => x.DictionaryInterface["age"]);

        var stages = Translate(collection, queryable);
        AssertStages(stages,
            "{ $match : { DictionaryInterface : { $elemMatch : { k : 'age' } } } }",
            "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : '$DictionaryInterface', as : 'kvp', cond : { $eq : ['$$kvp.k', 'age'] }, limit : 1 } }, 0] } }, in : '$$this.v' } } } }",
            "{ $sort : { _key1 : -1 } }",
            "{ $replaceRoot : { newRoot : '$_document' } }");

        var result = queryable.ToList();
        result.Should().HaveCount(4);
        result.First().Name.Should().Be("D");
    }

    [Fact]
    public void Where_IDictionaryAsArrayOfDocuments_ValuesContains_should_work()
    {
        var collection = Fixture.ArrayOfDocsCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { DictionaryInterface : { $elemMatch : { v : 42 } } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
    }

    [Fact]
    public void Where_IDictionaryAsDocument_All_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.First(kvp => kvp.Key.StartsWith("l")).Value > 40);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gt : [{ $let : { vars : { this : { $arrayElemAt : [{ $filter : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', cond : { $eq : [{ $indexOfCP : ['$$kvp.k', 'l'] }, 0] } } }, 0] } }, in : '$$this.v' } }, 40] } } }");

        var result = queryable.ToList();
        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Where_IDictionaryAsDocument_IndexerAccess_should_work()
    {
        var collection = Fixture.DocCollection;

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
    public void Where_IDictionaryAsDocument_KeysContains_should_work()
    {
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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
        var collection = Fixture.DocCollection;

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

    [Fact]
    public void Where_IDictionaryAsDocument_ValuesContains_should_work()
    {
        var collection = Fixture.DocCollection;

        var queryable = collection.AsQueryable()
            .Where(x => x.DictionaryInterface.Values.Contains(42));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $in : [42, { $map : { input : { $objectToArray : '$DictionaryInterface' }, as : 'kvp', in : '$$kvp.v' } }] } } }");

        var result = queryable.ToList();
        result.Should().ContainSingle();
    }

    public class DocumentRepresentation
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public Dictionary<string, int> Dictionary { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public IDictionary<string, int> DictionaryInterface { get; set; }
    }

    public class ArrayOfArraysRepresentation
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, int> Dictionary { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public IDictionary<string, int> DictionaryInterface { get; set; }
    }

    public class ArrayOfDocumentsRepresentation
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> Dictionary { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public IDictionary<string, int> DictionaryInterface { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<DocumentRepresentation> DocCollection { get; private set; }
        public IMongoCollection<ArrayOfArraysRepresentation> ArrayOfArraysCollection { get; private set; }
        public IMongoCollection<ArrayOfDocumentsRepresentation> ArrayOfDocsCollection { get; private set; }

        protected override void InitializeFixture()
        {
            DocCollection = Database.GetCollection<DocumentRepresentation>("test_document");
            ArrayOfArraysCollection = Database.GetCollection<ArrayOfArraysRepresentation>("test_array_of_arrays");
            ArrayOfDocsCollection = Database.GetCollection<ArrayOfDocumentsRepresentation>("test_array_of_docs");

            SeedTestDictionary();
        }

        private void SeedTestDictionary()
        {
            // Clear existing Dictionary
            DocCollection.DeleteMany(FilterDefinition<DocumentRepresentation>.Empty);
            ArrayOfArraysCollection.DeleteMany(FilterDefinition<ArrayOfArraysRepresentation>.Empty);
            ArrayOfDocsCollection.DeleteMany(FilterDefinition<ArrayOfDocumentsRepresentation>.Empty);

            // Insert test Dictionary for Document representation
            var docDictionary = new List<DocumentRepresentation>
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
            DocCollection.InsertMany(docDictionary);

            // Insert test Dictionary for ArrayOfArrays representation
            var arrayDictionary = new List<ArrayOfArraysRepresentation>
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
            ArrayOfArraysCollection.InsertMany(arrayDictionary);

            // Insert test Dictionary for ArrayOfDocuments representation
            var arrayDocDictionary = new List<ArrayOfDocumentsRepresentation>
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
            ArrayOfDocsCollection.InsertMany(arrayDocDictionary);
        }
    }
}