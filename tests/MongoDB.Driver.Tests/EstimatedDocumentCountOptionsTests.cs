/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class EstimatedDocumentCountOptionsTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new EstimatedDocumentCountOptions();

            result.MaxTime.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_should_return_expected_result(
            [Values(null, 1, 2)] int? maxTimeSeconds)
        {
            var value = maxTimeSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(maxTimeSeconds.Value) : null;
            var subject = new EstimatedDocumentCountOptions { MaxTime = value };

            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_have_expected_result(
            [Values(null, 1, 2)] int? maxTimeSeconds)
        {
            var value = maxTimeSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(maxTimeSeconds.Value) : null;
            var subject = new EstimatedDocumentCountOptions();

            subject.MaxTime = value;

            subject.MaxTime.Should().Be(value);
        }
    }
}
