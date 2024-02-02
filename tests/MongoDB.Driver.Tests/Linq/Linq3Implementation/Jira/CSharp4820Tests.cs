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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4820Tests : Linq3IntegrationTest
    {
        static CSharp4820Tests()
        {
            BsonClassMap.RegisterClassMap<C>(cm =>
            {
                cm.AutoMap();
                var readonlyCollectionMemberMap = cm.GetMemberMap(x => x.ReadOnlyCollection);
                var readOnlyCollectionSerializer = readonlyCollectionMemberMap.GetSerializer();
                var bracketingCollectionSerializer = ((IChildSerializerConfigurable)readOnlyCollectionSerializer).WithChildSerializer(new StringBracketingSerializer());
                readonlyCollectionMemberMap.SetSerializer(bracketingCollectionSerializer);
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void Update_Set_with_List_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var values = new List<string>() { "abc", "def" };
            var update = Builders<C>.Update.Set(x => x.ReadOnlyCollection, values);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            var rendered = (BsonDocument)update.Render(documentSerializer, serializerRegistry, linqProvider);

            rendered.Should().Be("{ $set : { ReadOnlyCollection : ['[abc]', '[def]'] } }");
        }

        [Theory]
        [ParameterAttributeData]
        public void Update_Set_with_Enumerable_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var values = new[] { "abc", "def" }.Select(x => x);
            var update = Builders<C>.Update.Set(x => x.ReadOnlyCollection, values);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            BsonDocument rendered = null;
            var exception = Record.Exception(() => rendered = (BsonDocument)update.Render(documentSerializer, serializerRegistry, linqProvider));

            if (linqProvider == LinqProvider.V2)
            {
                rendered.Should().Be("{ $set : { ReadOnlyCollection : ['[abc]', '[def]'] } }");
            }
            else
            {
                exception.Should().BeOfType<InvalidOperationException>();
                exception.Message.Should().Contain("Value could not be converted");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Update_Set_with_Enumerable_ToList_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var values = new[] { "abc", "def" }.Select(x => x);
            var update = Builders<C>.Update.Set(x => x.ReadOnlyCollection, values.ToList());
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<C>();

            var rendered = (BsonDocument)update.Render(documentSerializer, serializerRegistry, linqProvider);

            rendered.Should().Be("{ $set : { ReadOnlyCollection : ['[abc]', '[def]'] } }");
        }

        private class C
        {
            public int Id { get; set; }
            public IReadOnlyCollection<string> ReadOnlyCollection { get; set; }
        }

        private class StringBracketingSerializer : SerializerBase<string>
        {
            public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var bracketedValue = StringSerializer.Instance.Deserialize(context, args);
                return bracketedValue.Substring(1, bracketedValue.Length - 2);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
            {
                var bracketedValue = "[" + value + "]";
                StringSerializer.Instance.Serialize(context, bracketedValue);
            }
        }
    }
}
