/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonClassMapAutoMappingTests
    {
        private class A
        {
            public ObjectId Match { get; set; }
            public string NoMatch { get; set; }
        }

        private class B
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public ObjectId Match { get; set; }
        }

        [Test]
        public void TestMappingUsesMemberSerializationOptionsConvention()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberSerializationOptionsConvention(typeof(ObjectId), new RepresentationSerializationOptions(BsonType.JavaScriptWithScope)));
            ConventionRegistry.Register("test", pack, t => t == typeof(A));

            var classMap = new BsonClassMap<A>(cm => cm.AutoMap());

            var options = classMap.GetMemberMap("Match").SerializationOptions;
            Assert.IsInstanceOf<RepresentationSerializationOptions>(options);
            Assert.AreEqual(BsonType.JavaScriptWithScope, ((RepresentationSerializationOptions)options).Representation);
        }

        [Test]
        public void TestMappingUsesMemberSerializationOptionsConventionDoesNotMatchWrongProperty()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberSerializationOptionsConvention(typeof(ObjectId), new RepresentationSerializationOptions(BsonType.JavaScriptWithScope)));
            ConventionRegistry.Register("test", pack, t => t == typeof(A));

            var classMap = new BsonClassMap<A>(cm => cm.AutoMap());

            var options = classMap.GetMemberMap("NoMatch").SerializationOptions;
            Assert.IsNull(options);
        }

        [Test]
        public void TestMappingUsesMemberSerializationOptionsConventionDoesNotOverrideAttribute()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberSerializationOptionsConvention(typeof(ObjectId), new RepresentationSerializationOptions(BsonType.JavaScriptWithScope)));
            ConventionRegistry.Register("test", pack, t => t == typeof(B));

            var classMap = new BsonClassMap<B>(cm => cm.AutoMap());

            var options = classMap.GetMemberMap("Match").SerializationOptions;
            Assert.IsInstanceOf<RepresentationSerializationOptions>(options);
            Assert.AreEqual(BsonType.ObjectId, ((RepresentationSerializationOptions)options).Representation);
        }
    }
}
