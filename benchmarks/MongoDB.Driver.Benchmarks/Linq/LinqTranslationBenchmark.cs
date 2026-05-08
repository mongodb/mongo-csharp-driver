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
    private readonly string _activeStatus = "Active";

    private IBsonSerializer<OrderDocument> _orderSerializer;
    private ExpressionTranslationOptions _translationOptions;

    private Expression<Func<OrderDocument, bool>> _equalityByIdExpression;
    private Expression<Func<OrderDocument, bool>> _compoundFilterExpression;
    private Expression<Func<OrderDocument, bool>> _inListFilterExpression;
    private Expression<Func<OrderDocument, bool>> _stringMethodFilterExpression;
    private Expression<Func<OrderDocument, bool>> _arrayAnyExpression;
    private Expression<Func<OrderDocument, bool>> _nestedMemberFilterExpression;
    private Expression<Func<OrderDocument, bool>> _orChainFilterExpression;
    private Expression<Func<OrderDocument, bool>> _dateTimeMethodFilterExpression;
    private Expression<Func<OrderDocument, bool>> _instanceFieldCaptureExpression;

    private Expression<Func<OrderDocument, OrderDocument>> _wholeDocumentProjectionExpression;
    private Expression<Func<OrderDocument, OrderSummary>> _pocoProjectionExpression;
    private Expression<Func<OrderDocument, WideOrderProjection>> _widePocoProjectionExpression;
    private Expression<Func<OrderDocument, OrderItemIds>> _nestedTransformProjectionExpression;

    private MongoClient _queryClient;
    private MongoQueryProvider<OrderDocument> _queryProvider;
    private Expression _queryableChainExpression;

    [GlobalSetup]
    public void Setup()
    {
        _orderSerializer = BsonSerializer.LookupSerializer<OrderDocument>();
        _translationOptions = new ExpressionTranslationOptions();

        var targetId = 42;
        var statusFilter = "Active";
        var cutoff = new DateTime(2025, 1, 1);
        var ids = new[] { 1, 2, 3, 4, 5 };
        var prefix = "Acme";
        var priceThreshold = 100m;
        var city = "Seattle";
        var year = 2025;

        _equalityByIdExpression = x => x.Id == targetId;
        _compoundFilterExpression = x => x.Status == statusFilter && x.CreatedAt > cutoff;
        _inListFilterExpression = x => ids.Contains(x.Id);
        _stringMethodFilterExpression = x => x.CustomerName.StartsWith(prefix);
        _arrayAnyExpression = x => x.Items.Any(i => i.Price > priceThreshold);
        _nestedMemberFilterExpression = x => x.ShippingAddress.City == city;
        _orChainFilterExpression = x => x.Status == "Active" || x.Status == "Pending" || x.Status == "Processing" || x.Status == "Shipped";
        _dateTimeMethodFilterExpression = x => x.CreatedAt.Year == year;
        _instanceFieldCaptureExpression = x => x.Status == _activeStatus;

        _wholeDocumentProjectionExpression = x => x;
        _pocoProjectionExpression = x => new OrderSummary { Id = x.Id, Customer = x.CustomerName, Total = x.Total };
        _widePocoProjectionExpression = x => new WideOrderProjection
        {
            Id = x.Id,
            CustomerName = x.CustomerName,
            Status = x.Status,
            Total = x.Total,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            Currency = x.Currency,
            Subtotal = x.Subtotal,
            Tax = x.Tax,
            Discount = x.Discount,
            Notes = x.Notes,
            ItemCount = x.ItemCount,
            IsPaid = x.IsPaid,
            PaymentMethod = x.PaymentMethod,
            ShippingMethod = x.ShippingMethod
        };
        _nestedTransformProjectionExpression = x => new OrderItemIds { Id = x.Id, ProductIds = x.Items.Select(i => i.ProductId) };

        SetupQueryableChain(statusFilter);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _queryClient?.Dispose();
    }

    [Benchmark]
    public BsonDocument EqualityById()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _equalityByIdExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument CompoundFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _compoundFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument InListFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _inListFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument StringMethodFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _stringMethodFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument ArrayAnyWithPredicate()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _arrayAnyExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument NestedMemberFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _nestedMemberFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument OrChainFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _orChainFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public BsonDocument DateTimeMethodFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _dateTimeMethodFilterExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    // Captures an instance field instead of a stack-local. The expression tree
    // contains a member access on captured `this`, exercising a different
    // partial-evaluation path than the other filter benchmarks above.
    [Benchmark]
    public BsonDocument InstanceFieldCaptureFilter()
    {
        return LinqProviderAdapter.TranslateExpressionToFilter(
            _instanceFieldCaptureExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    // Sentinel: x => x takes the early-return special case in LinqProviderAdapter
    // and bypasses the translation pipeline. Movement here means the fast-path
    // detection itself regressed, not the translator.
    [Benchmark]
    public RenderedProjectionDefinition<OrderDocument> WholeDocumentProjectionSentinel()
    {
        return LinqProviderAdapter.TranslateExpressionToProjection(
            _wholeDocumentProjectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public RenderedProjectionDefinition<OrderSummary> PocoProjection()
    {
        return LinqProviderAdapter.TranslateExpressionToProjection(
            _pocoProjectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    // Same expression as PocoProjection but goes through the find-projection
    // simplifier (AstFindProjectionSimplifier), exercising a separate code path.
    [Benchmark]
    public RenderedProjectionDefinition<OrderSummary> FindProjection()
    {
        return LinqProviderAdapter.TranslateExpressionToFindProjection(
            _pocoProjectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public RenderedProjectionDefinition<WideOrderProjection> WidePocoProjection()
    {
        return LinqProviderAdapter.TranslateExpressionToProjection(
            _widePocoProjectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    [Benchmark]
    public RenderedProjectionDefinition<OrderItemIds> ProjectionWithNestedTransform()
    {
        return LinqProviderAdapter.TranslateExpressionToProjection(
            _nestedTransformProjectionExpression,
            _orderSerializer,
            BsonSerializer.SerializerRegistry,
            _translationOptions);
    }

    // Exercises the IQueryable composition path through MongoQueryProvider —
    // a different code path from the adapter shortcuts above, used when callers
    // write collection.AsQueryable().Where(...).Select(...) chains.
    [Benchmark]
    public object IQueryableComposition()
    {
        return ExpressionToExecutableQueryTranslator.Translate<OrderDocument, OrderSummary>(
            _queryProvider,
            _queryableChainExpression,
            _translationOptions);
    }

    private void SetupQueryableChain(string statusFilter)
    {
        var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
        var settings = mongoUri != null ? MongoClientSettings.FromConnectionString(mongoUri) : new MongoClientSettings();
        settings.ClusterSource = DisposingClusterSource.Instance;
        _queryClient = new MongoClient(settings);

        var collection = _queryClient.GetDatabase("linqbench").GetCollection<OrderDocument>("orders");
        var queryable = collection.AsQueryable();

        _queryProvider = (MongoQueryProvider<OrderDocument>)queryable.Provider;
        _queryableChainExpression = queryable
            .Where(x => x.Status == statusFilter)
            .Select(x => new OrderSummary { Id = x.Id, Customer = x.CustomerName, Total = x.Total })
            .OrderBy(s => s.Total)
            .Take(10)
            .Expression;
    }

    #region Test Models

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

    public class OrderSummary
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public decimal Total { get; set; }
    }

    public class WideOrderProjection
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
    }

    public class OrderItemIds
    {
        public int Id { get; set; }
        public IEnumerable<string> ProductIds { get; set; }
    }

    #endregion
}
