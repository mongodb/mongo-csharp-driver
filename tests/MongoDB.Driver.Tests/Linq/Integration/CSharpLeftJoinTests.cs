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
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class CSharpLeftJoinTests : LinqIntegrationTest<CSharpLeftJoinTests.ClassFixture>
{
    public CSharpLeftJoinTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    // LeftJoin projecting into a Tuple, materialized (Include/navigation property case)
    [Fact]
    public void LeftJoin_with_tuple_should_work()
    {
        var orders = Fixture.OrdersCollection;

        var queryable = orders.AsQueryable()
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable(),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => Tuple.Create(o, c));

        var stages = Translate(orders, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'customers', localField : '_outer.CustomerId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { _v : ['$_outer', '$_inner'], _id : 0 } }");

        var results = queryable.ToList();
        results.Should().HaveCount(3);
        results.Select(r => r.Item1.Id).Should().BeEquivalentTo([1, 2, 3]);
        // Order 3 has no matching customer (FK = 99), inner should be null
        var orderWithNoCustomer = results.Single(r => r.Item1.Id == 3);
        orderWithNoCustomer.Item2.Should().BeNull();
    }

    // LeftJoin followed by Select — mid-pipeline source example 1
    [Fact]
    public void LeftJoin_with_anonymous_type_followed_by_Select_should_work()
    {
        var orders = Fixture.OrdersCollection;

        var queryable = orders.AsQueryable()
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable(),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { Outer = o, Inner = c })
            .Select(ti => ti.Inner.Name);

        var stages = Translate(orders, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'customers', localField : '_outer.CustomerId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { Outer : '$_outer', Inner : '$_inner', _id : 0 } }",
            "{ $project : { _v : '$Inner.Name', _id : 0 } }");

        var results = queryable.ToList();
        results.Should().BeEquivalentTo(["Alice", "Bob", null]);
    }

    // LeftJoin followed by Where on inner + Select on outer — mid-pipeline source example 2
    [Fact]
    public void LeftJoin_with_anonymous_type_followed_by_Where_and_Select_should_work()
    {
        var orders = Fixture.OrdersCollection;

        var queryable = orders.AsQueryable()
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable(),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { Outer = o, Inner = c })
            .Where(ti => ti.Inner.Name == "Alice")
            .Select(ti => new { ti.Outer.Id, ti.Outer.Description });

        var stages = Translate(orders, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'customers', localField : '_outer.CustomerId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { Outer : '$_outer', Inner : '$_inner', _id : 0 } }",
            "{ $match : { 'Inner.Name' : 'Alice' } }",
            "{ $project : { _id : '$Outer._id', Description : '$Outer.Description' } }");

        var results = queryable.ToList();
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(1);
        results[0].Description.Should().Be("Order for Alice");
    }

    // Chained LeftJoins (ThenInclude / multi-level navigation)
    [Fact]
    public void Chained_LeftJoins_should_work()
    {
        var orderDetails = Fixture.OrderDetailsCollection;

        var queryable = orderDetails.AsQueryable()
            .LeftJoin(
                Fixture.OrdersCollection.AsQueryable(),
                od => od.OrderId,
                o => o.Id,
                (od, o) => new { Outer = od, Inner = o })
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable(),
                ti => ti.Inner.CustomerId,
                c => c.Id,
                (ti, c) => new { od = ti.Outer, Order = ti.Inner, Customer = c });

        var stages = Translate(orderDetails, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'orders', localField : '_outer.OrderId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { Outer : '$_outer', Inner : '$_inner', _id : 0 } }",
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'customers', localField : '_outer.Inner.CustomerId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { od : '$_outer.Outer', Order : '$_outer.Inner', Customer : '$_inner', _id : 0 } }");

        var results = queryable.ToList();
        results.Should().HaveCount(2);
    }

    // Chained LeftJoins with Select projection
    [Fact]
    public void Chained_LeftJoins_with_Select_should_work()
    {
        var orderDetails = Fixture.OrderDetailsCollection;

        var queryable = orderDetails.AsQueryable()
            .LeftJoin(
                Fixture.OrdersCollection.AsQueryable(),
                od => od.OrderId,
                o => o.Id,
                (od, o) => new { Outer = od, Inner = o })
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable(),
                ti => ti.Inner.CustomerId,
                c => c.Id,
                (ti, c) => new { od = ti.Outer, Order = ti.Inner, Customer = c })
            .Select(r => new { r.od.ProductId, r.Order.Description, CustomerName = r.Customer.Name });

        var stages = Translate(orderDetails, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'orders', localField : '_outer.OrderId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { Outer : '$_outer', Inner : '$_inner', _id : 0 } }",
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'customers', localField : '_outer.Inner.CustomerId', foreignField : '_id', as : '_inner' } }",
            "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
            "{ $project : { od : '$_outer.Outer', Order : '$_outer.Inner', Customer : '$_inner', _id : 0 } }",
            "{ $project : { ProductId : '$od.ProductId', Description : '$Order.Description', CustomerName : '$Customer.Name', _id : 0 } }");

        var results = queryable.ToList();
        results.Should().HaveCount(2);
    }

    // LeftJoin preserves outer documents when no matching inner exists
    [Fact]
    public void LeftJoin_should_preserve_outer_when_no_matching_inner()
    {
        var orders = Fixture.OrdersCollection;

        // Order 3 has CustomerId=99 which matches no customer
        var queryable = orders.AsQueryable()
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable(),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { OrderId = o.Id, CustomerName = c.Name });

        var results = queryable.ToList();
        results.Should().HaveCount(3);
        results.Select(r => r.OrderId).Should().BeEquivalentTo([1, 2, 3]);
        var noMatch = results.Single(r => r.OrderId == 3);
        noMatch.CustomerName.Should().BeNull();
    }

    // A filter chained onto the inner queryable must be honored: a row whose only candidate
    // inner match is filtered out gets a null inner, preserving left-join semantics.
    [Fact]
    public void LeftJoin_with_filtered_inner_queryable_should_apply_filter()
    {
        var orders = Fixture.OrdersCollection;

        // Only Alice (id=10) should participate as an inner match.
        var queryable = orders.AsQueryable()
            .LeftJoin(
                Fixture.CustomersCollection.AsQueryable().Where(c => c.Name == "Alice"),
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { OrderId = o.Id, CustomerName = c.Name });

        var results = queryable.ToList();
        results.Should().HaveCount(3);

        // Order 2 (CustomerId=20) only matches Bob, who is filtered out of the inner source,
        // so its inner match is null.
        var order2 = results.Single(r => r.OrderId == 2);
        order2.CustomerName.Should().BeNull();
    }

#if NET10_0_OR_GREATER
    // A nested Enumerable.LeftJoin on an array member is routed to the aggregation-expression
    // translator (inside the Select body), not the $lookup pipeline translator. It is currently
    // unsupported: $lookup cannot appear inside an aggregation expression, so this cannot reuse
    // the Queryable.LeftJoin mapping and has no $map/$filter-based translator yet.
    [Fact]
    public void Nested_Enumerable_LeftJoin_on_array_member_is_not_supported()
    {
        var collection = Fixture.BlogsCollection;

        var queryable = collection.AsQueryable()
            .Select(b => b.Posts.LeftJoin(
                b.Authors,
                p => p.AuthorId,
                a => a.Id,
                (p, a) => new { p.Title, AuthorName = a.Name }));

        var exception = Record.Exception(() => Translate(collection, queryable));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }
#endif

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Description { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProductId { get; set; }
    }

#if NET10_0_OR_GREATER
    public class Blog
    {
        public int Id { get; set; }
        public Post[] Posts { get; set; }
        public Author[] Authors { get; set; }
    }

    public class Post
    {
        public string Title { get; set; }
        public int AuthorId { get; set; }
    }

    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
#endif

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<Order> OrdersCollection { get; private set; }
        public IMongoCollection<Customer> CustomersCollection { get; private set; }
        public IMongoCollection<OrderDetail> OrderDetailsCollection { get; private set; }
#if NET10_0_OR_GREATER
        public IMongoCollection<Blog> BlogsCollection { get; private set; }
#endif

        protected override void InitializeFixture()
        {
            OrdersCollection = CreateCollection<Order>("orders");
            OrdersCollection.InsertMany([
                new Order { Id = 1, CustomerId = 10, Description = "Order for Alice" },
                new Order { Id = 2, CustomerId = 20, Description = "Order for Bob" },
                new Order { Id = 3, CustomerId = 99, Description = "Order with no customer" }]);

            CustomersCollection = CreateCollection<Customer>("customers");
            CustomersCollection.InsertMany([
                new Customer { Id = 10, Name = "Alice" },
                new Customer { Id = 20, Name = "Bob" }]);

            OrderDetailsCollection = CreateCollection<OrderDetail>("orderDetails");
            OrderDetailsCollection.InsertMany([
                new OrderDetail { Id = 100, OrderId = 1, ProductId = "P1" },
                new OrderDetail { Id = 101, OrderId = 2, ProductId = "P2" }]);
#if NET10_0_OR_GREATER
            BlogsCollection = CreateCollection<Blog>("blogs");
#endif
        }
    }
}
