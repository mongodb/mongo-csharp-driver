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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class IndexKeysBuilderTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoServerInstance _primary;

        public IndexKeysBuilderTests()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _primary = _server.Primary;
        }

        [SkippableFact]
        public void CreateIndex_with_wildcard_index_should_create_expected_index()
        {
            RequireServer.Check().Supports(Feature.WildcardIndexes);
            var collection = _database.GetCollection<BsonDocument>("test_wildcard_index");
            collection.Drop();

            collection.CreateIndex(
                IndexKeys.Wildcard("a"),
                IndexOptions
                    .SetName("custom"));
            var indexes = collection.GetIndexes();
            var index = indexes.RawDocuments.Single(i => i["name"].AsString == "custom");

            index["key"]["a.$**"].AsInt32.Should().Be(1);
        }

        [SkippableFact]
        public void CreateIndex_with_wildcardProjection_should_create_expected_index()
        {
            RequireServer.Check().Supports(Feature.WildcardIndexes);
            var collection = _database.GetCollection<BsonDocument>("test_wildcard_index");
            collection.Drop();

            collection.CreateIndex(
                IndexKeys.Wildcard(), // wildcardProjection can be only with '$**'
                IndexOptions
                    .SetWildcardProjection("_id", true)
                    .SetName("custom"));
            var indexes = collection.GetIndexes();
            var index = indexes.RawDocuments.Single(i => i["name"].AsString == "custom");

            index["key"]["$**"].AsInt32.Should().Be(1);
            if (CoreTestConfiguration.ServerVersion >= new SemanticVersion(4, 5, 0, ""))
            {
                index["wildcardProjection"].Should().Be(BsonDocument.Parse("{ _id : true }"));
            }
            else
            {
                index["wildcardProjection"].Should().Be(BsonDocument.Parse("{ _id : 1 }"));
            }
        }

        [Fact]
        public void TestAscending1()
        {
            var keys = IndexKeys.Ascending("a");
            string expected = "{ \"a\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscending2()
        {
            var keys = IndexKeys.Ascending("a", "b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingAscending()
        {
            var keys = IndexKeys.Ascending("a").Ascending("b");
            string expected = "{ \"a\" : 1, \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingDescending()
        {
            var keys = IndexKeys.Ascending("a").Descending("b");
            string expected = "{ \"a\" : 1, \"b\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescending1()
        {
            var keys = IndexKeys.Descending("a");
            string expected = "{ \"a\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescending2()
        {
            var keys = IndexKeys.Descending("a", "b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescendingAscending()
        {
            var keys = IndexKeys.Descending("a").Ascending("b");
            string expected = "{ \"a\" : -1, \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestDescendingDescending()
        {
            var keys = IndexKeys.Descending("a").Descending("b");
            string expected = "{ \"a\" : -1, \"b\" : -1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatial()
        {
            var keys = IndexKeys.GeoSpatial("a");
            string expected = "{ \"a\" : \"2d\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatialAscending()
        {
            var keys = IndexKeys.GeoSpatial("a").Ascending("b");
            string expected = "{ \"a\" : \"2d\", \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingGeoSpatial()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatial("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2d\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatialSpherical()
        {
            var keys = IndexKeys.GeoSpatialSpherical("a");
            string expected = "{ \"a\" : \"2dsphere\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestGeoSpatialSphericalAscending()
        {
            var keys = IndexKeys.GeoSpatialSpherical("a").Ascending("b");
            string expected = "{ \"a\" : \"2dsphere\", \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingGeoSpatialSpherical()
        {
            var keys = IndexKeys.Ascending("a").GeoSpatialSpherical("b");
            string expected = "{ \"a\" : 1, \"b\" : \"2dsphere\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestHashed()
        {
            var keys = IndexKeys.Hashed("a");
            string expected = "{ \"a\" : \"hashed\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestHashedAscending()
        {
            var keys = IndexKeys.Hashed("a").Ascending("b");
            string expected = "{ \"a\" : \"hashed\", \"b\" : 1 }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestAscendingHashed()
        {
            var keys = IndexKeys.Ascending("a").Hashed("b");
            string expected = "{ \"a\" : 1, \"b\" : \"hashed\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestText()
        {
            var key = IndexKeys.Text("a");
            string expected = "{ \"a\" : \"text\" }";
            Assert.Equal(expected, key.ToJson());
        }

        [Fact]
        public void TestTextMultiple()
        {
            var keys = IndexKeys.Text("a", "b");
            string expected = "{ \"a\" : \"text\", \"b\" : \"text\" }";
            Assert.Equal(expected, keys.ToJson());
        }

        [Fact]
        public void TestTextAll()
        {
            var key = IndexKeys.TextAll();
            string expected = "{ \"$**\" : \"text\" }";
            Assert.Equal(expected, key.ToJson());
        }

        [Fact]
        public void TestTextCombination()
        {
            var key = IndexKeys.Text("a").Ascending("b");
            string expected = "{ \"a\" : \"text\", \"b\" : 1 }";
            Assert.Equal(expected, key.ToJson());
        }

        [SkippableFact]
        public void TestTextIndexCreation()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0").ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var collection = _database.GetCollection<BsonDocument>("test_text");
            collection.Drop();
            collection.CreateIndex(IndexKeys.Text("a", "b").Ascending("c"), IndexOptions.SetTextLanguageOverride("idioma").SetName("custom").SetTextDefaultLanguage("spanish"));
            var indexes = collection.GetIndexes();
            var index = indexes.RawDocuments.Single(i => i["name"].AsString == "custom");
            Assert.Equal("idioma", index["language_override"].AsString);
            Assert.Equal("spanish", index["default_language"].AsString);
            Assert.Equal(1, index["key"]["c"].AsInt32);
        }

        [Fact]
        public void Wildcard_should_return_expected_result()
        {
            var keys = IndexKeys.Wildcard();
            BsonDocument expected = BsonDocument.Parse("{ \"$**\" : 1 }");
            keys.ToBsonDocument().Should().Be(expected);

            keys = IndexKeys.Wildcard("b");
            expected = BsonDocument.Parse("{ \"b.$**\" : 1 }");
            keys.ToBsonDocument().Should().Be(expected);
        }
    }
}
