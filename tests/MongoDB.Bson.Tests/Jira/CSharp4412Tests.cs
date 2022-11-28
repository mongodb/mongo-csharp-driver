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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp783
{
    public class CSharp4412Tests
    {
        [Fact]
        public void IEnumerable_Ids_should_deserialize_from_ObjectIds()
        {
            var json = "{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }";

            var rehydrated = BsonSerializer.Deserialize<ClassWithIEnumerableIds>(json);

            rehydrated.Ids.Should().Equal("0102030405060708090a0b0c");
        }

        public static readonly object[][] IEnumerable_should_serialize_as_ObjectIds_MemberData =
        {
             new object[] { new string[] { "0102030405060708090a0b0c" } },
             new object[] { new List<string> { "0102030405060708090a0b0c" } },
             new object[] { new Collection<string> { "0102030405060708090a0b0c" } },
             new object[] { new ReadOnlyCollection<string>(new List<string> { "0102030405060708090a0b0c" }) },
             new object[] { new HashSet<string> { "0102030405060708090a0b0c" } },
             new object[] { new SortedSet<string> { "0102030405060708090a0b0c" } }
        };

        [Theory]
        [MemberData(nameof(IEnumerable_should_serialize_as_ObjectIds_MemberData))]
        public void IEnumerable_should_serialize_as_ObjectIds(IEnumerable<string> value)
        {
            var document = new ClassWithIEnumerableIds { Ids = value };

            var json = document.ToJson();

            json.Should().Be("{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }");
        }

        [Fact]
        public void ICollection_Ids_should_deserialize_from_ObjectIds()
        {
            var json = "{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }";

            var rehydrated = BsonSerializer.Deserialize<ClassWithICollectionIds>(json);

            rehydrated.Ids.Should().Equal("0102030405060708090a0b0c");
        }

        public static readonly object[][] ICollection_should_serialize_as_ObjectIds_MemberData =
        {
             new object[] { new string[] { "0102030405060708090a0b0c" } },
             new object[] { new List<string> { "0102030405060708090a0b0c" } },
             new object[] { new Collection<string> { "0102030405060708090a0b0c" } },
             new object[] { new ReadOnlyCollection<string>(new List<string> { "0102030405060708090a0b0c" }) },
             new object[] { new HashSet<string> { "0102030405060708090a0b0c" } },
             new object[] { new SortedSet<string> { "0102030405060708090a0b0c" } }
        };

        [Theory]
        [MemberData(nameof(ICollection_should_serialize_as_ObjectIds_MemberData))]
        public void ICollection_should_serialize_as_ObjectIds(ICollection<string> value)
        {
            var document = new ClassWithICollectionIds { Ids = value };

            var json = document.ToJson();

            json.Should().Be("{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }");
        }

        [Fact]
        public void IList_Ids_should_deserialize_from_ObjectIds()
        {
            var json = "{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }";

            var rehydrated = BsonSerializer.Deserialize<ClassWithIListIds>(json);

            rehydrated.Ids.Should().Equal("0102030405060708090a0b0c");
        }

        public static readonly object[][] IList_should_serialize_as_ObjectIds_MemberData =
        {
             new object[] { new string[] { "0102030405060708090a0b0c" } },
             new object[] { new List<string> { "0102030405060708090a0b0c" } },
             new object[] { new Collection<string> { "0102030405060708090a0b0c" } },
             new object[] { new ReadOnlyCollection<string>(new List<string> { "0102030405060708090a0b0c" }) }
        };

        [Theory]
        [MemberData(nameof(IList_should_serialize_as_ObjectIds_MemberData))]
        public void IList_should_serialize_as_ObjectIds(IList<string> value)
        {
            var document = new ClassWithIListIds { Ids = value };

            var json = document.ToJson();

            json.Should().Be("{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }");
        }

        [Fact]
        public void ISet_Ids_should_deserialize_from_ObjectIds()
        {
            var json = "{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }";

            var rehydrated = BsonSerializer.Deserialize<ClassWithISetIds>(json);

            rehydrated.Ids.Should().Equal("0102030405060708090a0b0c");
        }

        public static readonly object[][] ISet_should_serialize_as_ObjectIds_MemberData =
        {
             new object[] { new HashSet<string> { "0102030405060708090a0b0c" } },
             new object[] { new SortedSet<string> { "0102030405060708090a0b0c" } }
        };

        [Theory]
        [MemberData(nameof(ISet_should_serialize_as_ObjectIds_MemberData))]
        public void ISet_should_serialize_as_ObjectIds(ISet<string> value)
        {
            var document = new ClassWithISetIds { Ids = value };

            var json = document.ToJson();

            json.Should().Be("{ \"Ids\" : [ObjectId(\"0102030405060708090a0b0c\")] }");
        }

        private class ClassWithIEnumerableIds
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public IEnumerable<string> Ids { get; set; }
        }

        private class ClassWithICollectionIds
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public ICollection<string> Ids { get; set; }
        }

        private class ClassWithIListIds
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public IList<string> Ids { get; set; }
        }

        private class ClassWithISetIds
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public ISet<string> Ids { get; set; }
        }
    }
}
