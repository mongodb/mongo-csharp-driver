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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5564Tests : LinqIntegrationTest<CSharp5564Tests.ClassFixture>
{
    public CSharp5564Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Find_E_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.E == E.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { E : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_EByte_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.EByte == EByte.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { EByte : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_EInt_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.EInt == EInt.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { EInt : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_ELong_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.ELong == ELong.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { ELong : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_ESByte_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.ESByte == ESByte.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { ESByte : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_EShort_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.EShort == EShort.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { EShort : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_EUInt_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.EUInt == EUInt.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { EUInt : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_EULong_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.EULong == EULong.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { EULong : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    [Fact]
    public void Find_EUShort_equals_B_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable();
        var result = queryable.FirstOrDefault(x => x.EUShort == EUShort.B);

        var stages = queryable.GetMongoQueryProvider().LoggedStages;
        AssertStages(
            stages,
            "{ $match : { EUShort : 'B' } }",
            "{ $limit : 1 }");

        result.Id.Should().Be(2);
    }

    public class C
    {
        public int Id { get; set; }
        [BsonRepresentation(BsonType.String)] public E E { get; set; }
        [BsonRepresentation(BsonType.String)] public EByte EByte { get; set; }
        [BsonRepresentation(BsonType.String)] public EInt EInt { get; set; }
        [BsonRepresentation(BsonType.String)] public ELong ELong { get; set; }
        [BsonRepresentation(BsonType.String)] public ESByte ESByte { get; set; }
        [BsonRepresentation(BsonType.String)] public EShort EShort { get; set; }
        [BsonRepresentation(BsonType.String)] public EUInt EUInt { get; set; }
        [BsonRepresentation(BsonType.String)] public EULong EULong { get; set; }
        [BsonRepresentation(BsonType.String)] public EUShort EUShort { get; set; }
    }

    public enum E
    {
        A = 1,
        B = 2
    }

    public enum EByte : byte
    {
        A = 1,
        B = 2
    }

    public enum EInt : int
    {
        A = 1,
        B = 2
    }

    public enum ELong : long
    {
        A = 1,
        B = 2
    }

    public enum ESByte : sbyte
    {
        A = 1,
        B = 2
    }

    public enum EShort : short
    {
        A = 1,
        B = 2
    }

    public enum EUInt : uint
    {
        A = 1,
        B = 2
    }

    public enum EULong : ulong
    {
        A = 1,
        B = 2
    }

    public enum EUShort : ushort
    {
        A = 1,
        B = 2
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, E = E.A, EByte = EByte.A, EInt = EInt.A, ELong = ELong.A, ESByte = ESByte.A, EShort = EShort.A, EUInt = EUInt.A, EULong = EULong.A, EUShort = EUShort.A },
            new C { Id = 2, E = E.B, EByte = EByte.B, EInt = EInt.B, ELong = ELong.B, ESByte = ESByte.B, EShort = EShort.B, EUInt = EUInt.B, EULong = EULong.B, EUShort = EUShort.B },
        ];
    }
}
