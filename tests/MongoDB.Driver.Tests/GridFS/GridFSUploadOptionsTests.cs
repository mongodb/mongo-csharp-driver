/* Copyright 2015-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    public class GridFSUploadOptionsTests
    {
        [Fact]
        public void BatchSize_get_should_return_expected_result()
        {
            var subject = new GridFSUploadOptions { BatchSize = 123 };

            var result = subject.BatchSize;

            result.Should().Be(123);
        }

        [Fact]
        public void BatchSize_set_should_have_expected_result()
        {
            var subject = new GridFSUploadOptions();

            subject.BatchSize = 123;

            subject.BatchSize.Should().Be(123);
        }

        [Theory]
        [ParameterAttributeData]
        public void BatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int value)
        {
            var subject = new GridFSUploadOptions();

            Action action = () => subject.BatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void ChunkSizeBytes_get_should_return_expected_result()
        {
            var subject = new GridFSUploadOptions { ChunkSizeBytes = 123 };

            var result = subject.ChunkSizeBytes;

            result.Should().Be(123);
        }

        [Fact]
        public void ChunkSizeBytes_set_should_have_expected_result()
        {
            var subject = new GridFSUploadOptions();

            subject.ChunkSizeBytes = 123;

            subject.ChunkSizeBytes.Should().Be(123);
        }

        [Theory]
        [ParameterAttributeData]
        public void ChunkSizeBytes_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int value)
        {
            var subject = new GridFSUploadOptions();

            Action action = () => subject.ChunkSizeBytes = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void default_constructor_should_return_expected_result()
        {
            var result = new GridFSUploadOptions();

#pragma warning disable 618
            result.BatchSize.Should().NotHaveValue();
            result.ChunkSizeBytes.Should().NotHaveValue();
            result.Metadata.Should().BeNull();
#pragma warning restore
        }

        [Fact]
        public void Metadata_get_should_return_expected_result()
        {
            var value = new BsonDocument("meta", 1);
            var subject = new GridFSUploadOptions { Metadata = value };

            var result = subject.Metadata;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Metadata_set_should_have_expected_result()
        {
            var subject = new GridFSUploadOptions();
            var value = new BsonDocument("meta", 1);

            subject.Metadata = value;

            subject.Metadata.Should().BeSameAs(value);
        }
    }
}
