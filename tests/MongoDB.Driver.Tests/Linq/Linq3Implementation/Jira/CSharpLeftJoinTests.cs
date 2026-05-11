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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharpLeftJoinTests : Linq3IntegrationTest
    {
        // LeftJoin with LeftJoinResult as the result type (Include/navigation property case)
        [Fact]
        public void LeftJoin_with_LeftJoinResult_should_work()
        {
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            var queryable = orders.AsQueryable()
                .LeftJoin(
                    customers.AsQueryable(),
                    o => o.CustomerId,
                    c => c.Id,
                    (o, c) => new LeftJoinResult<Order, Customer> { Outer = o, Inner = c });

            var stages = Translate(orders, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'customers', localField : '_outer.CustomerId', foreignField : '_id', as : '_inner' } }",
                "{ $unwind : { path : '$_inner', preserveNullAndEmptyArrays : true } }",
                "{ $project : { Outer : '$_outer', Inner : '$_inner', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(3);
            results.Select(r => r.Outer.Id).Should().BeEquivalentTo([1, 2, 3]);
            // Order 3 has no matching customer (FK = 99), inner should be null
            var orderWithNoCustomer = results.Single(r => r.Outer.Id == 3);
            orderWithNoCustomer.Inner.Should().BeNull();
        }

        // LeftJoin followed by Select — mid-pipeline source example 1
        [Fact]
        public void LeftJoin_with_anonymous_type_followed_by_Select_should_work()
        {
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            var queryable = orders.AsQueryable()
                .LeftJoin(
                    customers.AsQueryable(),
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
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            var queryable = orders.AsQueryable()
                .LeftJoin(
                    customers.AsQueryable(),
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

        // LeftJoin using IMongoCollection overload
        [Fact]
        public void LeftJoin_with_IMongoCollection_overload_should_work()
        {
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            var queryable = orders.AsQueryable()
                .LeftJoin(
                    customers,
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
        }

        // Chained LeftJoins (ThenInclude / multi-level navigation)
        [Fact]
        public void Chained_LeftJoins_should_work()
        {
            var orderDetails = CreateOrderDetailsCollection();
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            var queryable = orderDetails.AsQueryable()
                .LeftJoin(
                    orders.AsQueryable(),
                    od => od.OrderId,
                    o => o.Id,
                    (od, o) => new { Outer = od, Inner = o })
                .LeftJoin(
                    customers.AsQueryable(),
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
            var orderDetails = CreateOrderDetailsCollection();
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            var queryable = orderDetails.AsQueryable()
                .LeftJoin(
                    orders.AsQueryable(),
                    od => od.OrderId,
                    o => o.Id,
                    (od, o) => new { Outer = od, Inner = o })
                .LeftJoin(
                    customers.AsQueryable(),
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
            var orders = CreateOrdersCollection();
            var customers = CreateCustomersCollection();

            // Order 3 has CustomerId=99 which matches no customer
            var queryable = orders.AsQueryable()
                .LeftJoin(
                    customers.AsQueryable(),
                    o => o.CustomerId,
                    c => c.Id,
                    (o, c) => new { OrderId = o.Id, CustomerName = c.Name });

            var results = queryable.ToList();
            results.Should().HaveCount(3);
            results.Select(r => r.OrderId).Should().BeEquivalentTo([1, 2, 3]);
            var noMatch = results.Single(r => r.OrderId == 3);
            noMatch.CustomerName.Should().BeNull();
        }

        [Fact]
        public void LeftJoin_should_throw_when_outer_type_has_field_named_outer()
        {
            var orders = GetCollection<OrderWithOuterField>("orders");
            var customers = CreateCustomersCollection();

            var queryable = orders.AsQueryable()
                .LeftJoin(customers.AsQueryable(), o => o.CustomerId, c => c.Id,
                    (o, c) => new { o, c });

            var exception = Record.Exception(() => Translate(orders, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>()
                .Which.Message.Should().Contain("'_outer'");
        }

        [Fact]
        public void LeftJoin_should_throw_when_outer_type_has_field_named_inner()
        {
            var orders = GetCollection<OrderWithInnerField>("orders");
            var customers = CreateCustomersCollection();

            var queryable = orders.AsQueryable()
                .LeftJoin(customers.AsQueryable(), o => o.CustomerId, c => c.Id,
                    (o, c) => new { o, c });

            var exception = Record.Exception(() => Translate(orders, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>()
                .Which.Message.Should().Contain("'_inner'");
        }

        [Fact]
        public void LeftJoinResult_should_round_trip_via_BsonSerializer()
        {
            var original = new LeftJoinResult<Order, Customer>
            {
                Outer = new Order { Id = 1, CustomerId = 10, Description = "Test" },
                Inner = new Customer { Id = 10, Name = "Alice" }
            };

            var document = original.ToBsonDocument();
            var result = BsonSerializer.Deserialize<LeftJoinResult<Order, Customer>>(document);

            result.Outer.Id.Should().Be(1);
            result.Outer.Description.Should().Be("Test");
            result.Inner.Name.Should().Be("Alice");
        }

        [Fact]
        public void LeftJoinResult_with_null_inner_should_round_trip_via_BsonSerializer()
        {
            var original = new LeftJoinResult<Order, Customer>
            {
                Outer = new Order { Id = 3, CustomerId = 99, Description = "No match" },
                Inner = null
            };

            var document = original.ToBsonDocument();
            var result = BsonSerializer.Deserialize<LeftJoinResult<Order, Customer>>(document);

            result.Outer.Id.Should().Be(3);
            result.Inner.Should().BeNull();
        }

        private IMongoCollection<Order> CreateOrdersCollection()
        {
            var collection = GetCollection<Order>("orders");
            CreateCollection(
                collection,
                new Order { Id = 1, CustomerId = 10, Description = "Order for Alice" },
                new Order { Id = 2, CustomerId = 20, Description = "Order for Bob" },
                new Order { Id = 3, CustomerId = 99, Description = "Order with no customer" });
            return collection;
        }

        private IMongoCollection<Customer> CreateCustomersCollection()
        {
            var collection = GetCollection<Customer>("customers");
            CreateCollection(
                collection,
                new Customer { Id = 10, Name = "Alice" },
                new Customer { Id = 20, Name = "Bob" });
            return collection;
        }

        private IMongoCollection<OrderDetail> CreateOrderDetailsCollection()
        {
            var collection = GetCollection<OrderDetail>("orderDetails");
            CreateCollection(
                collection,
                new OrderDetail { Id = 100, OrderId = 1, ProductId = "P1" },
                new OrderDetail { Id = 101, OrderId = 2, ProductId = "P2" });
            return collection;
        }

        private class Order
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public string Description { get; set; }
        }

        private class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class OrderDetail
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public string ProductId { get; set; }
        }

        private class OrderWithOuterField
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            [MongoDB.Bson.Serialization.Attributes.BsonElement("_outer")]
            public string Outer { get; set; }
        }

        private class OrderWithInnerField
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            [MongoDB.Bson.Serialization.Attributes.BsonElement("_inner")]
            public string Inner { get; set; }
        }
    }
}
