﻿/* Copyright 2010-2011 10gen Inc.
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
using MongoDB.Bson.Serialization.IdGenerators;

namespace MongoDB.BsonUnitTests.Serialization.Attributes
{
    [TestFixture]
    public class BsonAttributeTests
    {
        [BsonDiscriminator("discriminator", Required = true)]
        [BsonIgnoreExtraElements(false)]
        public class Test
        {
            [BsonDefaultValue("default1")]

            public string SerializedDefaultValue { get; set; }
            [BsonDefaultValue("default2")]
            [BsonIgnoreIfDefault]

            public string NotSerializedDefaultValue { get; set; }
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

            [BsonRequired]
            public string Required { get; set; }
            public string NotRequired { get; set; }

            [BsonElement("notordered")]
            public string NotOrdered { get; set; }
            [BsonElement("ordered", Order = 1)]
            public string Ordered { get; set; }
            public string NoElement { get; set; }
        }

        [Test]
        public void TestDiscriminator()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));
            Assert.AreEqual("discriminator", classMap.Discriminator);
            Assert.AreEqual(true, classMap.DiscriminatorIsRequired);
        }

        [Test]
        public void TestIgnoreExtraElements()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));
            Assert.AreEqual(false, classMap.IgnoreExtraElements);
        }

        [Test]
        public void TestDefaultValue()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var serializedDefaultValue = classMap.GetMemberMap("SerializedDefaultValue");
            Assert.AreEqual(true, serializedDefaultValue.HasDefaultValue);
            Assert.AreEqual(false, serializedDefaultValue.IgnoreIfDefault);
            Assert.AreEqual("default1", serializedDefaultValue.DefaultValue);

            var notSerializedDefaultValue = classMap.GetMemberMap("NotSerializedDefaultValue");
            Assert.AreEqual(true, notSerializedDefaultValue.HasDefaultValue);
            Assert.AreEqual(true, notSerializedDefaultValue.IgnoreIfDefault);
            Assert.AreEqual("default2", notSerializedDefaultValue.DefaultValue);

            var noDefaultValue = classMap.GetMemberMap("NoDefaultValue");
            Assert.AreEqual(false, noDefaultValue.HasDefaultValue);
            Assert.AreEqual(false, noDefaultValue.IgnoreIfDefault);
            Assert.IsNull(noDefaultValue.DefaultValue);
        }

        [Test]
        public void TestId()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var isId = classMap.GetMemberMap("IsId");
            Assert.AreEqual("_id", isId.ElementName);
            Assert.AreSame(classMap.IdMemberMap, isId);

            var isNotId = classMap.GetMemberMap("IsNotId");
            Assert.AreEqual("IsNotId", isNotId.ElementName);
            Assert.AreNotSame(classMap.IdMemberMap, isNotId);
        }

        [Test]
        public void TestIgnored()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var ignored = classMap.GetMemberMap("Ignored");
            Assert.IsNull(ignored);

            var notIgnored = classMap.GetMemberMap("NotIgnored");
            Assert.IsNotNull(notIgnored);
        }

        [Test]
        public void TestIgnoredIfNull()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var ignoredIfDefault = classMap.GetMemberMap("IgnoredIfDefault");
            Assert.AreEqual(true, ignoredIfDefault.IgnoreIfDefault);

            var notIgnoredIfDefault = classMap.GetMemberMap("NotIgnoredIfDefault");
            Assert.AreEqual(false, notIgnoredIfDefault.IgnoreIfDefault);
        }

        [Test]
        public void TestRequired()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var required = classMap.GetMemberMap("Required");
            Assert.AreEqual(true, required.IsRequired);

            var notRequired = classMap.GetMemberMap("NotRequired");
            Assert.AreEqual(false, notRequired.IsRequired);
        }

        [Test]
        public void TestElement()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));

            var notOrdered = classMap.GetMemberMap("NotOrdered");
            Assert.AreEqual("notordered", notOrdered.ElementName);
            Assert.AreEqual(int.MaxValue, notOrdered.Order);

            var ordered = classMap.GetMemberMap("Ordered");
            Assert.AreEqual("ordered", ordered.ElementName);
            Assert.AreEqual(1, ordered.Order);
            Assert.AreSame(classMap.MemberMaps.First(), ordered);

            var noElement = classMap.GetMemberMap("NoElement");
            Assert.AreEqual("NoElement", noElement.ElementName);
            Assert.AreEqual(int.MaxValue, noElement.Order);
        }
    }
}
