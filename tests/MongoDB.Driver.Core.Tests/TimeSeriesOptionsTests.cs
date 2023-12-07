/* Copyright 2021-present MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver
{
    public class TimeSeriesOptionsTests
    {
        [Fact]
        public void constructor_with_timeField_should_initialize_instance()
        {
            const string timeField = "time";
            var result = new TimeSeriesOptions(timeField);

            result.TimeField.Should().Be(timeField);
            result.MetaField.Should().BeNull();
            result.Granularity.Should().BeNull();
        }

        [Fact]
        public void constructor_with_null_timeField_should_throw()
        {
            const string timeField = null;
            var exception = Record.Exception(() => new TimeSeriesOptions(timeField));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void constructor_with_empty_string_timeField_should_throw()
        {
            const string timeField = "";
            var exception = Record.Exception(() => new TimeSeriesOptions(timeField));
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void constructor_with_all_parameters_should_initialize_instance()
        {
            const string timeField = "time";
            const string metaField = "meta";
            const TimeSeriesGranularity granularity = TimeSeriesGranularity.Hours;
            const int bucketMaxSpanSeconds = 30;
            const int bucketRoundingSeconds = 30;

            var result = new TimeSeriesOptions(timeField, metaField, granularity, bucketMaxSpanSeconds, bucketRoundingSeconds);

            result.TimeField.Should().Be(timeField);
            result.MetaField.Should().Be(metaField);
            result.Granularity.Should().Be(granularity);
            result.BucketMaxSpanSeconds.Should().Be(bucketMaxSpanSeconds);
            result.BucketRoundingSeconds.Should().Be(bucketRoundingSeconds);
        }

        [Theory]
        [InlineData(true, "{ timeField: 'time' }")]
        [InlineData(false, "{ timeField: 'time', metaField: 'meta', granularity: 'hours', bucketMaxSpanSeconds: 30, bucketRoundingSeconds: 30 }")]
        public void ToBsonDocument_should_return_expected_result(bool withDefaults, string expected)
        {
            const string timeField = "time";
            const string metaField = "meta";
            const TimeSeriesGranularity granularity = TimeSeriesGranularity.Hours;
            const int bucketMaxSpanSeconds = 30;
            const int bucketRoundingSeconds = 30;

            var result = withDefaults
                ? new TimeSeriesOptions(timeField)
                : new TimeSeriesOptions(timeField, metaField, granularity, bucketMaxSpanSeconds, bucketRoundingSeconds);

            result.ToBsonDocument().Should().Be(expected);
        }
    }
}
