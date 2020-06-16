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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class IndexKeysBuilderTypedTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoServerInstance _primary;

        public IndexKeysBuilderTypedTests()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _primary = _server.Primary;
        }

        private class Test
        {
            [BsonElement("a")]
            public string A { get; set; }

            [BsonElement("b")]
            public string B { get; set; }

            [BsonElement("c")]
            public int C { get; set; }

            [BsonElement("d")]
            public string[] D { get; set; }

            [BsonElement("e")]
            public List<string> E { get; set; }

            [BsonElement("_id")]
            public int Id { get; set; }
        }

        [SkippableFact]
        public void CreateIndex_with_wildcard_index_should_create_expected_index()
        {
            RequireServer.Check().Supports(Feature.WildcardIndexes);
            var collection = _database.GetCollection<Test>("test_wildcard_index");
            collection.Drop();
            collection.CreateIndex(
                IndexKeys<Test>.Wildcard(c => c.A),
                IndexOptions.SetName("custom"));
            var indexes = collection.GetIndexes();
            var index = indexes.RawDocuments.Single(i => i["name"].AsString == "custom");
            index["key"]["a.$**"].AsInt32.Should().Be(1);
        }

        [SkippableFact]
        public void CreateIndex_with_wildcardProjection_should_create_expected_index()
        {
            RequireServer.Check().Supports(Feature.WildcardIndexes);
            var collection = _database.GetCollection<Test>("test_wildcard_index");
            collection.Drop();
            collection.CreateIndex(
                IndexKeys<Test>.Wildcard(),
                IndexOptions<Test>
                    .SetName("custom")
                    .SetWildcardProjection(c => c.B, true)
                    .SetWildcardProjection(c => c.Id, false));
            var indexes = collection.GetIndexes();
            var index = indexes.RawDocuments.Single(i => i["name"].AsString == "custom");
            index["key"]["$**"].AsInt32.Should().Be(1);
            if (CoreTestConfiguration.ServerVersion >= new SemanticVersion(4, 5, 0, ""))
            {
                index["wildcardProjection"].Should().Be(BsonDocument.Parse("{ b : true, _id : false }"));
            }
            else
            {
                index["wildcardProjection"].Should().Be(BsonDocument.Parse("{ b : 1, _id : 0 }"));
            }
        }

        [Fact]
        public void TestAscending1()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A);
            string expected = "{ \"a\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscending2()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A, x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingAscending()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingDescending()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescending1()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A);
            string expected = "{ \"a\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescending2()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A, x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescendingAscending()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescendingDescending()
        {
            var keys = IndexKeys<Test>.Descending(x => x.A).Descending(x => x.B);
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatial()
        {
            var keys = IndexKeys<Test>.GeoSpatial(x => x.A);
            string expected = "{ \"a\" : \"2d\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatialAscending()
        {
            var keys = IndexKeys<Test>.GeoSpatial(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"2d\", \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingGeoSpatial()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).GeoSpatial(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatialSpherical()
        {
            var keys = IndexKeys<Test>.GeoSpatialSpherical(x => x.A);
            string expected = "{ \"a\" : \"2dsphere\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatialSphericalAscending()
        {
            var keys = IndexKeys<Test>.GeoSpatialSpherical(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"2dsphere\", \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingGeoSpatialSpherical()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).GeoSpatialSpherical(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"2dsphere\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestHashed()
        {
            var keys = IndexKeys<Test>.Hashed(x => x.A);
            string expected = "{ \"a\" : \"hashed\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestHashedAscending()
        {
            var keys = IndexKeys<Test>.Hashed(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"hashed\", \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingHashed()
        {
            var keys = IndexKeys<Test>.Ascending(x => x.A).Hashed(x => x.B);
            string expected = "{ \"a\" : 1, \"b\" : \"hashed\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestText()
        {
            var key = IndexKeys<Test>.Text(x => x.A);
            string expected = "{ \"a\" : \"text\" }";
            Assert.Equal(expected, key.ToJson());
        }

        [Fact]
        public void TestTextMultiple()
        {
            var keys = IndexKeys<Test>.Text(x => x.A, x => x.B);
            string expected = "{ \"a\" : \"text\", \"b\" : \"text\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestTextAll()
        {
            var key = IndexKeys<Test>.TextAll();
            string expected = "{ \"$**\" : \"text\" }";
            Assert.Equal(expected, key.ToJson());
        }
        [Fact]
        public void TestTextCombination()
        {
            var key = IndexKeys<Test>.Text(x => x.A).Ascending(x => x.B);
            string expected = "{ \"a\" : \"text\", \"b\" : 1 }";
            Assert.Equal(expected, key.ToJson());
        }

        [Fact]
        public void TestTextArrayField()
        {
            var keys = IndexKeys<Test>.Text(x => x.D);
            string expected = "{ \"d\" : \"text\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestTextArrayFields()
        {
            var keys = IndexKeys<Test>.Text(x => x.D).Text(x => x.E);
            string expected = "{ \"d\" : \"text\", \"e\" : \"text\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestTextArrayNonArrayFields()
        {
            var keys = IndexKeys<Test>.Text(x => x.A, x => x.B).Text(x => x.D, x => x.E);
            string expected = "{ \"a\" : \"text\", \"b\" : \"text\", \"d\" : \"text\", \"e\" : \"text\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestTextArrayNonArrayFields2()
        {
            var keys = IndexKeys<Test>.Text(x => x.A).Text(x => x.E).Text(x => x.D).Text(x => x.B);
            string expected = "{ \"a\" : \"text\", \"e\" : \"text\", \"d\" : \"text\", \"b\" : \"text\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [SkippableFact]
        public void TestTextIndexCreation()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0").ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var collection = _database.GetCollection<Test>("test_text");
            collection.Drop();
            collection.CreateIndex(IndexKeys<Test>.Text(x => x.A, x => x.B).Ascending(x => x.C), IndexOptions.SetTextLanguageOverride("idioma").SetName("custom").SetTextDefaultLanguage("spanish"));
            var indexes = collection.GetIndexes();
            var index = indexes.RawDocuments.Single(i => i["name"].AsString == "custom");
            Assert.Equal("idioma", index["language_override"].AsString);
            Assert.Equal("spanish", index["default_language"].AsString);
            Assert.Equal(1, index["key"]["c"].AsInt32);
        }

        [Fact]
        public void Wildcard_index_should_return_expected_result()
        {
            var keys = IndexKeys<Test>.Wildcard();
            BsonDocument expected = BsonDocument.Parse("{ \"$**\" : 1 }");
            keys.ToBsonDocument().Should().Be(expected);

            keys = IndexKeys<Test>.Wildcard(x => x.A);
            expected = BsonDocument.Parse("{ \"a.$**\" : 1 }");
            keys.ToBsonDocument().Should().Be(expected);
        }
    }
}
