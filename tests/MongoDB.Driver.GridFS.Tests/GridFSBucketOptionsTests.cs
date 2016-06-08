/* Copyright 2015-2016 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.GridFS.Tests
{
    public class GridFSBucketOptionsTests
    {
        [Fact]
        public void BucketName_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { BucketName = "bucket" };

            var result = subject.BucketName;

            result.Should().Be("bucket");
        }

        [Fact]
        public void BucketName_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.BucketName = "bucket";

            subject.BucketName.Should().Be("bucket");
        }

        [Fact]
        public void BucketName_set_should_throw_when_name_is_empty()
        {
            var subject = new GridFSBucketOptions();

            Action action = () => subject.BucketName = "";

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void BucketName_set_should_throw_when_name_is_null()
        {
            var subject = new GridFSBucketOptions();

            Action action = () => subject.BucketName = null;

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void ChunkSizeBytes_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ChunkSizeBytes = 123 };

            var result = subject.ChunkSizeBytes;

            result.Should().Be(123);
        }

        [Fact]
        public void ChunkSizeBytes_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.ChunkSizeBytes = 123;

            subject.ChunkSizeBytes.Should().Be(123);
        }

        [Theory]
        [ParameterAttributeData]
        public void ChunkSizeBytes_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int value)
        {
            var subject = new GridFSBucketOptions();

            Action action = () => subject.ChunkSizeBytes = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void constructor_with_immutable_other_should_initialize_instance()
        {
            var mutable = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadConcern = ReadConcern.Majority, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };
            var other = new ImmutableGridFSBucketOptions(mutable);

            var result = new GridFSBucketOptions(other);

            result.BucketName.Should().Be(other.BucketName);
            result.ChunkSizeBytes.Should().Be(other.ChunkSizeBytes);
            result.ReadConcern.Should().Be(other.ReadConcern);
            result.ReadPreference.Should().Be(other.ReadPreference);
            result.WriteConcern.Should().Be(other.WriteConcern);
        }

        [Fact]
        public void constructor_with_mutable_other_should_initialize_instance()
        {
            var other = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadConcern = ReadConcern.Majority, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };

            var result = new GridFSBucketOptions(other);

            result.BucketName.Should().Be(other.BucketName);
            result.ChunkSizeBytes.Should().Be(other.ChunkSizeBytes);
            result.ReadConcern.Should().Be(other.ReadConcern);
            result.ReadPreference.Should().Be(other.ReadPreference);
            result.WriteConcern.Should().Be(other.WriteConcern);
        }

        [Fact]
        public void constructor_with_no_arguments_should_initialize_instance_with_default_values()
        {
            var result = new GridFSBucketOptions();

            result.BucketName.Should().Be("fs");
            result.ChunkSizeBytes.Should().Be(255 * 1024);
            result.ReadPreference.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void ReadPreference_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ReadPreference = ReadPreference.Secondary };

            var result = subject.ReadPreference;

            result.Should().Be(ReadPreference.Secondary);
        }

        [Fact]
        public void ReadConcern_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ReadConcern = ReadConcern.Majority };

            var result = subject.ReadConcern;

            result.Should().Be(ReadConcern.Majority);
        }

        [Fact]
        public void ReadConcern_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.ReadConcern = ReadConcern.Majority;

            subject.ReadConcern.Should().Be(ReadConcern.Majority);
        }

        [Fact]
        public void ReadPreference_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.ReadPreference = ReadPreference.Secondary;

            subject.ReadPreference.Should().Be(ReadPreference.Secondary);
        }

        [Fact]
        public void WriteConcern_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { WriteConcern = WriteConcern.WMajority };

            var result = subject.WriteConcern;

            result.Should().Be(WriteConcern.WMajority);
        }

        [Fact]
        public void WriteConcern_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.WriteConcern = WriteConcern.WMajority;

            subject.WriteConcern.Should().Be(WriteConcern.WMajority);
        }
    }

    public class ImmutableGridFSBucketOptionsTests
    {
        [Fact]
        public void BucketName_get_should_return_expected_result()
        {
            var subject = new ImmutableGridFSBucketOptions(new GridFSBucketOptions { BucketName = "bucket" });

            var result = subject.BucketName;

            result.Should().Be("bucket");
        }

        [Fact]
        public void ChunkSizeBytes_get_should_return_expected_result()
        {
            var subject = new ImmutableGridFSBucketOptions(new GridFSBucketOptions { ChunkSizeBytes = 123 });

            var result = subject.ChunkSizeBytes;

            result.Should().Be(123);
        }

        [Fact]
        public void constructor_with_arguments_should_initialize_instance()
        {
            var mutable = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadConcern = ReadConcern.Majority, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };

            var result = new ImmutableGridFSBucketOptions(mutable);

            result.BucketName.Should().Be("bucket");
            result.ChunkSizeBytes.Should().Be(123);
            result.ReadConcern.Should().Be(ReadConcern.Majority);
            result.ReadPreference.Should().Be(ReadPreference.Secondary);
            result.WriteConcern.Should().Be(WriteConcern.WMajority);
        }

        [Fact]
        public void constructor_with_no_arguments_should_initialize_instance_with_default_values()
        {
            var result = new GridFSBucketOptions();

            result.BucketName.Should().Be("fs");
            result.ChunkSizeBytes.Should().Be(255 * 1024);
            result.ReadConcern.Should().BeNull();
            result.ReadPreference.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void Defaults_get_should_return_cached_instance()
        {
            var result1 = ImmutableGridFSBucketOptions.Defaults;
            var result2 = ImmutableGridFSBucketOptions.Defaults;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void Defaults_get_should_return_expected_result()
        {
            var result = ImmutableGridFSBucketOptions.Defaults;

            result.BucketName.Should().Be("fs");
            result.ChunkSizeBytes.Should().Be(255 * 1024);
            result.ReadConcern.Should().BeNull();
            result.ReadPreference.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void ReadConcern_get_should_return_expected_result()
        {
            var subject = new ImmutableGridFSBucketOptions(new GridFSBucketOptions { ReadConcern = ReadConcern.Majority });

            var result = subject.ReadConcern;

            result.Should().Be(ReadConcern.Majority);
        }

        [Fact]
        public void ReadPreference_get_should_return_expected_result()
        {
            var subject = new ImmutableGridFSBucketOptions(new GridFSBucketOptions { ReadPreference = ReadPreference.Secondary });

            var result = subject.ReadPreference;

            result.Should().Be(ReadPreference.Secondary);
        }

        [Fact]
        public void WriteConcern_get_should_return_expected_result()
        {
            var subject = new ImmutableGridFSBucketOptions(new GridFSBucketOptions { WriteConcern = WriteConcern.WMajority });

            var result = subject.WriteConcern;

            result.Should().Be(WriteConcern.WMajority);
        }
    }
}
