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
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
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

        [Theory]
        [InlineData(0)]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void TestConvention(BsonType value)
        {
            var convention = new EnumRepresentationConvention(value);
            var classMap = new BsonClassMap<TestClass>();
            var nonEnumMemberMap = classMap.MapMember(x => x.NonEnum);
            classMap.MapMember(x => x.DefaultEnum);
            var changedEnumMemberMap = classMap.MapMember(x => x.ChangedRepresentationEnum);
            convention.Apply(nonEnumMemberMap);
            convention.Apply(changedEnumMemberMap);
            Assert.Equal(value, ((IRepresentationConfigurable)(changedEnumMemberMap.GetSerializer())).Representation);
        }

        [Fact]
        public void TestConventionOverride()
        {
            var int64Convention = new EnumRepresentationConvention(BsonType.Int64);
            var strConvention = new EnumRepresentationConvention(BsonType.String);
            var classMap = new BsonClassMap<TestClass>();
            var memberMap = classMap.MapMember(x => x.ChangedRepresentationEnum);
            int64Convention.Apply(memberMap);
            strConvention.Apply(memberMap);
            Assert.Equal(BsonType.String, ((IRepresentationConfigurable)(memberMap.GetSerializer())).Representation);
        }

        [Fact]
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
