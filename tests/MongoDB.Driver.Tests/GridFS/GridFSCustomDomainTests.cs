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
using System.Text;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.GridFS;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    [Trait("Category", "Integration")]
    public class GridFSCustomDomainTests
    {
        [Fact]
        public void TFileId_serializer_is_resolved_through_bucket_domain_on_upload()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new MultipleRegistriesTests.CustomStringSerializer());

            var client = CreateClientWithDomain(customDomain);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var bucket = new GridFSBucket<string>(database);

            bucket.UploadFromBytes("abc", "hello.txt", Encoding.UTF8.GetBytes("hi"));

            // Read fs.files raw to confirm the custom serializer ran on _id at upload time.
            // Reading as BsonDocument bypasses any custom string handling on the read side.
            var filesCollection = database.GetCollection<BsonDocument>("fs.files");
            var fileDoc = filesCollection.Find(FilterDefinition<BsonDocument>.Empty).Single();
            fileDoc["_id"].AsString.Should().Be("abctest");
        }

        [Fact]
        public void TFileId_serializer_is_resolved_through_bucket_domain_on_filter_render()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new MultipleRegistriesTests.CustomStringSerializer());

            var client = CreateClientWithDomain(customDomain);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var bucket = new GridFSBucket<string>(database);

            bucket.UploadFromBytes("abc", "hello.txt", Encoding.UTF8.GetBytes("hi"));

            // Filter is rendered through the bucket's domain; "abc" -> "abctest" via the custom serializer,
            // matching the stored _id. If the render used the default-domain registry, this would find nothing.
            var filter = Builders<GridFSFileInfo<string>>.Filter.Eq(f => f.Id, "abc");
            var hits = bucket.Find(filter).ToList();
            hits.Should().HaveCount(1);
            hits.Single().Filename.Should().Be("hello.txt");
        }

        private static IMongoClient CreateClientWithDomain(IBsonSerializationDomain domain)
        {
            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = domain);
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection("fs.files");
            db.DropCollection("fs.chunks");
            return client;
        }
    }
}
