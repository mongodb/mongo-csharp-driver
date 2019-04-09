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
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp2579
{
    public class CSharp2579Tests
    {
        public struct A { }
        public class SerializerA: StructSerializerBase<A> { }

        [Fact]
        public void TestExcplicitSerializerRegistrationOverridesDefault()
        {
            var explicitSerializer = new SerializerA();
            var defaultSerializer = BsonSerializer.SerializerRegistry.GetSerializer<A>();

            BsonSerializer.RegisterSerializer(explicitSerializer);

            Assert.Same(explicitSerializer, BsonSerializer.SerializerRegistry.GetSerializer<A>());
        }


        public struct B { }
        public class SerializerB: StructSerializerBase<B> { }

        [Fact]
        public void TestMultipleExcplicitSerializerRegistrationFails()
        {
            BsonSerializer.RegisterSerializer(new SerializerB());

            Assert.Throws<BsonSerializationException>(() =>
            {
                BsonSerializer.RegisterSerializer(new SerializerB());
            });
        }
    }
}
