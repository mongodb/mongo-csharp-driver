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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
#if NET6_0_OR_GREATER
    public class CSharp4749Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Filter_using_implicit_conversion_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var filter = Builders<Document>.Filter.Gt(v => v.S, 1);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<Document>();

            if (linqProvider == LinqProvider.V2)
            {
                var renderedFilter = filter.Render(documentSerializer, serializerRegistry, linqProvider);
                renderedFilter.Should().Be("{ S : { $gt : 1 } }"); // note: this translation is actually NOT correct!
            }
            else
            {
                var exception = Record.Exception(() => filter.Render(documentSerializer, serializerRegistry, linqProvider));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_using_UserType_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var filter = Builders<Document>.Filter.Gt(v => v.S, new UserType(1));
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<Document>();

            var renderedFilter = filter.Render(documentSerializer, serializerRegistry, linqProvider);
            renderedFilter.Should().Be("{ S : { $gt : { Value : 1 } } }");
        }

        private IMongoCollection<Document> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<Document>("test", linqProvider);
            CreateCollection(collection);
            return collection;
        }

        record struct UserType(int Value)
        {
            public static implicit operator int(UserType self) => self.Value;
        }

        record Document(UserType S);
    }
#endif
}
