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
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class WindowMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Translate_should_return_expected_result_for_AddToSet()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.AddToSet(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $addToSet : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResult = new[] { 1, 2, 3 };
            foreach (var result in results)
            {
                result["Result"].AsBsonArray.Select(i => i.AsInt32).Should().BeEquivalentTo(expectedResult);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.DecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$DecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(2.0M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.DoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$DoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(2.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(2.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.Int64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$Int64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(2.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_nullable_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.NullableDecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$NullableDecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(1.5M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_nullable_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.NullableDoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$NullableDoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_nullable_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.NullableInt32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$NullableInt32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_nullable_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.NullableInt64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$NullableInt64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_nullable_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.NullableSingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$NullableSingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Average_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Average(x => x.SingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $avg : '$SingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(2.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Count()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Count(null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $count : { } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(3);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.DecimalField1, x => x.DecimalField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$DecimalField1', '$DecimalField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().BeApproximately(0.6666M, 0.0001M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.DoubleField1, x => x.DoubleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$DoubleField1', '$DoubleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.6666, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.Int32Field1, x => x.Int32Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$Int32Field1', '$Int32Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.6666, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.Int64Field1, x => x.Int64Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$Int64Field1', '$Int64Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.6666, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_nullable_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.NullableDecimalField1, x => x.NullableDecimalField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$NullableDecimalField1', '$NullableDecimalField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(0.25M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_nullable_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.NullableDoubleField1, x => x.NullableDoubleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$NullableDoubleField1', '$NullableDoubleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.25);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_nullable_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.NullableInt32Field1, x => x.NullableInt32Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$NullableInt32Field1', '$NullableInt32Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.25);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_nullable_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.NullableInt64Field1, x => x.NullableInt64Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$NullableInt64Field1', '$NullableInt64Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.25);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_nullable_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.NullableSingleField1, x => x.NullableSingleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$NullableSingleField1', '$NullableSingleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.25);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovariancePopulation_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovariancePopulation(x => x.SingleField1, x => x.SingleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covariancePop : ['$SingleField1', '$SingleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.6666, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.DecimalField1, x => x.DecimalField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$DecimalField1', '$DecimalField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(1.0M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.DoubleField1, x => x.DoubleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$DoubleField1', '$DoubleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.Int32Field1, x => x.Int32Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$Int32Field1', '$Int32Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.Int64Field1, x => x.Int64Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$Int64Field1', '$Int64Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_nullable_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.NullableDecimalField1, x => x.NullableDecimalField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$NullableDecimalField1', '$NullableDecimalField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(0.5M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_nullable_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.NullableDoubleField1, x => x.NullableDoubleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$NullableDoubleField1', '$NullableDoubleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_nullable_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.NullableInt32Field1, x => x.NullableInt32Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$NullableInt32Field1', '$NullableInt32Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_nullable_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.NullableInt64Field1, x => x.NullableInt64Field2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$NullableInt64Field1', '$NullableInt64Field2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_nullable_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.NullableSingleField1, x => x.NullableSingleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$NullableSingleField1', '$NullableSingleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(0.5);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_CovarianceSample_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.CovarianceSample(x => x.SingleField1, x => x.SingleField2, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $covarianceSamp : ['$SingleField1', '$SingleField2'] } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(1.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_DenseRank()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.DenseRank() });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $denseRank : { } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { 1, 2, 3 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Derivative(x => x.DecimalField, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $derivative : { input : '$DecimalField' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0M, 1.0M };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Decimal_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Derivative(x => x.DecimalField, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $derivative : { input : '$DecimalField', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0M, 1.0M };
            for (var n = 0; n < results.Count; n++)
            {
                if (expectedResults[n].IsDecimal128)
                {
                    results[n]["Result"].AsDecimal.Should().BeApproximately(expectedResults[n].AsDecimal, 0.0001M);
                }
                else
                {
                    results[n]["Result"].Should().Be(expectedResults[n]);
                }
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Derivative(x => x.DoubleField, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $derivative : { input : '$DoubleField' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Double_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Derivative(x => x.DoubleField, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $derivative : { input : '$DoubleField', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                if (expectedResults[n].IsDouble)
                {
                    results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n].AsDouble, 0.0001);
                }
                else
                {
                    results[n]["Result"].Should().Be(expectedResults[n]);
                }
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Derivative(x => x.Int32Field, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $derivative : { input : '$Int32Field' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Int32_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Derivative(x => x.Int32Field, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $derivative : { input : '$Int32Field', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                if (expectedResults[n].IsDouble)
                {
                    results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n].AsDouble, 0.0001);
                }
                else
                {
                    results[n]["Result"].Should().Be(expectedResults[n]);
                }
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Derivative(x => x.Int64Field, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $derivative : { input : '$Int64Field' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Int64_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Derivative(x => x.Int64Field, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $derivative : { input : '$Int64Field', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                if (expectedResults[n].IsDouble)
                {
                    results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n].AsDouble, 0.0001);
                }
                else
                {
                    results[n]["Result"].Should().Be(expectedResults[n]);
                }
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Derivative(x => x.SingleField, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $derivative : { input : '$SingleField' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Derivative_with_Single_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Derivative(x => x.SingleField, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $derivative : { input : '$SingleField', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { BsonNull.Value, 1.0, 1.0 };
            for (var n = 0; n < results.Count; n++)
            {
                if (expectedResults[n].IsDouble)
                {
                    results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n].AsDouble, 0.0001);
                }
                else
                {
                    results[n]["Result"].Should().Be(expectedResults[n]);
                }
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_DocumentNumber()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.DocumentNumber() });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $documentNumber : { } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { 1, 2, 3 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Decimal_and_alpha()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.DecimalField, ExponentialMovingAverageWeighting.Alpha(0.5), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$DecimalField', alpha : 0.5 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0M, 1.5M, 2.25M };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDecimal.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Decimal_and_n()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.DecimalField, ExponentialMovingAverageWeighting.N(2), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$DecimalField', N : 2 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0M, 1.6666M, 2.5555M };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDecimal.Should().BeApproximately(expectedResults[n], 0.0001M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Double_and_alpha()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.DoubleField, ExponentialMovingAverageWeighting.Alpha(0.5), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$DoubleField', alpha : 0.5 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.5, 2.25 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Double_and_n()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.DoubleField, ExponentialMovingAverageWeighting.N(2), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$DoubleField', N : 2 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.6666, 2.5555 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n], 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Int32_and_alpha()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.Int32Field, ExponentialMovingAverageWeighting.Alpha(0.5), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$Int32Field', alpha : 0.5 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.5, 2.25 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Int32_and_n()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.Int32Field, ExponentialMovingAverageWeighting.N(2), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$Int32Field', N : 2 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.6666, 2.5555 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n], 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Int64_and_alpha()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.Int64Field, ExponentialMovingAverageWeighting.Alpha(0.5), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$Int64Field', alpha : 0.5 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.5, 2.25 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Int64_and_n()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.Int64Field, ExponentialMovingAverageWeighting.N(2), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$Int64Field', N : 2 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.6666, 2.5555 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n], 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Single_and_alpha()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.SingleField, ExponentialMovingAverageWeighting.Alpha(0.5), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$SingleField', alpha : 0.5 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.5, 2.25 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_ExponentialMovingAverage_with_Single_and_n()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.ExponentialMovingAverage(x => x.SingleField, ExponentialMovingAverageWeighting.N(2), null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $expMovingAvg : { input : '$SingleField', N : 2 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 1.0, 1.6666, 2.5555 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().BeApproximately(expectedResults[n], 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_First()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.First(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $first : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(1);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Integral(x => x.DecimalField, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $integral : { input : '$DecimalField' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0M, 1.5M, 2.5M };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].ToDecimal().Should().Be(expectedResults[n]); // Use ToDecimal instead of AsDecimal because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Decimal_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Integral(x => x.DecimalField, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $integral : { input : '$DecimalField', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0M, 1.5M, 2.5M };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].ToDecimal().Should().Be(expectedResults[n]); // Use ToDecimal instead of AsDecimal because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Integral(x => x.DoubleField, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $integral : { input : '$DoubleField' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].ToDouble().Should().Be(expectedResults[n]); // Use ToDouble instead of AsDouble because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Double_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Integral(x => x.DoubleField, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $integral : { input : '$DoubleField', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Integral(x => x.Int32Field, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $integral : { input : '$Int32Field' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].ToDouble().Should().Be(expectedResults[n]); // Use ToDouble instead of AsDouble because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Int32_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Integral(x => x.Int32Field, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $integral : { input : '$Int32Field', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Integral(x => x.Int64Field, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $integral : { input : '$Int64Field' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].ToDouble().Should().Be(expectedResults[n]); // Use ToDouble instead of AsDouble because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Int64_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Integral(x => x.Int64Field, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $integral : { input : '$Int64Field', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Integral(x => x.SingleField, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $integral : { input : '$SingleField' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].ToDouble().Should().Be(expectedResults[n]); // Use ToDouble instead of AsDouble because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Integral_with_Single_and_unit()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.DayField),
                    output: p => new { Result = p.Integral(x => x.SingleField, WindowTimeUnit.Day, DocumentsWindow.Create(-1, 0)) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { DayField : 1 }, output : { Result : { $integral : { input : '$SingleField', unit : 'day' }, window : { documents : [-1, 0] } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new[] { 0.0, 1.5, 2.5 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].AsDouble.Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Last()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Last(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $last : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(3);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Locf()
        {
            RequireServer.Check().Supports(Feature.SetWindowFieldsLocf);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Locf(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $locf : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Select(r => r["Result"].AsInt32).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Max()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Max(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $max : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(3);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Min()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Min(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $min : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(1);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Push()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Push(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $push : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].Should().Be(new BsonArray { 1, 2, 3 });
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Rank()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Rank() });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1 }, output : { Result : { $rank : { } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { 1, 2, 3 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Shift()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Shift(x => x.Int32Field, 1) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1  }, output : { Result : { $shift : { output : '$Int32Field', by : 1 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { 2, 3, BsonNull.Value };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Shift_with_defaultValue()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(
                    partitionBy: x => 1,
                    sortBy: Builders<C>.Sort.Ascending(x => x.Id),
                    output: p => new { Result = p.Shift(x => x.Int32Field, 1, 4) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { partitionBy : 1, sortBy : { _id : 1  }, output : { Result : { $shift : { output : '$Int32Field', by : 1, default : 4 } } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            var expectedResults = new BsonValue[] { 2, 3, 4 };
            for (var n = 0; n < results.Count; n++)
            {
                results[n]["Result"].Should().Be(expectedResults[n]);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.DecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$DecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].ToDecimal().Should().BeApproximately(0.8164M, 0.0001M); // Use ToDecimal instead of AsDecimal because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.DoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$DoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.8164, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.8164, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.Int64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$Int64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.8164, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_nullable_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.NullableDecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$NullableDecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].ToDecimal().Should().BeApproximately(0.5000M, 0.0001M); // Use ToDecimal instead of AsDecimal because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_nullable_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.NullableDoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$NullableDoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.5000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_nullable_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.NullableInt32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$NullableInt32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.5000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_nullable_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.NullableInt64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$NullableInt64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.5000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_nullable_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.NullableSingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$NullableSingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.5000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationPopulation_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationPopulation(x => x.SingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevPop : '$SingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.8164, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.DecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$DecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].ToDecimal().Should().BeApproximately(1.0000M, 0.0001M); // Use ToDecimal instead of AsDecimal because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.DoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$DoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(1.0000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(1.0000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.Int64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$Int64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(1.0000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_nullable_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.NullableDecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$NullableDecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].ToDecimal().Should().BeApproximately(0.7071M, 0.0001M); // Use ToDecimal instead of AsDecimal because sometimes the server returns some other numeric type
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_nullable_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.NullableDoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$NullableDoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.7071, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_nullable_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.NullableInt32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$NullableInt32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.7071, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_nullable_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.NullableInt64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$NullableInt64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.7071, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_nullable_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.NullableSingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$NullableSingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(0.7071, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_StandardDeviationSample_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.StandardDeviationSample(x => x.SingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $stdDevSamp : '$SingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().BeApproximately(1.0000, 0.0001);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.DecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$DecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(6.0M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.DoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$DoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(6.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.Int32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$Int32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(6);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.Int64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$Int64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt64.Should().Be(6L);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_nullable_Decimal()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.NullableDecimalField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$NullableDecimalField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDecimal.Should().Be(3.0M);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_nullable_Double()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.NullableDoubleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$NullableDoubleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(3.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_nullable_Int32()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.NullableInt32Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$NullableInt32Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt32.Should().Be(3);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_nullable_Int64()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.NullableInt64Field, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$NullableInt64Field' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsInt64.Should().Be(3L);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_nullable_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.NullableSingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$NullableSingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(3.0);
            }
        }

        [Fact]
        public void Translate_should_return_expected_result_for_Sum_with_Single()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = CreateCollection();

            var aggregate = collection.Aggregate()
                .SetWindowFields(output: p => new { Result = p.Sum(x => x.SingleField, null) });

            var stages = Translate(collection, aggregate);
            var expectedStages = new[] { "{ $setWindowFields : { output : { Result : { $sum : '$SingleField' } } } }" };
            AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            foreach (var result in results)
            {
                result["Result"].AsDouble.Should().Be(6.0);
            }
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();
            var documents = CreateTestDocuments();
            CreateCollection(collection, documents);
            return collection;
        }

        private IEnumerable<C> CreateTestDocuments()
        {
            var documents = new C[3];
            for (var n = 1; n <= 3; n++)
            {
                var document = new C
                {
                    Id = n,
                    DayField = new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddDays(n - 1),
                    DecimalField = n,
                    DecimalField1 = n,
                    DecimalField2 = n,
                    DoubleField = n,
                    DoubleField1 = n,
                    DoubleField2 = n,
                    Int32Field = n,
                    Int32Field1 = n,
                    Int32Field2 = n,
                    Int64Field = n,
                    Int64Field1 = n,
                    Int64Field2 = n,
                    NullableDecimalField = n < 3 ? n : null,
                    NullableDecimalField1 = n < 3 ? n : null,
                    NullableDecimalField2 = n < 3 ? n : null,
                    NullableDoubleField = n < 3 ? n : null,
                    NullableDoubleField1 = n < 3 ? n : null,
                    NullableDoubleField2 = n < 3 ? n : null,
                    NullableInt32Field = n < 3 ? n : null,
                    NullableInt32Field1 = n < 3 ? n : null,
                    NullableInt32Field2 = n < 3 ? n : null,
                    NullableInt64Field = n < 3 ? n : null,
                    NullableInt64Field1 = n < 3 ? n : null,
                    NullableInt64Field2 = n < 3 ? n : null,
                    NullableSingleField = n < 3 ? n : null,
                    NullableSingleField1 = n < 3 ? n : null,
                    NullableSingleField2 = n < 3 ? n : null,
                    SingleField = n,
                    SingleField1 = n,
                    SingleField2 = n,
                };

                documents[n - 1] = document;
            }

            return documents;
        }

        public class C
        {
            public int Id { get; set; }
            public DateTime DayField { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal DecimalField { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal DecimalField1 { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal DecimalField2 { get; set; }
            public double DoubleField { get; set; }
            public double DoubleField1 { get; set; }
            public double DoubleField2 { get; set; }
            public int Int32Field { get; set; }
            public int Int32Field1 { get; set; }
            public int Int32Field2 { get; set; }
            public long Int64Field { get; set; }
            public long Int64Field1 { get; set; }
            public long Int64Field2 { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal? NullableDecimalField { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal? NullableDecimalField1 { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal? NullableDecimalField2 { get; set; }
            public double? NullableDoubleField { get; set; }
            public double? NullableDoubleField1 { get; set; }
            public double? NullableDoubleField2 { get; set; }
            public int? NullableInt32Field { get; set; }
            public int? NullableInt32Field1 { get; set; }
            public int? NullableInt32Field2 { get; set; }
            public long? NullableInt64Field { get; set; }
            public long? NullableInt64Field1 { get; set; }
            public long? NullableInt64Field2 { get; set; }
            public float? NullableSingleField { get; set; }
            public float? NullableSingleField1 { get; set; }
            public float? NullableSingleField2 { get; set; }
            public float SingleField { get; set; }
            public float SingleField1 { get; set; }
            public float SingleField2 { get; set; }
        }
    }
}
