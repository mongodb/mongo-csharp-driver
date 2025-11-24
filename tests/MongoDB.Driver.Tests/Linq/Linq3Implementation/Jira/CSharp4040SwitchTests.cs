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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4040SwitchTests : LinqIntegrationTest<CSharp4040SwitchTests.ClassFixture>, IDisposable
{
    static CSharp4040SwitchTests()
    {
        AppContext.SetSwitch("DisableCSharp4040Validation", true);

        var discriminatorConvention = new HierarchicalDiscriminatorConvention("TypeNames");

        BsonClassMap.RegisterClassMap<C>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
            cm.SetDiscriminatorIsRequired(true);
            cm.MapMember(x => x.TypeNames).SetShouldSerializeMethod(_ => false);
            cm.SetDiscriminatorConvention(discriminatorConvention);
        });
    }

    public void Dispose()
    {
        AppContext.SetSwitch("DisableCSharp4040Validation", false);
    }

    public CSharp4040SwitchTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Documents_should_serializer_as_expected()
    {
        var collection = Fixture.Collection;

        var seralizedDocuments = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToList();

        seralizedDocuments.Count.Should().Be(2);
        seralizedDocuments[0].Should().Be("{ _id : 1, TypeNames : ['C', 'D'] }");
        seralizedDocuments[1].Should().Be("{ _id : 2, TypeNames : ['C', 'D', 'E'] }");
    }

    [Fact]
    public void OfType_C_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<C>();

        var stages = Translate(collection, queryable);
        AssertStages(stages, []);

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void OfType_D_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<D>();

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { TypeNames : 'D' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void OfType_E_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OfType<E>();

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { TypeNames : 'E' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    [Fact]
    public void Where_TypeNames_Contains_C_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.TypeNames.Contains("C"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { TypeNames : 'C' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Where_TypeNames_Contains_D_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.TypeNames.Contains("D"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { TypeNames : 'D' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Where_TypeNames_Contains_E_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.TypeNames.Contains("E"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { TypeNames : 'E' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    public abstract class C
    {
        public int Id { get; set; }
        public virtual IReadOnlyList<string> TypeNames => ["C"];
    }

    public class D : C
    {
        public override IReadOnlyList<string> TypeNames => ["C", "D"];
    }

    public class E : D
    {
        public override IReadOnlyList<string> TypeNames => ["C", "D", "E"];
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new D { Id = 1 },
            new E { Id = 2 }
        ];
    }
}
