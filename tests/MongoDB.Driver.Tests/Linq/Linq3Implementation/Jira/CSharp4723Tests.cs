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
    public class CSharp4723Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Find_projection_in_findoneandupdate_should_work()
        {
            var collection = GetCollection();

            var update = Builders<A>.Update.Set("Value", "updated");
            var options = new FindOneAndUpdateOptions<A>
            {
                Projection = Builders<A>.Projection.Expression(x => x)
            };

            var result = collection.FindOneAndUpdate<A, A>(x => x.Id == 1, update, options);

            result.Id.Should().Be(1);
            result.Value.Should().Be("1");
        }

        [Fact]
        public void Find_projection_in_findoneandreplace_should_work()
        {
            var collection = GetCollection();
            var options = new FindOneAndReplaceOptions<A, A>
            {
                Projection = Builders<A>.Projection.Expression(x => x)
            };

            var result = collection.FindOneAndReplace<A, A>(x => x.Id == 1, new A { Id = 1, Value = "updated" }, options);

            result.Id.Should().Be(1);
            result.Value.Should().Be("1");
        }

        [Fact]
        public void Find_projection_in_findoneanddelete_should_work()
        {
            var collection = GetCollection();

            var options = new FindOneAndDeleteOptions<A>
            {
                Projection = Builders<A>.Projection.Expression(x => x),
            };

            var result = collection.FindOneAndDelete<A, A>(x => x.Id == 1, options);

            result.Id.Should().Be(1);
            result.Value.Should().Be("1");
        }

        private IMongoCollection<A> GetCollection()
        {
            var collection = GetCollection<A>("test");
            CreateCollection(
                collection,
                new A { Id = 1, Value = "1"});
            return collection;
        }

        private class A
        {
            public int Id { get; set; }

            public string Value { get; set; }
        }
    }
}
