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
using MongoDB.Driver.Tests;
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
            var options = new GridFSBucketOptions();

            var result = new GridFSBucket(database, options);

            result.Database.Should().BeSameAs(database);
            result.Options.BucketName.Should().Be(options.BucketName);
            result.Options.ChunkSizeBytes.Should().Be(options.ChunkSizeBytes);
            result.Options.ReadPreference.Should().Be(options.ReadPreference);
            result.Options.WriteConcern.Should().Be(options.WriteConcern);
        }

        [Test]
        public void constructor_should_throw_when_database_is_null()
        {
            var options = new GridFSBucketOptions();

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
        public void Delete_with_BsonValue_id_should_throw_when_id_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.DeleteAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Delete(null);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void DownloadAsBytes_with_BsonValue_id_should_throw_when_id_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.DownloadAsBytesAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadAsBytes(null);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void DownloadAsBytesByName_should_throw_when_filename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            Action action;
            if (async)
            {
                action = () => subject.DownloadAsBytesByNameAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadAsBytesByName(null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void DownloadToStream_with_BsonValue_id_should_throw_when_id_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var destination = Substitute.For<Stream>();

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.DownloadToStreamAsync(null, destination).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadToStream(null, destination);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void DownloadToStream_with_BsonValue_id_should_throw_when_destination_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var id = (BsonValue)123;

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.DownloadToStreamAsync(id, null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadToStream(id, null);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void DownloadToStream_with_ObjectId_id_should_throw_when_destination_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var id = ObjectId.GenerateNewId();

            Action action;
            if (async)
            {
                action = () => subject.DownloadToStreamAsync(id, null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadToStream(id, null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void DownloadToStreamByName_should_throw_when_destination_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var filename = "filename";

            Action action;
            if (async)
            {
                action = () => subject.DownloadToStreamByNameAsync(filename, null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadToStreamByName(filename, null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("destination");
        }

        [Test]
        public void DownloadToStreamByName_should_throw_when_filename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var destination = Substitute.For<Stream>();

            Action action;
            if (async)
            {
                action = () => subject.DownloadToStreamByNameAsync(null, destination).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.DownloadToStreamByName(null, destination);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void Drop_should_drop_the_files_and_chunks_collections(
            [Values(false, true)] bool async)
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var subject = new GridFSBucket(database);
            subject.UploadFromBytes("test", new byte[] { 0 }); // causes the collections to be created

            if (async)
            {
                subject.DropAsync().GetAwaiter().GetResult();
            }
            else
            {
                subject.Drop();
            }

            var collections = database.ListCollections().ToList();
            var collectionNames = collections.Select(c => c["name"].AsString);
            collectionNames.Should().NotContain("fs.files");
            collectionNames.Should().NotContain("fs.chunks");
        }

        [Test]
        public void Find_should_throw_when_filter_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            Action action;
            if (async)
            {
                action = () => subject.FindAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Find(null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filter");
        }

        [Test]
        public void OpenDownloadStream_with_BsonValue_id_should_throw_when_id_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.OpenDownloadStreamAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.OpenDownloadStream(null);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void OpenDownloadStreamByName_should_throw_when_filename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            Action action;
            if (async)
            {
                action = () => subject.OpenDownloadStreamByNameAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.OpenDownloadStreamByName(null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void OpenUploadStream_should_throw_when_filename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            Action action;
            if (async)
            {
                action = () => subject.OpenUploadStreamAsync(null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.OpenUploadStream(null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void Options_get_should_return_the_expected_result()
        {
            var database = Substitute.For<IMongoDatabase>();
            var options = new GridFSBucketOptions();
            var subject = new GridFSBucket(database, options);

            var result = subject.Options;

            result.BucketName.Should().Be(options.BucketName);
            result.ChunkSizeBytes.Should().Be(options.ChunkSizeBytes);
            result.ReadConcern.Should().Be(options.ReadConcern);
            result.ReadPreference.Should().Be(options.ReadPreference);
            result.WriteConcern.Should().Be(options.WriteConcern);
        }

        [Test]
        public void Rename_with_BsonValue_id_should_throw_when_id_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var newFilename = "filename";

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.RenameAsync(null, newFilename).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.Rename(null, newFilename);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Test]
        public void Rename_with_BsonValue_id_should_throw_when_newFilename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var id = (BsonValue)123;

#pragma warning disable 618
            Action action;
            if (async)
            {
                action = () => subject.RenameAsync(id, null).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.Rename(id, null);
            }
#pragma warning restore

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newFilename");
        }

        [Test]
        public void Rename_with_ObjectId_id_should_throw_when_newFilename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var id = ObjectId.GenerateNewId();

            Action action;
            if (async)
            {
                action = () => subject.RenameAsync(id, null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Rename(id, null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newFilename");
        }

        [Test]
        public void UploadFromBytes_should_throw_when_filename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var source = new byte[0];

            Action action;
            if (async)
            {
                action = () => subject.UploadFromBytesAsync(null, source).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.UploadFromBytes(null, source);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void UploadFromBytes_should_throw_when_source_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var filename = "filename";

            Action action;
            if (async)
            {
                action = () => subject.UploadFromBytesAsync(filename, null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.UploadFromBytes(filename, null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        [Test]
        public void UploadFromStream_should_throw_when_filename_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var source = Substitute.For<Stream>();

            Action action;
            if (async)
            {
                action = () => subject.UploadFromStreamAsync(null, source).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.UploadFromStream(null, source);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Test]
        public void UploadFromStream_should_throw_when_source_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var filename = "filename";

            Action action;
            if (async)
            {
                action = () => subject.UploadFromStreamAsync(filename, null).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.UploadFromStream(filename, null);
            }

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        // private methods
        private GridFSBucket CreateSubject(GridFSBucketOptions options = null)
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
