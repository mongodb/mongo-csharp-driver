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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class MqlSubtypeTests
{
    [Fact]
    public void SerializerFinder_should_resolve_mql_subtype()
    {
        var expression = MakeLambda<MyModel, BsonBinarySubType>(model => Mql.Subtype(model.Data));
        var serializerMap = InitializerSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType<EnumSerializer<BsonBinarySubType>>();
    }

    private class MyModel
    {
        public byte[] Data { get; set; }
    }

    // TODO: move to a shared location if the team agrees to start writing unit tests.
    private static LambdaExpression MakeLambda<T1, TResult>(Expression<Func<T1, TResult>> func)
        => func;

    private static SerializerMap InitializerSerializerMap(LambdaExpression expression)
    {
        var modelParameter = expression.Parameters.Single();
        var modelSerializer = BsonSerializer.LookupSerializer(modelParameter.Type);
        var nodeSerializers = new SerializerMap();
        nodeSerializers.AddSerializer(modelParameter, modelSerializer);
        return nodeSerializers;
    }
}

