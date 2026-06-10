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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5516Tests : LinqIntegrationTest<CSharp5516Tests.ClassFixture>
{
    public CSharp5516Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Test1()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => new Animal[] { x.Cat, x.Dog });

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : ["$Cat", "$Dog"], _id : 0 } }""");

        var result = queryable.Single();
        result.Should().HaveCount(2);
        result[0].Should().BeOfType<Cat>();
        result[0].As<Cat>().C.Should().Be("abc");
        result[1].Should().BeOfType<Dog>();
        result[1].As<Dog>().D.Should().Be(123);
    }

    public sealed class C
    {
        public int Id { get; set; }
        public Cat Cat { get; set; }
        public Dog Dog { get; set; }
    }

    public abstract class Animal
    {
    }

    public sealed class Cat : Animal
    {
        public string C {  get; set; }
    }

    public sealed class Dog : Animal
    {
        public int D {  get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Cat = new Cat { C = "abc" }, Dog = new Dog { D = 123 } }
        ];
    }
}
