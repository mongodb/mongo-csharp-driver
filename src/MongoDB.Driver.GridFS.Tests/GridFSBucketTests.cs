/* Copyright 2015 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.GridFS.Tests
{
    [TestFixture]
    public class GridFSBucketTests
    {
        [Test]
        public void constructor_should_initialize_instance()
        {
            var database = Substitute.For<IMongoDatabase>();
            var options = new ImmutableGridFSBucketOptions();

            var result = new GridFSBucket(database, options);

            result.Database.Should().BeSameAs(database);
            result.Options.Should().BeSameAs(options);
        }

        [Test]
        public void constructor_should_throw_when_database_is_null()
        {
            var options = new ImmutableGridFSBucketOptions();

            Action action = () => new GridFSBucket(null, options);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("database");
        }

        [Test]
        public void constructor_should_use_default_options_when_options_is_null()
        {
            var database = Substitute.For<IMongoDatabase>();

            var result = new GridFSBucket(database, null);

            result.Options.Should().BeSameAs(ImmutableGridFSBucketOptions.Defaults);
        }

        [Test]
        public void Database_get_should_return_the_expected_result()
        {
            var database = Substitute.For<IMongoDatabase>();
            var subject = new GridFSBucket(database, null);

            var result = subject.Database;

            result.Should().BeSameAs(database);
        }

        [Test]
        public void DeleteAsync_with_BsonValue_id_should_throw_when_id_is_null()
        {
            var subject = CreateSubject();

#pragma warning disable 618
            Func<Task> action = () => subject.DeleteAsync(null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void DownloadAsBytesAsync_with_BsonValue_id_should_throw_when_id_is_null()
        {
            var subject = CreateSubject();

#pragma warning disable 618
            Func<Task> action = () => subject.DownloadAsBytesAsync(null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void DownloadAsBytesByNameAsync_should_throw_when_filename_is_null()
        {
            var subject = CreateSubject();

            Func<Task> action = () => subject.DownloadAsBytesByNameAsync(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void DownloadToStreamAsync_with_BsonValue_id_should_throw_when_id_is_null()
        {
            var subject = CreateSubject();
            var destination = Substitute.For<Stream>();

#pragma warning disable 618
            Func<Task> action = () => subject.DownloadToStreamAsync(null, destination);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void DownloadToStreamAsync_with_BsonValue_id_should_throw_when_destination_is_null()
        {
            var subject = CreateSubject();
            var id = (BsonValue)123;

#pragma warning disable 618
            Func<Task> action = () => subject.DownloadToStreamAsync(id, null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void DownloadToStreamAsync_with_ObjectId_id_should_throw_when_destination_is_null()
        {
            var subject = CreateSubject();
            var id = ObjectId.GenerateNewId();

#pragma warning disable 618
            Func<Task> action = () => subject.DownloadToStreamAsync(id, null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void DownloadToStreamByNameAsync_should_throw_when_destination_is_null()
        {
            var subject = CreateSubject();
            var filename = "filename";

            Func<Task> action = () => subject.DownloadToStreamByNameAsync(filename, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void DownloadToStreamByNameAsync_should_throw_when_filename_is_null()
        {
            var subject = CreateSubject();
            var destination = Substitute.For<Stream>();

            Func<Task> action = () => subject.DownloadToStreamByNameAsync(null, destination);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void Findsync_should_throw_when_filter_is_null()
        {
            var subject = CreateSubject();

            Func<Task> action = () => subject.FindAsync(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filter");
        }

        [Test]
        public void OpenDownloadStreamAsync_with_BsonValue_id_should_throw_when_id_is_null()
        {
            var subject = CreateSubject();

#pragma warning disable 618
            Func<Task> action = () => subject.OpenDownloadStreamAsync(null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void OpenDownloadStreamByNameAsync_should_throw_when_filename_is_null()
        {
            var subject = CreateSubject();

            Func<Task> action = () => subject.OpenDownloadStreamByNameAsync(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void OpenUploadStreamAsync_should_throw_when_filename_is_null()
        {
            var subject = CreateSubject();

            Func<Task> action = () => subject.OpenUploadStreamAsync(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void Options_get_should_return_the_expected_result()
        {
            var database = Substitute.For<IMongoDatabase>();
            var options = new ImmutableGridFSBucketOptions();
            var subject = new GridFSBucket(database, options);

            var result = subject.Options;

            result.Should().BeSameAs(options);
        }

        [Test]
        public void RenameAsync_with_BsonValue_id_should_throw_when_id_is_null()
        {
            var subject = CreateSubject();
            var newFilename = "filename";

#pragma warning disable 618
            Func<Task> action = () => subject.RenameAsync(null, newFilename);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void RenameAsync_with_BsonValue_id_should_throw_when_newFilename_is_null()
        {
            var subject = CreateSubject();
            var id = (BsonValue)123;

#pragma warning disable 618
            Func<Task> action = () => subject.RenameAsync(id, null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newFilename");
        }

        [Test]
        public void RenameAsync_with_ObjectId_id_should_throw_when_newFilename_is_null()
        {
            var subject = CreateSubject();
            var id = ObjectId.GenerateNewId();

#pragma warning disable 618
            Func<Task> action = () => subject.RenameAsync(id, null);
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newFilename");
        }

        [Test]
        public void UploadFromBytesAsync_id_should_throw_when_filename_is_null()
        {
            var subject = CreateSubject();
            var source = new byte[0];

            Func<Task> action = () => subject.UploadFromBytesAsync(null, source);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void UploadFromBytesAsync_id_should_throw_when_source_is_null()
        {
            var subject = CreateSubject();
            var filename = "filename";

            Func<Task> action = () => subject.UploadFromBytesAsync(filename, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        [Test]
        public void UploadFromStreamAsync_id_should_throw_when_filename_is_null()
        {
            var subject = CreateSubject();
            var source = Substitute.For<Stream>();

            Func<Task> action = () => subject.UploadFromStreamAsync(null, source);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void UploadFromStreamAsync_id_should_throw_when_source_is_null()
        {
            var subject = CreateSubject();
            var filename = "filename";

            Func<Task> action = () => subject.UploadFromStreamAsync(filename, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        // private methods
        private GridFSBucket CreateSubject(ImmutableGridFSBucketOptions options = null)
        {
            var cluster = Substitute.For<ICluster>();

            var client = Substitute.For<IMongoClient>();
            client.Cluster.Returns(cluster);

            var database = Substitute.For<IMongoDatabase>();
            database.Client.Returns(client);

            return new GridFSBucket(database, options);
        }
    }
}
