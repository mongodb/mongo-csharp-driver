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


using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class StructCustomSerializerTest
    {
        public StructCustomSerializerTest()
        {
            try
            {
                BsonSerializer.RegisterSerializer(typeof(HouseNumber), new HouseNumberSerializer());
            }
            catch { }
        }
        
        [Fact]
        public void Deserialize_StructFromNull_DefaultValue()
        {
            var stuff = BsonSerializer.Deserialize<HouseNumber>("null");
            Assert.Equal(default, stuff);
        }

        [Fact]
        public void Deserialize_StructAsPropertyFromModel_DefaultValue()
        {
            var address = BsonSerializer.Deserialize<Address>(@"{ ""Value"": null }");
            Assert.Equal(default, address.Number);
        }

        internal readonly struct HouseNumber
        {
            private readonly int val;
            public HouseNumber(int val) => this.val = val;
            public override string ToString() => val < 1 ? "" : val.ToString();
            public object ToJson() => val < 1 ? null : (object)val;
        }

        internal class Address
        {
            public HouseNumber Number { get; set; }
        }

        internal class HouseNumberSerializer : SerializerBase<HouseNumber>
        {
            public override HouseNumber Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var bsonType = context.Reader.GetCurrentBsonType();

                return bsonType == BsonType.Int32
                    ? new HouseNumber(context.Reader.ReadInt32())
                    : default;
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, HouseNumber value)
            {
                var val = value.ToJson();
                if(val is int num)
                {
                    context.Writer.WriteInt32(num);
                }
                else
                {
                    context.Writer.WriteNull();
                }
            }
        }
    }
}
