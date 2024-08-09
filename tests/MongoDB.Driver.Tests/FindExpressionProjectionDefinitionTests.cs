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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FindExpressionProjectionDefinitionTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Projection_to_class_should_work(
            [Values(false, true)] bool renderForFind)
        {
            var expectedRenderedProjection = renderForFind switch
            { 
                true => "{ A : 1, X : '$B', _id : 0 }",
                false => "{ A : '$A', X : '$B', _id : 0 }"
            };

            AssertProjection(
                x => new Projection { A = x.A, X = x.B },
                renderForFind,
                expectedRenderedProjection);
        }

        [Theory]
        [ParameterAttributeData]
        public void Projection_to_anonymous_type_should_work(
            [Values(false, true)] bool renderForFind)
        {
            var expectedRenderedProjection = renderForFind switch
            {
                true => "{ A : 1, X : '$B', _id : 0 }",
                false => "{ A : '$A', X : '$B', _id : 0 }"
            };

            AssertProjection(
                x => new { x.A, X = x.B },
                renderForFind,
                expectedRenderedProjection);
        }

        private void AssertProjection<TProjection>(
            Expression<Func<Document, TProjection>> expression,
            bool renderForFind,
            string expectedRenderedProjection)
        {
            var projection = new FindExpressionProjectionDefinition<Document, TProjection>(expression);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<Document>();

            var renderedProjection = projection.Render(new RenderArgs<Document>(documentSerializer, serializerRegistry, renderForFind: renderForFind));

            renderedProjection.Document.Should().BeEquivalentTo(expectedRenderedProjection);
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
