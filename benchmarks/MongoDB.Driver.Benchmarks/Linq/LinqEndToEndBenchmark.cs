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
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using static MongoDB.Benchmarks.BenchmarkHelper;
using static MongoDB.Benchmarks.Linq.LinqTranslationBenchmark;

namespace MongoDB.Benchmarks.Linq;

[MemoryDiagnoser]
public class LinqEndToEndBenchmark
{
    private const string DatabaseName = "linqbench";
    private const string CollectionName = "orders";
    private const int SeedCount = 500;

    private IMongoClient _client;
    private IMongoCollection<OrderDocument> _collection;

    private readonly string _statusFilter = "Active";
    private readonly string _prefix = "Acme";
    private readonly string _city = "Seattle";
    private readonly DateTime _cutoff = new(2025, 1, 1);

    private readonly int[] _lookupIds = { 1, 50, 100, 200, 400 };
    private const int PageSize = 10;

    private BsonDocument _multiFieldFilter;
    private BsonDocument _orFilter;
    private BsonDocument[] _groupByPipeline;
    private BsonDocument _projectionFilter;
    private BsonDocument _projectionDoc;
    private BsonDocument _inFilter;
    private BsonDocument[] _pagedQueryPipeline;

    [GlobalSetup]
    public void Setup()
    {
        _client = MongoConfiguration.CreateClient(DatabaseName);
        var db = _client.GetDatabase(DatabaseName);
        _collection = db.GetCollection<OrderDocument>(CollectionName);

        SeedData();
        CreateIndexes();
        PreBuildQueries();
    }

    // Indexes minimize server-side filter/group time so cross-benchmark deltas reflect
    // translation cost rather than COLLSCAN variance. Without indexes, ~95% of e2e time
    // on the heavier benchmarks goes to the server, and translator changes are invisible.
    private void CreateIndexes()
    {
        _collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<OrderDocument>(Builders<OrderDocument>.IndexKeys.Ascending(x => x.Status)),
            new CreateIndexModel<OrderDocument>(Builders<OrderDocument>.IndexKeys.Ascending(x => x.CreatedAt)),
            new CreateIndexModel<OrderDocument>(Builders<OrderDocument>.IndexKeys.Ascending("ShippingAddress.City")),
        });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.DropDatabase(DatabaseName);
        _client.Dispose();
    }

    // --- MultiFieldSearch: compound filter ---

    [Benchmark]
    public List<OrderDocument> MultiFieldSearchLinq()
    {
        return _collection.Find(x =>
            x.Status == _statusFilter &&
            x.CustomerName.StartsWith(_prefix) &&
            x.ShippingAddress.City == _city &&
            x.CreatedAt > _cutoff &&
            !x.IsPaid).ToList();
    }

    [Benchmark]
    public List<OrderDocument> MultiFieldSearchRaw()
    {
        return _collection.Find(_multiFieldFilter).ToList();
    }

    // --- OrFilter: simple OR filter ---

    [Benchmark]
    public List<OrderDocument> OrFilterLinq()
    {
        return _collection.Find(x =>
            x.Status == "Active" || x.Status == "Pending" || x.Status == "Processing" || x.Status == "Shipped").ToList();
    }

    [Benchmark]
    public List<OrderDocument> OrFilterRaw()
    {
        return _collection.Find(_orFilter).ToList();
    }

    // --- GroupBy aggregate ---

    [Benchmark]
    public List<BsonDocument> GroupByLinq()
    {
        return _collection.Aggregate()
            .Group(x => x.Status, g => new { Status = g.Key, Count = g.Count(), TotalRevenue = g.Sum(x => x.Total) })
            .As<BsonDocument>()
            .ToList();
    }

    [Benchmark]
    public List<BsonDocument> GroupByRaw()
    {
        var pipeline = PipelineDefinition<OrderDocument, BsonDocument>.Create(_groupByPipeline);
        return _collection.Aggregate(pipeline).ToList();
    }

    // --- Projection: Find with .Project to a typed DTO ---

    [Benchmark]
    public List<OrderSummary> ProjectionLinq()
    {
        return _collection.Find(x => x.Status == _statusFilter)
            .Project(x => new OrderSummary { CustomerName = x.CustomerName, Total = x.Total })
            .ToList();
    }

    [Benchmark]
    public List<OrderSummary> ProjectionRaw()
    {
        return _collection.Find(_projectionFilter)
            .Project<OrderSummary>(_projectionDoc)
            .ToList();
    }

    // --- InFilter: array Contains → $in ---

    [Benchmark]
    public List<OrderDocument> InFilterLinq()
    {
        return _collection.Find(x => _lookupIds.Contains(x.Id)).ToList();
    }

    [Benchmark]
    public List<OrderDocument> InFilterRaw()
    {
        return _collection.Find(_inFilter).ToList();
    }

    // --- PagedQuery: AsQueryable Where → OrderBy → ThenBy → Take, translates to a pipeline ---

    [Benchmark]
    public List<OrderDocument> PagedQueryLinq()
    {
        return _collection.AsQueryable()
            .Where(x => x.Status == _statusFilter)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.CustomerName)
            .Take(PageSize)
            .ToList();
    }

    [Benchmark]
    public List<OrderDocument> PagedQueryRaw()
    {
        var pipeline = PipelineDefinition<OrderDocument, OrderDocument>.Create(_pagedQueryPipeline);
        return _collection.Aggregate(pipeline).ToList();
    }

    private void PreBuildQueries()
    {
        // The Raw filter shapes below mirror exactly what the LINQ provider emits
        // for the matching Linq benchmark — verified by translating the LINQ expressions
        // with LinqProviderAdapter and comparing the rendered BSON. Equivalence matters
        // because LINQ-vs-Raw deltas are meant to isolate translation cost, not query-shape cost.

        // StartsWith translates to BsonRegularExpression with "s" (dot-matches-all) option,
        // serialized as { "$regularExpression": { "pattern": ..., "options": "s" } }
        _multiFieldFilter = new BsonDocument
        {
            { "Status", _statusFilter },
            { "CustomerName", new BsonRegularExpression($"^{_prefix}", "s") },
            { "ShippingAddress.City", _city },
            { "CreatedAt", new BsonDocument("$gt", _cutoff) },
            { "IsPaid", new BsonDocument("$ne", true) }
        };

        _orFilter = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("Status", "Active"),
            new BsonDocument("Status", "Pending"),
            new BsonDocument("Status", "Processing"),
            new BsonDocument("Status", "Shipped")
        });

        // The LINQ Group(keySelector, resultSelector) emits two stages: a $group with
        // auto-generated __agg0/__agg1 aliases, and a $project that renames them back
        // and the key to the user-visible field names.
        _groupByPipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Status" },
                { "__agg0", new BsonDocument("$sum", 1) },
                { "__agg1", new BsonDocument("$sum", "$Total") }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "Status", "$_id" },
                { "Count", "$__agg0" },
                { "TotalRevenue", "$__agg1" },
                { "_id", 0 }
            })
        };

        // Find().Project(x => new OrderSummary { ... }) emits a server-side inclusion projection.
        // The exact shape is verified against LinqProviderAdapter.TranslateExpressionToFindProjection.
        _projectionFilter = new BsonDocument("Status", _statusFilter);
        _projectionDoc = new BsonDocument
        {
            { "CustomerName", 1 },
            { "Total", 1 },
            { "_id", 0 }
        };

        // ids.Contains(x.Id) translates to { "_id": { "$in": [...] } } since Id maps to _id by convention.
        _inFilter = new BsonDocument("_id", new BsonDocument("$in", new BsonArray(_lookupIds.Select(id => (BsonValue)id))));

        // AsQueryable().Where(...).OrderBy(...).ThenBy(...).Take(n) translates to a pipeline:
        // $match, then $sort with the two keys, then $limit. Verified via translator dump.
        _pagedQueryPipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("Status", _statusFilter)),
            new BsonDocument("$sort", new BsonDocument { { "CreatedAt", 1 }, { "CustomerName", 1 } }),
            new BsonDocument("$limit", PageSize)
        };
    }

    private void SeedData()
    {
        var statuses = new[] { "Active", "Pending", "Processing", "Shipped", "Cancelled" };
        var cities = new[] { "Seattle", "Portland", "Denver", "Austin", "Boston" };
        var names = new[] { "Acme Corp", "Acme Inc", "Globex", "Initech", "Umbrella" };
        var random = new Random(42);

        var documents = Enumerable.Range(0, SeedCount).Select(i => new OrderDocument
        {
            Id = i,
            CustomerName = names[i % names.Length],
            Status = statuses[i % statuses.Length],
            Total = random.Next(10, 1000),
            Subtotal = random.Next(10, 900),
            Tax = random.Next(1, 100),
            Discount = random.Next(0, 50),
            CreatedAt = new DateTime(2024, 1, 1).AddDays(random.Next(0, 730)),
            IsPaid = i % 3 != 0,
            Currency = "USD",
            ItemCount = random.Next(1, 10),
            PaymentMethod = "Card",
            ShippingMethod = "Standard",
            ShippingAddress = new Address
            {
                Street = $"{i} Main St",
                City = cities[i % cities.Length],
                State = "WA",
                Zip = "98101",
                Country = "US"
            },
            Items = Enumerable.Range(0, random.Next(1, 5)).Select(j => new OrderItem
            {
                ProductId = $"prod-{j}",
                Price = random.Next(5, 200)
            }).ToList()
        }).ToList();

        _collection.InsertMany(documents);
    }
}

public class OrderSummary
{
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
}
