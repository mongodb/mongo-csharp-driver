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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4596Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Test_widening_expressions()
        {
            Assert((byte v) => (short)v);
            Assert((byte v) => (ushort)v);
            Assert((byte v) => (int)v);
            Assert((byte v) => (uint)v);
            Assert((byte v) => (long)v);
            Assert((byte v) => (ulong)v);
            Assert((byte v) => (float)v);
            Assert((byte v) => (double)v);
            Assert((byte v) => (decimal)v);
            Assert((sbyte v) => (short)v);
            Assert((sbyte v) => (ushort)v);
            Assert((sbyte v) => (int)v);
            Assert((sbyte v) => (uint)v);
            Assert((sbyte v) => (long)v);
            Assert((sbyte v) => (ulong)v);
            Assert((sbyte v) => (float)v);
            Assert((sbyte v) => (double)v);
            Assert((sbyte v) => (decimal)v);
            Assert((short v) => (ushort)v);
            Assert((short v) => (int)v);
            Assert((short v) => (uint)v);
            Assert((short v) => (long)v);
            Assert((short v) => (ulong)v);
            Assert((short v) => (float)v);
            Assert((short v) => (double)v);
            Assert((short v) => (decimal)v);
            Assert((ushort v) => (short)v);
            Assert((ushort v) => (int)v);
            Assert((ushort v) => (uint)v);
            Assert((ushort v) => (long)v);
            Assert((ushort v) => (ulong)v);
            Assert((ushort v) => (float)v);
            Assert((ushort v) => (double)v);
            Assert((ushort v) => (decimal)v);
            Assert((int v) => (uint)v);
            Assert((int v) => (long)v);
            Assert((int v) => (ulong)v);
            Assert((int v) => (float)v);
            Assert((int v) => (double)v);
            Assert((int v) => (decimal)v);
            Assert((uint v) => (int)v);
            Assert((uint v) => (long)v);
            Assert((uint v) => (ulong)v);
            Assert((uint v) => (float)v);
            Assert((uint v) => (double)v);
            Assert((uint v) => (decimal)v);
            Assert((long v) => (ulong)v);
            Assert((long v) => (float)v);
            Assert((long v) => (double)v);
            Assert((long v) => (decimal)v);
            Assert((ulong v) => (long)v);
            Assert((ulong v) => (float)v);
            Assert((ulong v) => (double)v);
            Assert((ulong v) => (decimal)v);
            Assert((float v) => (double)v);
            Assert((float v) => (decimal)v);
            Assert((double v) => (decimal)v);

            static void Assert<TNarrower, TWider>(Expression<Func<TNarrower, TWider>> expression)
            {
                var body = (UnaryExpression)expression.Body;
                var tnarrower = body.Operand.Type;
                var twider = body.Type;
                tnarrower.Should().Be(typeof(TNarrower));
                twider.Should().Be(typeof(TWider));
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Subtract_DateTimes_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);
            var startTime = new DateTime(2023, 04, 04, 0, 0, 0, DateTimeKind.Utc);

            var queryable = collection.AsQueryable()
                .Select(record => record.DateTimeUtc.Subtract(startTime, DateTimeUnit.Millisecond) / (double)5);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(
                    stages,
                    "{ $project : { _v : { $divide : [{ $toDouble : { $dateDiff : { startDate : ISODate('2023-04-04T00:00:00Z'), endDate : '$DateTimeUtc', unit : 'millisecond' } } }, 5.0] }, _id : 0 } }");

                var results = queryable.ToList();
                results.Should().Equal(200.0);
            }
        }

        private IMongoCollection<C> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, DateTimeUtc = new DateTime(2023, 04, 04, 0, 0, 1, DateTimeKind.Utc) });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public DateTime DateTimeUtc { get; set; }
        }
    }
}
