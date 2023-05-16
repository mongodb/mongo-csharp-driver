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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FindExpressionProjectionDefinitionTests
    {
        [Fact]
        public void Projection_to_class_should_work()
            => AssertProjection(
                x => new Projection { A = x.A, X = x.B },
                "{ A : 1, X : '$B', _id : 0 }");

        [Fact]
        public void Projection_to_anonymous_type_should_work()
            => AssertProjection(
                x => new { x.A, X = x.B },
                "{ A : 1, X : '$B', _id : 0 }");

        private void AssertProjection<TProjection>(
            Expression<Func<Document, TProjection>> expression,
            string expectedProjection)
        {
            var projection = new FindExpressionProjectionDefinition<Document, TProjection>(expression);

            var renderedProjection = projection.Render(
                BsonSerializer.LookupSerializer<Document>(),
                BsonSerializer.SerializerRegistry);

            renderedProjection.Document.Should().BeEquivalentTo(expectedProjection);
        }

        private class Document
        {
            public string A { get; set; }

            public int B { get; set; }
        }

        private class Projection
        {
            public string A { get; set; }

            public int X { get; set; }
        }
    }
}
