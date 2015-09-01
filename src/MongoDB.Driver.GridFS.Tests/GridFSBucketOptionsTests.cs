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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace MongoDB.Driver.GridFS.Tests
{
    [TestFixture]
    public class GridFSBucketOptionsTests
    {
        [Test]
        public void BucketName_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { BucketName = "bucket" };

            var result = subject.BucketName;

            result.Should().Be("bucket");
        }

        [Test]
        public void BucketName_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.BucketName = "bucket";

            subject.BucketName.Should().Be("bucket");
        }

        [Test]
        public void BucketName_set_should_throw_when_name_is_empty()
        {
            var subject = new GridFSBucketOptions();

            Action action = () => subject.BucketName = "";

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void BucketName_set_should_throw_when_name_is_null()
        {
            var subject = new GridFSBucketOptions();

            Action action = () => subject.BucketName = null;

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void ChunkSizeBytes_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ChunkSizeBytes = 123 };

            var result = subject.ChunkSizeBytes;

            result.Should().Be(123);
        }

        [Test]
        public void ChunkSizeBytes_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.ChunkSizeBytes = 123;

            subject.ChunkSizeBytes.Should().Be(123);
        }

        [Test]
        public void ChunkSizeBytes_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int value)
        {
            var subject = new GridFSBucketOptions();

            Action action = () => subject.ChunkSizeBytes = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void constructor_with_immutable_other_should_initialize_instance()
        {
            var mutable = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };
            var other = mutable.ToImmutable();

            var result = new GridFSBucketOptions(other);

            result.BucketName.Should().Be(other.BucketName);
            result.ChunkSizeBytes.Should().Be(other.ChunkSizeBytes);
            result.ReadPreference.Should().Be(other.ReadPreference);
            result.WriteConcern.Should().Be(other.WriteConcern);
        }

        [Test]
        public void constructor_with_mutable_other_should_initialize_instance()
        {
            var other = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };

            var result = new GridFSBucketOptions(other);

            result.BucketName.Should().Be(other.BucketName);
            result.ChunkSizeBytes.Should().Be(other.ChunkSizeBytes);
            result.ReadPreference.Should().Be(other.ReadPreference);
            result.WriteConcern.Should().Be(other.WriteConcern);
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance_with_default_values()
        {
            var result = new GridFSBucketOptions();

            result.BucketName.Should().Be("fs");
            result.ChunkSizeBytes.Should().Be(255 * 1024);
            result.ReadPreference.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Test]
        public void ReadPreference_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ReadPreference = ReadPreference.Secondary };

            var result = subject.ReadPreference;

            result.Should().Be(ReadPreference.Secondary);
        }

        [Test]
        public void ReadPreference_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.ReadPreference = ReadPreference.Secondary;

            subject.ReadPreference.Should().Be(ReadPreference.Secondary);
        }

        [Test]
        public void ToImmutable_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };

            var result = subject.ToImmutable();

            result.BucketName.Should().Be(subject.BucketName);
            result.ChunkSizeBytes.Should().Be(subject.ChunkSizeBytes);
            result.ReadPreference.Should().Be(subject.ReadPreference);
            result.WriteConcern.Should().Be(subject.WriteConcern);
        }

        [Test]
        public void WriteConcern_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { WriteConcern = WriteConcern.WMajority };

            var result = subject.WriteConcern;

            result.Should().Be(WriteConcern.WMajority);
        }

        [Test]
        public void WriteConcern_set_should_have_expected_result()
        {
            var subject = new GridFSBucketOptions();

            subject.WriteConcern = WriteConcern.WMajority;

            subject.WriteConcern.Should().Be(WriteConcern.WMajority);
        }
    }

    [TestFixture]
    public class ImmutableGridFSBucketOptionsTests
    {
        [Test]
        public void BucketName_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { BucketName = "bucket" }.ToImmutable();

            var result = subject.BucketName;

            result.Should().Be("bucket");
        }

        [Test]
        public void ChunkSizeBytes_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ChunkSizeBytes = 123 }.ToImmutable();

            var result = subject.ChunkSizeBytes;

            result.Should().Be(123);
        }

        [Test]
        public void constructor_with_arguments_should_initialize_instance()
        {
            var result = new ImmutableGridFSBucketOptions("bucket", 123, ReadPreference.Secondary, WriteConcern.WMajority);

            result.BucketName.Should().Be("bucket");
            result.ChunkSizeBytes.Should().Be(123);
            result.ReadPreference.Should().Be(ReadPreference.Secondary);
            result.WriteConcern.Should().Be(WriteConcern.WMajority);
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance_with_default_values()
        {
            var result = new GridFSBucketOptions();

            result.BucketName.Should().Be("fs");
            result.ChunkSizeBytes.Should().Be(255 * 1024);
            result.ReadPreference.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Test]
        public void Defaults_get_should_return_cached_instance()
        {
            var result1 = ImmutableGridFSBucketOptions.Defaults;
            var result2 = ImmutableGridFSBucketOptions.Defaults;

            result2.Should().BeSameAs(result1);
        }

        [Test]
        public void Defaults_get_should_return_expected_result()
        {
            var result = ImmutableGridFSBucketOptions.Defaults;

            result.BucketName.Should().Be("fs");
            result.ChunkSizeBytes.Should().Be(255 * 1024);
            result.ReadPreference.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Test]
        public void implicit_conversion_from_mutable_should_return_expected_result()
        {
            var mutable = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority };

            var result = (ImmutableGridFSBucketOptions)mutable;

            result.BucketName.Should().Be(mutable.BucketName);
            result.ChunkSizeBytes.Should().Be(mutable.ChunkSizeBytes);
            result.ReadPreference.Should().Be(mutable.ReadPreference);
            result.WriteConcern.Should().Be(mutable.WriteConcern);
        }

        [Test]
        public void ReadPreference_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { ReadPreference = ReadPreference.Secondary }.ToImmutable();

            var result = subject.ReadPreference;

            result.Should().Be(ReadPreference.Secondary);
        }

        [Test]
        public void ToMutable_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { BucketName = "bucket", ChunkSizeBytes = 123, ReadPreference = ReadPreference.Secondary, WriteConcern = WriteConcern.WMajority }.ToImmutable();

            var result = subject.ToMutable();

            result.BucketName.Should().Be(subject.BucketName);
            result.ChunkSizeBytes.Should().Be(subject.ChunkSizeBytes);
            result.ReadPreference.Should().Be(subject.ReadPreference);
            result.WriteConcern.Should().Be(subject.WriteConcern);
        }

        [Test]
        public void WriteConcern_get_should_return_expected_result()
        {
            var subject = new GridFSBucketOptions { WriteConcern = WriteConcern.WMajority }.ToImmutable();

            var result = subject.WriteConcern;

            result.Should().Be(WriteConcern.WMajority);
        }
    }
}
