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

using System;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class EnumRepresentationConventionTests
    {
        private enum WorkDays {
            Monday, 
            Wednesday, 
            Friday
        };

        private class TestClass
        {
            public string NonEnum { get; set; }
            public WorkDays DefaultEnum { get; set; }
            public WorkDays ChangedRepresentationEnum { get; set; }
        }

        [Test]
        [TestCase(0)]
        [TestCase(BsonType.Int32)]
        [TestCase(BsonType.Int64)]
        [TestCase(BsonType.String)]
        public void TestConvention(BsonType value)
        {
            var convention = new EnumRepresentationConvention(value);
            var classMap = new BsonClassMap<TestClass>();
            var nonEnumMemberMap = classMap.MapMember(x => x.NonEnum);
            var defaultEnumMemberMap = classMap.MapMember(x => x.DefaultEnum);
            var changedEnumMemberMap = classMap.MapMember(x => x.ChangedRepresentationEnum);
            convention.Apply(nonEnumMemberMap);
            convention.Apply(changedEnumMemberMap);
            Assert.IsNull(nonEnumMemberMap.SerializationOptions);
            Assert.IsNull(defaultEnumMemberMap.SerializationOptions);
            Assert.AreEqual(value, ((RepresentationSerializationOptions)changedEnumMemberMap.SerializationOptions).Representation);
        }

        [Test]
        public void TestConventionOverride()
        {
            var int64Convention = new EnumRepresentationConvention(BsonType.Int64);
            var strConvention = new EnumRepresentationConvention(BsonType.String);
            var classMap = new BsonClassMap<TestClass>();
            var memberMap = classMap.MapMember(x => x.ChangedRepresentationEnum);
            int64Convention.Apply(memberMap);
            strConvention.Apply(memberMap);
            Assert.AreEqual(BsonType.String, ((RepresentationSerializationOptions)memberMap.SerializationOptions).Representation);
        }

        [Test]
        public void TestConventionConstruction()
        {
            foreach (BsonType val in Enum.GetValues(typeof(BsonType)))
            {
                if ((val == 0) || 
                    (val == BsonType.String) ||
                    (val == BsonType.Int32) ||
                    (val == BsonType.Int64))
                {
                    new EnumRepresentationConvention(val);
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => { new EnumRepresentationConvention(val); });
                }
            }
        }
    }
}
