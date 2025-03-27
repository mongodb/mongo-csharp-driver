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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class SigmoidMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<SigmoidMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public SigmoidMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture, server => server.Supports(Feature.SigmoidOperator))
        {
        }

        [Fact]
        public void Sigmoid_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => Mql.Sigmoid(x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sigmoid : '$X' }, _id : 0 } }");

            var result = queryable.ToList();
            result.Should().BeEquivalentTo(new[] { 0.7310585786300049, 0.9933071490757153, 0.999997739675702, 0.9999999992417439});
        }

        [Fact]
        public void Sigmoid_with_non_numeric_representation_should_throw()
        {
            var exception = Record.Exception(() =>
            {
                var collection = Fixture.Collection;

                var queryable = collection
                    .AsQueryable()
                    .Select(x => Mql.Sigmoid(x.Y));

                Translate(collection, queryable);
            });

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception?.Message.Should().Contain("uses a non-numeric representation");
        }

        public class C
        {
            [BsonRepresentation(BsonType.String)]
            public double Y { get; set; }
            public double X { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new() { X = 1.0 },
                new() { X = 5.0 },
                new() { X = 13.0 },
                new() { X = 21.0 },
            ];
        }
    }
}