/* Copyright 2010-2012 10gen Inc.
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
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.BsonUnitTests.DefaultSerializer
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
        public void TestMappingUsesBsonSerializationOptionsConvention()
        {
            var profile = new ConventionProfile()
                .SetSerializationOptionsConvention(new TypeRepresentationSerializationOptionsConvention(typeof(ObjectId), BsonType.JavaScriptWithScope));

            BsonClassMap.RegisterConventions(profile, t => t == typeof(A));

            var classMap = BsonClassMap.LookupClassMap(typeof(A));

            var options = classMap.GetMemberMap("Match").SerializationOptions;
            Assert.IsInstanceOf<RepresentationSerializationOptions>(options);
            Assert.AreEqual(BsonType.JavaScriptWithScope, ((RepresentationSerializationOptions)options).Representation);
        }

        [Test]
        public void TestMappingUsesBsonSerializationOptionsConventionDoesNotMatchWrongProperty()
        {
            var profile = new ConventionProfile()
                .SetSerializationOptionsConvention(new TypeRepresentationSerializationOptionsConvention(typeof(ObjectId), BsonType.JavaScriptWithScope));

            BsonClassMap.RegisterConventions(profile, t => t == typeof(A));

            var classMap = BsonClassMap.LookupClassMap(typeof(A));

            var options = classMap.GetMemberMap("NoMatch").SerializationOptions;
            Assert.IsNull(options);
        }

        [Test]
        public void TestMappingWithAMatchingSerializationOptionsConventionDoesNotOverrideAttribute()
        {
            var profile = new ConventionProfile()
                .SetSerializationOptionsConvention(new TypeRepresentationSerializationOptionsConvention(typeof(ObjectId), BsonType.JavaScriptWithScope));

            BsonClassMap.RegisterConventions(profile, t => t == typeof(B));

            var classMap = BsonClassMap.LookupClassMap(typeof(B));

            var options = classMap.GetMemberMap("Match").SerializationOptions;
            Assert.IsInstanceOf<RepresentationSerializationOptions>(options);
            Assert.AreEqual(BsonType.ObjectId, ((RepresentationSerializationOptions)options).Representation);
        }
    }
}
