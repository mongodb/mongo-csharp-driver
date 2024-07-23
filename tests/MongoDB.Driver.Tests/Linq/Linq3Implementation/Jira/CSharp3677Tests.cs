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

using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3677Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Update_field_with_anonymous_class_should_work()
        {
            var collection = GetCollection();
            var pipeline = new EmptyPipelineDefinition<C>().Set(x => new { A = x.B });

            var result = collection.UpdateMany("{}", pipeline);
            result.ModifiedCount.Should().Be(1);

            var document = collection.Find("{}").Single();
            document.A.Should().Be(2);
        }

        [Fact]
        public void Update_field_with_initializer_syntax_should_work()
        {
            var collection = GetCollection();
            var pipeline = new EmptyPipelineDefinition<C>().Set(x => new C() { A = x.B });

            var result = collection.UpdateMany("{}", pipeline);
            result.ModifiedCount.Should().Be(1);

            var document = collection.Find("{}").Single();
            document.A.Should().Be(2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, A = 1, B = 2 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int A { get; set; }
            public int B { get; set; }
        }
    }
}
