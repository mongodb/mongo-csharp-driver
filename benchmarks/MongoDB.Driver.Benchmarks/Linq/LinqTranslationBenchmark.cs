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
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;

namespace MongoDB.Benchmarks.Linq;

[MemoryDiagnoser]
[BenchmarkCategory(DriverBenchmarkCategory.LinqBench)]
public class LinqTranslationBenchmark
{
    private IBsonSerializer<OrderDocument> _orderSerializer;
    private ExpressionTranslationOptions _translationOptions;

    private Expression<Func<OrderDocument, bool>> _multiFieldSearchExpression;
    private Expression<Func<OrderDocument, bool>> _orFilterExpression;
    private Expression<Func<OrderDocument, bool>> _batchLookupExpression;
    private Expression<Func<OrderDocument, bool>> _arrayElementQueryExpression;

    private Expression<Func<OrderDocument, string>> _fieldSelectionExpression;

    private Expression<Func<OrderDocument, OrderProjection>> _aggregationProjectionExpression;
    private Expression<Func<OrderDocument, OrderDocument>> _projectionSentinelExpression;

    private Expression<Func<OrderDocument, SetFields>> _updatePipelineExpression;

    private MongoClient _queryClient;
    private MongoQueryProvider<OrderDocument> _queryableProvider;
    private Expression _queryablePipelineExpression;
    private Expression _groupByAggregationExpression;

    [GlobalSetup]
    public void Setup()
    {
        _orderSerializer = BsonSerializer.LookupSerializer<OrderDocument>();
        _translationOptions = new ExpressionTranslationOptions();

        var statusFilter = "Active";
        var cutoff = new DateTime(2025, 1, 1);
        var prefix = "Acme";
        var city = "Seattle";
        var ids = new[] { 1, 2, 3, 4, 5 };
        var priceThreshold = 100m;

        _multiFieldSearchExpression = x =>
            x.Status == statusFilter &&
            x.CustomerName.StartsWith(prefix) &&
            x.ShippingAddress.City == city &&
            x.CreatedAt > cutoff &&
            !x.IsPaid;

        _orFilterExpression = x =>
            x.Status == "Active" || x.Status == "Pending" || x.Status == "Processing" || x.Status == "Shipped";

        _batchLookupExpression = x => ids.Contains(x.Id);

        _arrayElementQueryExpression = x => x.Items.Any(i => i.Price > priceThreshold);

        _fieldSelectionExpression = x => x.Items[0].ProductId;

        _aggregationProjectionExpression = x => new OrderProjection
        {
            Id = x.Id,
            Customer = x.CustomerName,
            Total = x.Subtotal + x.Tax - x.Discount,
            ProductIds = x.Items.Select(i => i.ProductId)
        };

        _projectionSentinelExpression = x => x;

        _updatePipelineExpression = x => new SetFields
        {
            Status = "Shipped",
            UpdatedAt = DateTime.UtcNow,
            Total = x.Subtotal + x.Tax - x.Discount
        };

        SetupQueryableExpressions(statusFilter);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _queryClient?.Dispose();
    }

    [Benchmark]
    public BsonDocument MultiFieldSearch()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _multiFieldSearchExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument OrFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _orFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument BatchLookup()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _batchLookupExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument ArrayElementQuery()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _arrayElementQueryExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public RenderedFieldDefinition FieldSelection()
    {
        return LinqProviderAdapter.TranslateExpressionToField(
            _fieldSelectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions,
            subPathRoot: null);
    }

    [Benchmark]
    public RenderedProjectionDefinition<OrderProjection> AggregationProjection()
    {
        return LinqProviderAdapter.TranslateExpressionToProjection(
            _aggregationProjectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    // x => x takes the early-return special case in LinqProviderAdapter
    // and bypasses the translation pipeline. Movement here means the fast-path
    // detection itself regressed, not the translator.
    [Benchmark]
    public RenderedProjectionDefinition<OrderDocument> ProjectionSentinel()
    {
        return LinqProviderAdapter.TranslateExpressionToProjection(
            _projectionSentinelExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument UpdatePipeline()
    {
        return LinqProviderAdapter.TranslateExpressionToSetStage(
            _updatePipelineExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public object QueryablePipeline()
    {
        return ExpressionToExecutableQueryTranslator.Translate<OrderDocument, OrderProjection>(
            _queryableProvider,
            _queryablePipelineExpression,
            _translationOptions);
    }

    [Benchmark]
    public object GroupByAggregation()
    {
        return ExpressionToExecutableQueryTranslator.Translate<OrderDocument, GroupResult>(
            _queryableProvider,
            _groupByAggregationExpression,
            _translationOptions);
    }

    private void SetupQueryableExpressions(string statusFilter)
    {
        var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
        var settings = mongoUri != null ? MongoClientSettings.FromConnectionString(mongoUri) : new MongoClientSettings();
        settings.ClusterSource = DisposingClusterSource.Instance;
        _queryClient = new MongoClient(settings);

        var collection = _queryClient.GetDatabase("linqbench").GetCollection<OrderDocument>("orders");
        var queryable = collection.AsQueryable();

        _queryableProvider = (MongoQueryProvider<OrderDocument>)queryable.Provider;

        _queryablePipelineExpression = queryable
            .Where(x => x.Status == statusFilter)
            .Select(x => new OrderProjection { Id = x.Id, Customer = x.CustomerName, Total = x.Total, ProductIds = x.Items.Select(i => i.ProductId) })
            .OrderBy(s => s.Total)
            .Take(10)
            .Expression;

        _groupByAggregationExpression = queryable
            .GroupBy(x => x.Status)
            .Select(g => new GroupResult { Status = g.Key, Count = g.Count(), TotalRevenue = g.Sum(x => x.Total) })
            .Expression;
    }

    #region Models

    public class OrderDocument
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Currency { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public string Notes { get; set; }
        public int ItemCount { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentMethod { get; set; }
        public string ShippingMethod { get; set; }
        public Address ShippingAddress { get; set; }
#pragma warning disable CA2227 // Collection properties should be read only
        public List<OrderItem> Items { get; set; }
#pragma warning restore CA2227
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderProjection
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public decimal Total { get; set; }
        public IEnumerable<string> ProductIds { get; set; }
    }

    public class SetFields
    {
        public string Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal Total { get; set; }
    }

    public class GroupResult
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    #endregion
}
