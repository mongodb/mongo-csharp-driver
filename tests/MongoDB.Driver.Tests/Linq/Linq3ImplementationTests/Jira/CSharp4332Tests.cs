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

#if NET6_0_OR_GREATER // because tests use readonly record struct
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4332Tests : Linq3IntegrationTest
    {
        private static readonly (Expression<Func<Document, bool>> Predicate, string ExpectedFilter)[] __testCases;

        static CSharp4332Tests()
        {
            var guid = Guid.Parse("0102030405060708090a0b0c0d0e0f10");
            var invoiceId = new InvoiceId(guid);
            var guidNullable = (Guid?)guid;
            var invoiceIdNullable = (InvoiceId?)invoiceId;

            __testCases = new (Expression<Func<Document, bool>> Predicate, string ExpectedFilter)[]
            {
                (c => c.Guid == guid, "{ Guid : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.GuidNullable == guid, "{ GuidNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.Guid == invoiceId, "{ Guid : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.GuidNullable == invoiceId, "{ GuidNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceId == invoiceId, "{ InvoiceId : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceIdNullable == invoiceId, "{ InvoiceIdNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),

                (c => c.Guid == guidNullable, "{ Guid : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.GuidNullable == guidNullable, "{ GuidNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.Guid == invoiceIdNullable, "{ Guid : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.GuidNullable == invoiceIdNullable, "{ GuidNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceId == invoiceIdNullable, "{ InvoiceId : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceIdNullable == invoiceIdNullable, "{ InvoiceIdNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),

                (c => c.InvoiceId == new InvoiceId(guidNullable.Value), "{ InvoiceId : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceIdNullable == (guidNullable.HasValue ? new InvoiceId(guidNullable.Value) : null), "{ InvoiceIdNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceId == new InvoiceId(guid), "{ InvoiceId : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
                (c => c.InvoiceIdNullable == new InvoiceId(guid), "{ InvoiceIdNullable : '01020304-0506-0708-090a-0b0c0d0e0f10' }"),
            };
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        public void Where_expression_should_work(string predicateAsString, string expectedFilter, int i)
        {
            var collection = CreateCollection();
            var predicate = __testCases[i].Predicate;

            var queryable = collection.AsQueryable().Where(predicate);

            var stages = Translate(collection, queryable);
            var filter = stages.Single()["$match"];
            filter.Should().Be(expectedFilter);

            var results = queryable.ToList();
            results.Single().Id.Should().Be(1);
        }

        public static IEnumerable<object[]> GetTestCases()
        {
            for (var i = 0; i < __testCases.Length; i++)
            {
                var predicateAsString = __testCases[0].Predicate.ToString();
                var expectedFilter = __testCases[i].ExpectedFilter;
                yield return new object[] { predicateAsString, expectedFilter, i };
            }
        }

        private IMongoCollection<Document> CreateCollection()
        {
            var collection = GetCollection<Document>("C") ;

            var guid = Guid.Parse("0102030405060708090a0b0c0d0e0f10");
            var invoiceId = new InvoiceId(guid);
            var guidNullable = (Guid?)guid;
            var invoiceIdNullable = (InvoiceId?)invoiceId;

            CreateCollection(
                collection,
                new Document
                {
                    Id = 1,
                    Guid = guid,
                    GuidNullable = guidNullable,
                    InvoiceId = invoiceId,
                    InvoiceIdNullable = invoiceIdNullable
                });

            return collection;
        }

        public class Document
        {
            public int Id { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Guid Guid { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Guid? GuidNullable { get; set; }

            public InvoiceId InvoiceId { get; set; }
            public InvoiceId? InvoiceIdNullable { get; set; }
        }

        [BsonSerializer(typeof(InvoiceIdSerializer))]
        public readonly record struct InvoiceId(Guid Value)
        {
            public static implicit operator Guid(InvoiceId s) => s.Value;
        }

        public class InvoiceIdSerializer : SerializerBase<InvoiceId>
        {
            public override InvoiceId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                if (context.Reader.CurrentBsonType == BsonType.Null)
                {
                    context.Reader.ReadNull();
                    return default;
                }

                if (Guid.TryParse(context.Reader.ReadString(), out var guid))
                {
                    return new InvoiceId(guid);
                }

                return new InvoiceId(default);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, InvoiceId value)
            {
                context.Writer.WriteString(value.Value.ToString());
            }
        }
    }
}
#endif
