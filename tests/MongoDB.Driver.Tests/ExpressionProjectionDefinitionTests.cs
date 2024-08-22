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
    public class ExpressionProjectionDefinitionTests
    {
        [Theory]
        [InlineData(true, "{ X : 1, Y : '$X', _id : 0 }")]
        [InlineData(false, "{ X : '$X', Y: '$X', _id : 0 }")]
        public void Projection_Render_should_respect_RenderToFind_parameter(bool renderForFind, string expectedProjection)
            => AssertProjection(
                x => new { X = x.X, Y = x.X },
                renderForFind,
                expectedProjection);

        private void AssertProjection<TProjection>(
            Expression<Func<C, TProjection>> expression,
            bool renderForFind,
            string expectedProjection)
        {
            var projection = new ExpressionProjectionDefinition<C, TProjection>(expression);

            var renderedProjection = projection.Render(new(BsonSerializer.LookupSerializer<C>(), BsonSerializer.SerializerRegistry, renderForFind: renderForFind));

            renderedProjection.Document.Should().BeEquivalentTo(expectedProjection);
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
