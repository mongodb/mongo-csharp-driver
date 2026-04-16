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
    public void SerializerFinder_should_resolve_mql_deserialize_ejson_in_member_init()
    {
        var expression = TestHelpers.MakeLambda<MyModel, OutputModel>(
            model => new OutputModel { Document = Mql.DeserializeEJson<string, BsonDocument>(model.StringField, null) });
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        var memberInit = (MemberInitExpression)expression.Body;
        var memberAssignment = (MemberAssignment)memberInit.Bindings[0];
        var deserializeExpr = memberAssignment.Expression;

        serializerMap.IsKnown(deserializeExpr, out _).Should().BeTrue();
        serializerMap.GetSerializer(deserializeExpr).Should().BeOfType<BsonDocumentSerializer>();
    }

    [Fact]
    public void SerializerFinder_should_resolve_mql_serialize_ejson_in_member_init()
    {
        var expression = TestHelpers.MakeLambda<MyModel, OutputModel>(
            model => new OutputModel { Document = Mql.SerializeEJson<int, BsonDocument>(model.IntField, null) });
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        var memberInit = (MemberInitExpression)expression.Body;
        var memberAssignment = (MemberAssignment)memberInit.Bindings[0];
        var serializeExpr = memberAssignment.Expression;

        serializerMap.IsKnown(serializeExpr, out _).Should().BeTrue();
        serializerMap.GetSerializer(serializeExpr).Should().BeOfType<BsonDocumentSerializer>();
    }

    [Fact]
    public void SerializerFinder_should_use_custom_serializer_from_parent_for_mql_deserialize_ejson()
    {
        var expression = TestHelpers.MakeLambda<MyModel, OutputModel>(
            model => new OutputModel { Document = Mql.DeserializeEJson<string, BsonDocument>(model.StringField, null) });
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        // Pre-assign a custom serializer to the MemberInit node (simulating parent context)
        var customDocSerializer = new CustomBsonDocumentSerializer();
        var outputSerializer = CreateOutputModelSerializer(customDocSerializer);
        serializerMap.AddSerializer(expression.Body, outputSerializer);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        var memberInit = (MemberInitExpression)expression.Body;
        var memberAssignment = (MemberAssignment)memberInit.Bindings[0];
        var deserializeExpr = memberAssignment.Expression;

        serializerMap.GetSerializer(deserializeExpr).Should().BeSameAs(customDocSerializer);
    }

    [Fact]
    public void SerializerFinder_should_use_custom_serializer_from_parent_for_mql_serialize_ejson()
    {
        var expression = TestHelpers.MakeLambda<MyModel, OutputModel>(
            model => new OutputModel { Document = Mql.SerializeEJson<int, BsonDocument>(model.IntField, null) });
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        // Pre-assign a custom serializer to the MemberInit node (simulating parent context)
        var customDocSerializer = new CustomBsonDocumentSerializer();
        var outputSerializer = CreateOutputModelSerializer(customDocSerializer);
        serializerMap.AddSerializer(expression.Body, outputSerializer);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        var memberInit = (MemberInitExpression)expression.Body;
        var memberAssignment = (MemberAssignment)memberInit.Bindings[0];
        var serializeExpr = memberAssignment.Expression;

        serializerMap.GetSerializer(serializeExpr).Should().BeSameAs(customDocSerializer);
    }

    private static IBsonSerializer<OutputModel> CreateOutputModelSerializer(IBsonSerializer<BsonDocument> documentMemberSerializer)
    {
        var classMap = new BsonClassMap<OutputModel>();
        classMap.AutoMap();
        classMap.GetMemberMap(nameof(OutputModel.Document)).SetSerializer(documentMemberSerializer);
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
        public BsonDocument Document { get; set; }
    }

    private class CustomBsonDocumentSerializer : SerializerBase<BsonDocument>
    {
        public override BsonDocument Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => BsonDocumentSerializer.Instance.Deserialize(context, args);

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BsonDocument value)
            => BsonDocumentSerializer.Instance.Serialize(context, args, value);
    }
}
