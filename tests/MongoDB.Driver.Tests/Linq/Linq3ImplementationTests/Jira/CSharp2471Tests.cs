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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp2471Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData("$acos", 1.0, 0.0)]
#if NETCOREAPP3_1_OR_GREATER
        [InlineData("$acosh", 1.0, 0.0)]
#endif
        [InlineData("$asin", 0.0, 0.0)]
#if NETCOREAPP3_1_OR_GREATER
        [InlineData("$asinh", 0.0, 0.0)]
#endif
        [InlineData("$atan", 0.0, 0.0)]
#if NETCOREAPP3_1_OR_GREATER
        [InlineData("$atanh", 0.0, 0.0)]
#endif
        [InlineData("$cos", 0.0, 1.0)]
        [InlineData("$cosh", 0.0, 1.0)]
        [InlineData("$degreesToRadians", 0.0, 0.0)]
        [InlineData("$radiansToDegrees", 0.0, 0.0)]
        [InlineData("$sin", 0.0, 0.0)]
        [InlineData("$sinh", 0.0, 0.0)]
        [InlineData("$tan", 0.0, 0.0)]
        [InlineData("$tanh", 0.0, 0.0)]
        public void Trig_method_should_work(string trigOperator, double x, double expectedResult)
        {
            RequireServer.Check().Supports(Feature.TrigOperators);
            var collection = CreateCollection(x);

            Expression<Func<C, double>> projection = trigOperator switch
            {
                "$acos" => x => Math.Acos(x.X),
#if NETCOREAPP3_1_OR_GREATER
                "$acosh" => x => Math.Acosh(x.X),
#endif
                "$asin" => x => Math.Asin(x.X),
#if NETCOREAPP3_1_OR_GREATER
                "$asinh" => x => Math.Asinh(x.X),
#endif
                "$atan" => x => Math.Atan(x.X),
#if NETCOREAPP3_1_OR_GREATER
                "$atanh" => x => Math.Atanh(x.X),
#endif
                "$cos" => x => Math.Cos(x.X),
                "$cosh" => x => Math.Cosh(x.X),
                "$degreesToRadians" => x => MongoDBMath.DegreesToRadians(x.X),
                "$radiansToDegrees" => x => MongoDBMath.RadiansToDegrees(x.X),
                "$sin" => x => Math.Sin(x.X),
                "$sinh" => x => Math.Sinh(x.X),
                "$tan" => x => Math.Tan(x.X),
                "$tanh" => x => Math.Tanh(x.X),
                _ => throw new Exception($"Invalid trig operator: {trigOperator}.")
            };

            var queryable = collection
                .AsQueryable()
                .Select(projection);

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {trigOperator} : '$X' }}, _id : 0 }} }}");

            var result = queryable.Single();
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Atan2_should_work()
        {
            RequireServer.Check().Supports(Feature.TrigOperators);
            var collection = CreateCollection(0.0);

            var queryable = collection
                .AsQueryable()
                .Select(x => Math.Atan2(x.X, 0.0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $atan2 : ['$X', 0.0] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(0.0);
        }

        private IMongoCollection<C> CreateCollection(double x)
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, X = x });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public double X { get; set; }
        }
    }
}
