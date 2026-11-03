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

using System;
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class ConnectionPoolBuilderTests
{
    [Fact]
    public void MaintenanceInterval_should_default_to_one_minute()
    {
        var subject = new ConnectionPoolBuilder();

        subject.MaintenanceInterval.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60000)]
    public void MaintenanceInterval_should_accept_positive_values(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.MaintenanceInterval = value;

        subject.MaintenanceInterval.Should().Be(value);
    }

    [Fact]
    public void MaintenanceInterval_should_accept_Timeout_InfiniteTimeSpan()
    {
        var subject = new ConnectionPoolBuilder();

        subject.MaintenanceInterval = Timeout.InfiniteTimeSpan;

        subject.MaintenanceInterval.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-1000)]
    public void MaintenanceInterval_should_throw_when_value_is_zero_or_negative_and_not_infinite(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.MaintenanceInterval = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be(nameof(ConnectionPoolBuilder.MaintenanceInterval));
        subject.MaintenanceInterval.Should().Be(TimeSpan.FromMinutes(1));
    }
}
