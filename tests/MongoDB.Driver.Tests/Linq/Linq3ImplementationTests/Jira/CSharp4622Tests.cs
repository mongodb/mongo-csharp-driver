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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4622Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var options = new ReplaceOptions { IsUpsert = true };
            var data = new Data { Id = 8, Text = "updated" };

            var result = collection.ReplaceOne(d => true, data, options);

            result.UpsertedId.Should().Be(8);
        }

        private IMongoCollection<Data> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<Data>("test", linqProvider);
            CreateCollection(collection);
            return collection;
        }

        private class Data
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }
    }
}
