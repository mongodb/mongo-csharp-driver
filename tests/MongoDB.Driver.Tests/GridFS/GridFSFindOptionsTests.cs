﻿/* Copyright 2015-present MongoDB Inc.
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
using MongoDB.Driver.GridFS;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    public class GridFSFindOptionsTests
    {
        [Fact]
        public void AllowDiskUse_get_should_return_expected_result()
        {
            var subject = new GridFSFindOptions { AllowDiskUse = true };

            var result = subject.AllowDiskUse;

            result.Should().Be(true);
        }

        [Fact]
        public void AllowDiskUse_set_should_have_expected_result()
        {
            var subject = new GridFSFindOptions();

            subject.AllowDiskUse = true;

            subject.AllowDiskUse.Should().Be(true);
        }

        [Fact]
        public void BatchSize_get_should_return_expected_result()
        {
            var subject = new GridFSFindOptions { BatchSize = 123 };

            var result = subject.BatchSize;

            result.Should().Be(123);
        }

        [Fact]
        public void BatchSize_set_should_have_expected_result()
        {
            var subject = new GridFSFindOptions();

            subject.BatchSize = 123;

            subject.BatchSize.Should().Be(123);
        }

        [Theory]
        [ParameterAttributeData]
        public void BatchSize_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            int value)
        {
            var subject = new GridFSFindOptions();

            Action action = () => subject.BatchSize = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void default_constructor_should_return_expected_result()
        {
            var result = new GridFSFindOptions();

            result.AllowDiskUse.Should().NotHaveValue();
            result.BatchSize.Should().NotHaveValue();
            result.Limit.Should().NotHaveValue();
            result.MaxTime.Should().NotHaveValue();
            result.NoCursorTimeout.Should().NotHaveValue();
            result.Skip.Should().NotHaveValue();
            result.Sort.Should().BeNull();
        }

        [Fact]
        public void Limit_get_should_return_expected_result()
        {
            var subject = new GridFSFindOptions { Limit = 123 };

            var result = subject.Limit;

            result.Should().Be(123);
        }

        [Fact]
        public void Limit_set_should_have_expected_result()
        {
            var subject = new GridFSFindOptions();

            subject.Limit = 123;

            subject.Limit.Should().Be(123);
        }

        [Fact]
        public void MaxTime_get_should_return_expected_result()
        {
            var subject = new GridFSFindOptions { MaxTime = TimeSpan.FromSeconds(123) };

            var result = subject.MaxTime;

            result.Should().Be(TimeSpan.FromSeconds(123));
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_have_expected_result(
            [Values(-10000, 0, 1, 9999, 10000, 10001)] long maxTimeTicks)
        {
            var subject = new GridFSFindOptions();
            var value = TimeSpan.FromTicks(maxTimeTicks);

            subject.MaxTime = value;

            subject.MaxTime.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -1)] long maxTimeTicks)
        {
            var subject = new GridFSFindOptions();
            var value = TimeSpan.FromTicks(maxTimeTicks);

            Action action = () => subject.MaxTime = value;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void NoCursorTimeout_get_should_return_expected_result()
        {
            var subject = new GridFSFindOptions { NoCursorTimeout = true };

            var result = subject.NoCursorTimeout;

            result.Should().Be(true);
        }

        [Fact]
        public void NoCursorTimeout_set_should_have_expected_result()
        {
            var subject = new GridFSFindOptions();

            subject.NoCursorTimeout = true;

            subject.NoCursorTimeout.Should().Be(true);
        }

        [Fact]
        public void Skip_get_should_return_expected_result()
        {
            var subject = new GridFSFindOptions { Skip = 123 };

            var result = subject.Skip;

            result.Should().Be(123);
        }

        [Fact]
        public void Skip_set_should_have_expected_result()
        {
            var subject = new GridFSFindOptions();

            subject.Skip = 123;

            subject.Skip.Should().Be(123);
        }

        [Fact]
        public void Skip_set_should_throw_when_value_is_invalid()
        {
            var subject = new GridFSFindOptions();

            Action action = () => subject.Skip = -1;

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void Sort_get_should_return_expected_result()
        {
            var sort = Builders<GridFSFileInfo>.Sort.Ascending(x => x.Length);
            var subject = new GridFSFindOptions { Sort = sort };

            var result = subject.Sort;

            result.Should().BeSameAs(sort);
        }

        [Fact]
        public void Sort_set_should_have_expected_result()
        {
            var subject = new GridFSFindOptions();
            var sort = Builders<GridFSFileInfo>.Sort.Ascending(x => x.Length);

            subject.Sort = sort;

            subject.Sort.Should().BeSameAs(sort);
        }
    }
}
