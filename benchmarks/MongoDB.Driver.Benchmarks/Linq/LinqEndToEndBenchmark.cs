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

    private BsonDocument _multiFieldFilter;
    private BsonDocument _orStatusFilter;
    private BsonDocument[] _groupByPipeline;

    [GlobalSetup]
    public void Setup()
    {
        _client = MongoConfiguration.CreateClient();
        var db = _client.GetDatabase(DatabaseName);
        _collection = db.GetCollection<OrderDocument>(CollectionName);

        SeedData();
        PreBuildQueries();
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

    // --- OrStatusFilter: simple OR filter ---

    [Benchmark]
    public List<OrderDocument> OrStatusFilterLinq()
    {
        return _collection.Find(x =>
            x.Status == "Active" || x.Status == "Pending" || x.Status == "Processing" || x.Status == "Shipped").ToList();
    }

    [Benchmark]
    public List<OrderDocument> OrStatusFilterRaw()
    {
        return _collection.Find(_orStatusFilter).ToList();
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

    private void PreBuildQueries()
    {
        _multiFieldFilter = new BsonDocument
        {
            { "Status", _statusFilter },
            { "CustomerName", new BsonDocument("$regex", $"^{_prefix}") },
            { "ShippingAddress.City", _city },
            { "CreatedAt", new BsonDocument("$gt", _cutoff) },
            { "IsPaid", new BsonDocument("$ne", true) }
        };

        _orStatusFilter = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("Status", "Active"),
            new BsonDocument("Status", "Pending"),
            new BsonDocument("Status", "Processing"),
            new BsonDocument("Status", "Shipped")
        });

        _groupByPipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Status" },
                { "Count", new BsonDocument("$sum", 1) },
                { "TotalRevenue", new BsonDocument("$sum", "$Total") }
            })
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
