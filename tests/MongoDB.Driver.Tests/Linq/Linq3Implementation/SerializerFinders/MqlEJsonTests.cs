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

using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class MqlEJsonTests
{
    [Fact]
    public void SerializerFinder_should_resolve_mql_deserialize_ejson()
    {
        var expression = TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.DeserializeEJson<string, BsonDocument>(model.StringField, null));
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType<BsonDocumentSerializer>();
    }

    [Fact]
    public void SerializerFinder_should_resolve_mql_serialize_ejson()
    {
        var expression = TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.SerializeEJson<int, BsonDocument>(model.IntField, null));
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType<BsonDocumentSerializer>();
    }

    [Fact]
    public void SerializerFinder_should_use_custom_serializer_from_parent_for_mql_deserialize_ejson()
    {
        var expression = TestHelpers.MakeLambda<MyModel, OutputModel>(
            model => new OutputModel { Nested = Mql.DeserializeEJson<string, NestedModel>(model.StringField, null) });
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        // Pre-assign a custom serializer to the MemberInit node (simulating parent context)
        var customNestedSerializer = new CustomNestedModelSerializer();
        var outputSerializer = CreateOutputModelSerializer(customNestedSerializer);
        serializerMap.AddSerializer(expression.Body, outputSerializer);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        var memberInit = (MemberInitExpression)expression.Body;
        var memberAssignment = (MemberAssignment)memberInit.Bindings[0];
        var deserializeExpr = memberAssignment.Expression;

        serializerMap.GetSerializer(deserializeExpr).Should().BeSameAs(customNestedSerializer);
    }

    [Fact]
    public void SerializerFinder_should_use_custom_serializer_from_parent_for_mql_serialize_ejson()
    {
        var expression = TestHelpers.MakeLambda<MyModel, OutputModel>(
            model => new OutputModel { Nested = Mql.SerializeEJson<int, NestedModel>(model.IntField, null) });
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        // Pre-assign a custom serializer to the MemberInit node (simulating parent context)
        var customNestedSerializer = new CustomNestedModelSerializer();
        var outputSerializer = CreateOutputModelSerializer(customNestedSerializer);
        serializerMap.AddSerializer(expression.Body, outputSerializer);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        var memberInit = (MemberInitExpression)expression.Body;
        var memberAssignment = (MemberAssignment)memberInit.Bindings[0];
        var serializeExpr = memberAssignment.Expression;

        serializerMap.GetSerializer(serializeExpr).Should().BeSameAs(customNestedSerializer);
    }

    private static IBsonSerializer<OutputModel> CreateOutputModelSerializer(IBsonSerializer<NestedModel> nestedMemberSerializer)
    {
        var classMap = new BsonClassMap<OutputModel>();
        classMap.AutoMap();
        classMap.GetMemberMap(nameof(OutputModel.Nested)).SetSerializer(nestedMemberSerializer);
        classMap.Freeze();
        return new BsonClassMapSerializer<OutputModel>(classMap);
    }

    private class MyModel
    {
        public string StringField { get; set; }
        public int IntField { get; set; }
    }

    private class OutputModel
    {
        public NestedModel Nested { get; set; }
    }

    private class NestedModel
    {
        public string Value { get; set; }
    }

    private class CustomNestedModelSerializer : SerializerBase<NestedModel>
    {
        public override NestedModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => null;

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, NestedModel value) { }
    }
}
