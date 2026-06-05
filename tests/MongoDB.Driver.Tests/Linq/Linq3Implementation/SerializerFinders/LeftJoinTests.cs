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
#if NET10_0_OR_GREATER
using System.Collections.Generic;
#endif
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class LeftJoinTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_left_join_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        // MongoQueryable.LeftJoin: result selector touches the inner parameter (proves inner serializer resolved)
        [TestHelpers.MakeLambda((Model model) => model.Outers.LeftJoin(model.Inners, o => o.Key, i => i.Key, (o, i) => i.InnerName)), typeof(IQueryableSerializer<string>)],

        // MongoQueryable.LeftJoin: result selector touches the outer parameter (proves outer serializer resolved)
        [TestHelpers.MakeLambda((Model model) => model.Outers.LeftJoin(model.Inners, o => o.Key, i => i.Key, (o, i) => o.OuterName)), typeof(IQueryableSerializer<string>)],

        // MongoQueryable.LeftJoin: result selector projects into LeftJoinResult<TOuter, TInner>
        [TestHelpers.MakeLambda((Model model) => model.Outers.LeftJoin(model.Inners, o => o.Key, i => i.Key, (o, i) => new LeftJoinResult<Outer, Inner> { Outer = o, Inner = i })), typeof(IQueryableSerializer<LeftJoinResult<Outer, Inner>>)],

#if NET10_0_OR_GREATER
        // .NET 10 BCL Queryable.LeftJoin (IEnumerable inner forces binding away from the IQueryable-inner MongoQueryable overload)
        [TestHelpers.MakeLambda((Model model) => model.Outers.LeftJoin(model.InnerList, o => o.Key, i => i.Key, (o, i) => i.InnerName)), typeof(IQueryableSerializer<string>)],
#endif
    ];

    private class Model
    {
        public IQueryable<Outer> Outers { get; set; }
        public IQueryable<Inner> Inners { get; set; }
#if NET10_0_OR_GREATER
        public IEnumerable<Inner> InnerList { get; set; }
#endif
    }

    private class Outer
    {
        public int Key { get; set; }
        public string OuterName { get; set; }
    }

    private class Inner
    {
        public int Key { get; set; }
        public string InnerName { get; set; }
    }
}
