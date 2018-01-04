/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FilterDefinitionBuilderEnumerableNullableEnumContainsNullableEnumTests
    {
        private static IBsonSerializerRegistry __registry = BsonSerializer.SerializerRegistry;
        private static IBsonSerializer<C> __serializer = BsonSerializer.SerializerRegistry.GetSerializer<C>();
        private static FilterDefinitionBuilder<C> __subject = Builders<C>.Filter;

        public enum E { A, B };

        public class C
        {
            public int Id;
            [BsonRepresentation(BsonType.String)]
            public E? P;
        }

        [Fact]
        public void Contains_should_render_correctly()
        {
            var values = (IEnumerable<E?>)new E?[] { null, E.A, E.B };
            var filter = __subject.Where(x => values.Contains(x.P));

            var result = filter.Render(__serializer, __registry);
            
            result.Should().Be("{ P : { $in : [ null, 'A', 'B' ] } }");
        }
    }
}
