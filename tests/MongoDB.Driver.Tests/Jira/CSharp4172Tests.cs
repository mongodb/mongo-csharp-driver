﻿/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3ImplementationTests;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp4172Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Find_uses_the_expected_serializer(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection<Order>(null, null, linqProvider);

            var find = collection.Find(o => o.Items.Any(i => i.Type == ItemType.Refund));
            var result = find.ToString();

            var expectedResult = "find({ \"Items\" : { \"$elemMatch\" : { \"Type\" : \"refund\" } } })";
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_uses_the_expected_serializer(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection<Order>(null, null, linqProvider);

            var aggregate = collection
                .Aggregate()
                .Project((o) => new { o.Id, HasAnyRefund = o.Items.Any(i => i.Type == ItemType.Refund) });
            var stages = Translate(collection, aggregate);

            // LINQ2 uses the wrong serializer but won't be fixed
            var expectedStage = linqProvider switch
            {
                LinqProvider.V2 => "{ $project : { Id : '$_id', HasAnyRefund : { $anyElementTrue : { $map : { input : '$Items', as : 'i', in : { $eq : ['$$i.Type', 1] } } } }, _id : 0 } }",
                LinqProvider.V3 => "{ $project : { _id : '$_id', HasAnyRefund : { $anyElementTrue : { $map : { input : '$Items', as : 'i', in : { $eq : ['$$i.Type', 'refund'] } } } } } }",
                _ => throw new ArgumentException($"Invalid linqProvider: {linqProvider}.", nameof(linqProvider))
            };
            AssertStages(stages, expectedStage);
        }

        public class Order
        {
            public int Id { get; set; }
            public List<Item> Items { get; set; }
        }

        public class Item
        {
            [BsonSerializer(typeof(CamelCaseEnumSerializer<ItemType>))]
            public ItemType Type { get; set; }
        }

        public enum ItemType
        {
            SaleItem,
            Refund
        }

        public class CamelCaseEnumSerializer<T> : EnumSerializer<T>
            where T : struct, Enum
        {
            private static string ToCamelCase(string s)
            {
                return char.ToLowerInvariant(s[0]) + s.Substring(1);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
            {
                context.Writer.WriteString(ToCamelCase(value.ToString()));
            }

            public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                return (T)Enum.Parse(typeof(T), context.Reader.ReadString(), true);
            }
        }
    }
}
