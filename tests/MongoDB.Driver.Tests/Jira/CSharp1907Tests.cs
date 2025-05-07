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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp1907Tests : LinqIntegrationTest<CSharp1907Tests.ClassFixture>
    {
        public CSharp1907Tests(ClassFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void Test()
        {
            var col = Fixture.Collection;

            var query1 = col.OfType<Widget1>().Find(Builders<Widget1>.Filter.Eq(x => x.Name, "Test"));
            var query2 = col.Find(Builders<IFoo>.Filter.OfType<Widget1>(Builders<Widget1>.Filter.Eq(x => x.Name, "Test")));

            var result1 = query1.ToList();
            var result2 = query2.ToList();

            result1.Should().HaveCount(1);
            result1.First().Should().BeOfType<Widget1>();
            result1.First().Name.Should().Be("Test");

            result2.Should().HaveCount(1);
            result2.First().Should().BeOfType<Widget1>();
            result2.First().Name.Should().Be("Test");
        }

        public sealed class ClassFixture : MongoCollectionFixture<IFoo>
        {
            protected override IEnumerable<IFoo> InitialData =>
            [
                new Widget1 { Name = "Test", Bar = 1 },
                new Widget2 { Name = "Test", Bar = "2" }
            ];
        }

        public interface IFoo
        {
            ObjectId Id { get; set;}
            string Name { get; set;}
        }

        public class Widget1 : IFoo
        {
            public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            public string Name { get; set; }
            public int Bar { get; set; }
        }

        public class Widget2 : IFoo
        {
            public ObjectId Id { get; set;} = ObjectId.GenerateNewId();
            public string Name { get; set; }
            public string Bar { get; set; }
        }
    }
}