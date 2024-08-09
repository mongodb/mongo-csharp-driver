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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4757Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Deserialize_should_ungroup_members()
        {
            var serializedOrderItem = "{ _id : { OrderId : 1, ProductId : 2 }, Quantity : 3 }";

            var orderItem = BsonSerializer.Deserialize<OrderItem>(serializedOrderItem);

            orderItem.OrderId.Should().Be(1);
            orderItem.ProductId.Should().Be(2);
            orderItem.Quantity.Should().Be(3);
        }

        [Fact]
        public void Serialize_should_group_members()
        {
            var orderItem = new OrderItem { OrderId = 1, ProductId = 2, Quantity = 3 };

            var serializedOrderItem = orderItem.ToBsonDocument();

            serializedOrderItem.Should().Be("{ _id : { OrderId : 1, ProductId : 2 }, Quantity : 3 }");
        }

        [Fact]
        public void Select_with_grouped_member_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.OrderId);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$_id.OrderId', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 4);
        }

        [Fact]
        public void Select_with_ungrouped_member_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.Quantity);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Quantity', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(3, 6);
        }

        [Fact]
        public void Where_with_grouped_member_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.OrderId == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match: { '_id.OrderId' : 1 } }");

            var result = queryable.Single();
            result.OrderId.Should().Be(1);
        }

        [Fact]
        public void Where_with_ungrouped_member_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Quantity == 3);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match: { Quantity : 3 } }");

            var result = queryable.Single();
            result.OrderId.Should().Be(1);
        }

        private IMongoCollection<OrderItem> GetCollection()
        {
            var collection = GetCollection<OrderItem>("test");
            CreateCollection(
                collection,
                new OrderItem { OrderId = 1, ProductId = 2, Quantity = 3 },
                new OrderItem { OrderId = 4, ProductId = 5, Quantity = 6 });
            return collection;
        }

        [BsonSerializer(typeof(OrderItemsSerializer))]
        private class OrderItem
        {
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        private class OrderItemsSerializer : ClassSerializerBase<OrderItem>, IBsonDocumentSerializer
        {
            // public methods
            public bool IsGroupedMember(string name, out string groupElementName)
            {
                switch (name)
                {
                    case "OrderId":
                        groupElementName = "_id";
                        return true;

                    case "ProductId":
                        groupElementName = "_id";
                        return true;

                    default:
                        groupElementName = null;
                        return false;
                }
            }

            public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
            {
                switch (memberName)
                {
                    case "OrderId":
                        serializationInfo = BsonSerializationInfo.CreateWithPath(new[] { "_id", "OrderId" }, Int32Serializer.Instance, typeof(int));
                        return true;

                    case "ProductId":
                        serializationInfo = BsonSerializationInfo.CreateWithPath(new[] { "_id", "ProductId" }, Int32Serializer.Instance, typeof(int));
                        return true;

                    case "Quantity":
                        serializationInfo = new BsonSerializationInfo("Quantity", Int32Serializer.Instance, typeof(int));
                        return true;

                    default:
                        serializationInfo = null;
                        return false;
                }
            }

            // protected methods
            protected override OrderItem DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                reader.ReadStartDocument();
                reader.ReadName("_id");
                reader.ReadStartDocument();
                reader.ReadName("OrderId");
                var orderId = reader.ReadInt32();
                reader.ReadName("ProductId");
                var productId = reader.ReadInt32();
                reader.ReadEndDocument();
                reader.ReadName("Quantity");
                var quantity = reader.ReadInt32();
                reader.ReadEndDocument();

                return new OrderItem { OrderId = orderId, ProductId = productId, Quantity = quantity };
            }

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, OrderItem value)
            {
                var writer = context.Writer;
                writer.WriteStartDocument();
                writer.WriteName("_id");
                writer.WriteStartDocument();
                writer.WriteName("OrderId");
                writer.WriteInt32(value.OrderId);
                writer.WriteName("ProductId");
                writer.WriteInt32(value.ProductId);
                writer.WriteEndDocument();
                writer.WriteName("Quantity");
                writer.WriteInt32(value.Quantity);
                writer.WriteEndDocument();
            }
        }
    }
}
