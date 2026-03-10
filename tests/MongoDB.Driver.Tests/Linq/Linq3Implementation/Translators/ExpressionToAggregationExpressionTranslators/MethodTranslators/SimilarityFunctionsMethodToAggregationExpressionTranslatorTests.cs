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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class SimilarityFunctionsMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<
    SimilarityFunctionsMethodToAggregationExpressionTranslatorTests.ClassFixture>
{
    public SimilarityFunctionsMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.SimilarityFunctions))
    {
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_float_arrays()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.FloatArray1, i.FloatArray2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$FloatArray1', '$FloatArray2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_float_lists()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.FloatList1, i.FloatList2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$FloatList1', '$FloatList2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_float_collections()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.FloatCollection1, i.FloatCollection2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$FloatCollection1', '$FloatCollection2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_float_memory()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.FloatMemory1, i.FloatMemory2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$FloatMemory1', '$FloatMemory2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_double_arrays()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.DoubleArray1, i.DoubleArray2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$DoubleArray1', '$DoubleArray2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_double_lists()
    {
        var normalize = false;
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.DoubleList1, i.DoubleList2, normalize));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$DoubleList1', '$DoubleList2'], score: false } }, _id: 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Should().BeApproximately(38.72, 0.1);
        results[1].Should().BeApproximately(3942.72, 0.1);
        results[2].Should().BeApproximately(394982.72, 0.1);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_double_collections()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.DoubleCollection1, i.DoubleCollection2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$DoubleCollection1', '$DoubleCollection2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_DotProduct_double_memory()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.DotProduct(i.DoubleMemory1, i.DoubleMemory2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityDotProduct: { vectors: ['$DoubleMemory1', '$DoubleMemory2'], score: true } }, _id: 0 } }");

        AssertDotProductResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_float_arrays()
    {
        var normalize = false;
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.FloatArray1, i.FloatArray2, normalize));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$FloatArray1', '$FloatArray2'], score: false } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_float_lists()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.FloatList1, i.FloatList2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$FloatList1', '$FloatList2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_float_collections()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.FloatCollection1, i.FloatCollection2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$FloatCollection1', '$FloatCollection2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_float_memory()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.FloatMemory1, i.FloatMemory2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$FloatMemory1', '$FloatMemory2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_double_arrays()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.DoubleArray1, i.DoubleArray2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$DoubleArray1', '$DoubleArray2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_double_lists()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.DoubleList1, i.DoubleList2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$DoubleList1', '$DoubleList2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_double_collections()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.DoubleCollection1, i.DoubleCollection2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$DoubleCollection1', '$DoubleCollection2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Cosine_double_memory()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Cosine(i.DoubleMemory1, i.DoubleMemory2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityCosine: { vectors: ['$DoubleMemory1', '$DoubleMemory2'], score: true } }, _id: 0 } }");

        AssertCosineResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_float_arrays()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.FloatArray1, i.FloatArray2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$FloatArray1', '$FloatArray2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_float_lists()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.FloatList1, i.FloatList2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$FloatList1', '$FloatList2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_float_collections()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.FloatCollection1, i.FloatCollection2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$FloatCollection1', '$FloatCollection2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_float_memory()
    {
        var normalize = false;
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.FloatMemory1, i.FloatMemory2, normalize));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$FloatMemory1', '$FloatMemory2'], score: false } }, _id: 0 } }");

        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Should().BeApproximately(5.72, 0.1);
        results[1].Should().BeApproximately(57.68, 0.1);
        results[2].Should().BeApproximately(577.29, 0.1);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_double_arrays()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.DoubleArray1, i.DoubleArray2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$DoubleArray1', '$DoubleArray2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_double_lists()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.DoubleList1, i.DoubleList2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$DoubleList1', '$DoubleList2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_double_collections()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.DoubleCollection1, i.DoubleCollection2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$DoubleCollection1', '$DoubleCollection2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    [Fact]
    public void SimilarityFunctions_Euclidean_double_memory()
    {
        var collection = Fixture.Collection;
        var queryable = collection
            .AsQueryable()
            .Select(i => SimilarityFunctions.Euclidean(i.DoubleMemory1, i.DoubleMemory2, true));

        var stages = Translate(collection, queryable);

        AssertStages(stages,
            "{ $project: { _v: { $similarityEuclidean: { vectors: ['$DoubleMemory1', '$DoubleMemory2'], score: true } }, _id: 0 } }");

        AssertEuclideanResults(queryable);
    }

    public class Data
    {
        public float[] FloatArray1 { get; set; }
        public float[] FloatArray2 { get; set; }
        public double[] DoubleArray1 { get; set; }
        public double[] DoubleArray2 { get; set; }

        public List<float> FloatList1 { get; set; }
        public List<float> FloatList2 { get; set; }
        public List<double> DoubleList1 { get; set; }
        public List<double> DoubleList2 { get; set; }

        public ICollection<float> FloatCollection1 { get; set; }
        public ICollection<float> FloatCollection2 { get; set; }
        public ICollection<double> DoubleCollection1 { get; set; }
        public ICollection<double> DoubleCollection2 { get; set; }

        public ReadOnlyMemory<float> FloatMemory1 { get; set; }
        public ReadOnlyMemory<float> FloatMemory2 { get; set; }
        public ReadOnlyMemory<double> DoubleMemory1 { get; set; }
        public ReadOnlyMemory<double> DoubleMemory2 { get; set; }
    }

    private void AssertDotProductResults(IQueryable<double> queryable)
    {
        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Should().BeApproximately(19.86, 0.1);
        results[1].Should().BeApproximately(1971.86, 0.1);
        results[2].Should().BeApproximately(197491.85, 0.1);
    }

    private void AssertEuclideanResults(IQueryable<double> queryable)
    {
        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Should().BeApproximately(0.149, 0.01);
        results[1].Should().BeApproximately(0.017, 0.01);
        results[2].Should().BeApproximately(0.0017, 0.01);
    }

    private void AssertCosineResults(IQueryable<double> queryable)
    {
        var results = queryable.ToList();
        results.Count.Should().Be(3);
        results[0].Should().BeApproximately(0.99, 0.1);
        results[1].Should().BeApproximately(0.99, 0.1);
        results[2].Should().BeApproximately(0.99, 0.1);
    }

    public sealed class ClassFixture : MongoCollectionFixture<Data>
    {
        protected override IEnumerable<Data> InitialData =>
        [
            new()
            {
                FloatArray1 = [1.1f, 2.2f, 3.3f],
                FloatArray2 = [4.4f, 5.5f, 6.6f],
                FloatList1 = [1.1f, 2.2f, 3.3f],
                FloatList2 = [4.4f, 5.5f, 6.6f],
                FloatCollection1 = [1.1f, 2.2f, 3.3f],
                FloatCollection2 = [4.4f, 5.5f, 6.6f],
                FloatMemory1 = new([1.1f, 2.2f, 3.3f]),
                FloatMemory2 = new([4.4f, 5.5f, 6.6f]),
                DoubleArray1 = [1.1, 2.2, 3.3],
                DoubleArray2 = [4.4, 5.5, 6.6],
                DoubleList1 = [1.1, 2.2, 3.3],
                DoubleList2 = [4.4, 5.5, 6.6],
                DoubleCollection1 = [1.1, 2.2, 3.3],
                DoubleCollection2 = [4.4, 5.5, 6.6],
                DoubleMemory1 = new([1.1, 2.2, 3.3]),
                DoubleMemory2 = new([4.4, 5.5, 6.6])
            },
            new()
            {
                FloatArray1 = [11.1f, 22.2f, 33.3f],
                FloatArray2 = [44.4f, 55.5f, 66.6f],
                FloatList1 = [11.1f, 22.2f, 33.3f],
                FloatList2 = [44.4f, 55.5f, 66.6f],
                FloatCollection1 = [11.1f, 22.2f, 33.3f],
                FloatCollection2 = [44.4f, 55.5f, 66.6f],
                FloatMemory1 = new([11.1f, 22.2f, 33.3f]),
                FloatMemory2 = new([44.4f, 55.5f, 66.6f]),
                DoubleArray1 = [11.1, 22.2, 33.3],
                DoubleArray2 = [44.4, 55.5, 66.6],
                DoubleList1 = [11.1, 22.2, 33.3],
                DoubleList2 = [44.4, 55.5, 66.6],
                DoubleCollection1 = [11.1, 22.2, 33.3],
                DoubleCollection2 = [44.4, 55.5, 66.6],
                DoubleMemory1 = new([11.1, 22.2, 33.3]),
                DoubleMemory2 = new([44.4, 55.5, 66.6])
            },
            new()
            {
                FloatArray1 = [111.1f, 222.2f, 333.3f],
                FloatArray2 = [444.4f, 555.5f, 666.6f],
                FloatList1 = [111.1f, 222.2f, 333.3f],
                FloatList2 = [444.4f, 555.5f, 666.6f],
                FloatCollection1 = [111.1f, 222.2f, 333.3f],
                FloatCollection2 = [444.4f, 555.5f, 666.6f],
                FloatMemory1 = new([111.1f, 222.2f, 333.3f]),
                FloatMemory2 = new([444.4f, 555.5f, 666.6f]),
                DoubleArray1 = [111.1, 222.2, 333.3],
                DoubleArray2 = [444.4, 555.5, 666.6],
                DoubleList1 = [111.1, 222.2, 333.3],
                DoubleList2 = [444.4, 555.5, 666.6],
                DoubleCollection1 = [111.1, 222.2, 333.3],
                DoubleCollection2 = [444.4, 555.5, 666.6],
                DoubleMemory1 = new([111.1, 222.2, 333.3]),
                DoubleMemory2 = new([444.4, 555.5, 666.6])
            },
        ];
    }
}
