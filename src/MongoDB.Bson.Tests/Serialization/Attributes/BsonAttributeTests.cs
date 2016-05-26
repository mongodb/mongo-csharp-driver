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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Attributes
{
    public class BsonAttributeTests
    {
        [BsonDiscriminator("discriminator", Required = true)]
        [BsonIgnoreExtraElements(false)]
        public class Test
        {
            [BsonDefaultValue("default1")]
            public string SerializedDefaultValue1 { get; set; }
#pragma warning disable 618 // SerializeDefaultValue is obsolete
            [BsonDefaultValue("default2", SerializeDefaultValue = true)]
            public string SerializedDefaultValue2 { get; set; }
            [BsonDefaultValue("default3", SerializeDefaultValue = false)]
            public string NotSerializedDefaultValue { get; set; }
#pragma warning restore 618
            public string NoDefaultValue { get; set; }

            [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
            public ObjectId IsId { get; set; }
            public ObjectId IsNotId { get; set; }

            [BsonIgnore]
            public string Ignored { get; set; }
            public string NotIgnored { get; set; }

            [BsonIgnoreIfDefault]
            public string IgnoredIfDefault { get; set; }
            public string NotIgnoredIfDefault { get; set; }

            [BsonIgnoreIfNull]
            public string IgnoredIfNull { get; set; }
            public string NotIgnoredIfNull { get; set; }

            [BsonRequired]
            public string Required { get; set; }
            public string NotRequired { get; set; }

            [BsonElement("notordered")]
            public string NotOrdered { get; set; }
            [BsonElement("ordered", Order = 1)]
            public string Ordered { get; set; }
            public string NoElement { get; set; }
        }

        [Fact]
        public void TestDiscriminator()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));
            Assert.Equal("discriminator", classMap.Discriminator);
            Assert.Equal(true, classMap.DiscriminatorIsRequired);
        }

        [Fact]
        public void TestIgnoreExtraElements()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));
            Assert.Equal(false, classMap.IgnoreExtraElements);
        }

        [Fact]
        public void TestDefaultValue()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var serializedDefaultValue1 = classMap.GetMemberMap("SerializedDefaultValue1");
            Assert.Equal(false, serializedDefaultValue1.IgnoreIfDefault);
            Assert.Equal("default1", serializedDefaultValue1.DefaultValue);

            var serializedDefaultValue2 = classMap.GetMemberMap("SerializedDefaultValue2");
            Assert.Equal(false, serializedDefaultValue2.IgnoreIfDefault);
            Assert.Equal("default2", serializedDefaultValue2.DefaultValue);

            var notSerializedDefaultValue = classMap.GetMemberMap("NotSerializedDefaultValue");
            Assert.Equal(true, notSerializedDefaultValue.IgnoreIfDefault);
            Assert.Equal("default3", notSerializedDefaultValue.DefaultValue);

            var noDefaultValue = classMap.GetMemberMap("NoDefaultValue");
            Assert.Equal(false, noDefaultValue.IgnoreIfDefault);
            Assert.Null(noDefaultValue.DefaultValue);
        }

        [Fact]
        public void TestId()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var isId = classMap.GetMemberMap("IsId");
            Assert.Equal("_id", isId.ElementName);
            Assert.Same(classMap.IdMemberMap, isId);

            var isNotId = classMap.GetMemberMap("IsNotId");
            Assert.Equal("IsNotId", isNotId.ElementName);
            Assert.NotSame(classMap.IdMemberMap, isNotId);
        }

        [Fact]
        public void TestIgnored()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var ignored = classMap.GetMemberMap("Ignored");
            Assert.Null(ignored);

            var notIgnored = classMap.GetMemberMap("NotIgnored");
            Assert.NotNull(notIgnored);
        }

        [Fact]
        public void TestIgnoredIfNull()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var ignoredIfNull = classMap.GetMemberMap("IgnoredIfNull");
            Assert.Equal(true, ignoredIfNull.IgnoreIfNull);

            var notIgnoredIfNull = classMap.GetMemberMap("NotIgnoredIfNull");
            Assert.Equal(false, notIgnoredIfNull.IgnoreIfNull);
        }

        [Fact]
        public void TestIgnoredIfDefault()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var ignoredIfDefault = classMap.GetMemberMap("IgnoredIfDefault");
            Assert.Equal(true, ignoredIfDefault.IgnoreIfDefault);

            var notIgnoredIfDefault = classMap.GetMemberMap("NotIgnoredIfDefault");
            Assert.Equal(false, notIgnoredIfDefault.IgnoreIfDefault);
        }

        [Fact]
        public void TestRequired()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var required = classMap.GetMemberMap("Required");
            Assert.Equal(true, required.IsRequired);

            var notRequired = classMap.GetMemberMap("NotRequired");
            Assert.Equal(false, notRequired.IsRequired);
        }

        [Fact]
        public void TestElement()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var notOrdered = classMap.GetMemberMap("NotOrdered");
            Assert.Equal("notordered", notOrdered.ElementName);
            Assert.Equal(int.MaxValue, notOrdered.Order);

            var ordered = classMap.GetMemberMap("Ordered");
            Assert.Equal("ordered", ordered.ElementName);
            Assert.Equal(1, ordered.Order);
            Assert.Same(classMap.AllMemberMaps.First(), ordered);

            var noElement = classMap.GetMemberMap("NoElement");
            Assert.Equal("NoElement", noElement.ElementName);
            Assert.Equal(int.MaxValue, noElement.Order);
        }
    }
}
