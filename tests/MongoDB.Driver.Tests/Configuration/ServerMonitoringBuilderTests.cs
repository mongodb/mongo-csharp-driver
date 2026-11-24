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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class ServerMonitoringBuilderTests
{
    [Fact]
    public void Constructor_should_initialize_defaults()
    {
        var subject = new ServerMonitoringBuilder();

        subject.HeartbeatInterval.Should().Be(ServerSettings.DefaultHeartbeatInterval);
        subject.HeartbeatTimeout.Should().Be(ServerSettings.DefaultHeartbeatTimeout);
        subject.Mode.Should().Be(ServerMonitoringMode.Auto);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2000)]
    public void HeartbeatInterval_should_accept_positive_values(int milliseconds)
    {
        var subject = new ServerMonitoringBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.HeartbeatInterval = value;

        subject.HeartbeatInterval.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2)]
    public void HeartbeatInterval_should_throw_when_value_is_zero_or_negative(int milliseconds)
    {
        var subject = new ServerMonitoringBuilder();

        var exception = Record.Exception(() => subject.HeartbeatInterval = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("HeartbeatInterval");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3000)]
    public void HeartbeatTimeout_should_accept_positive_values(int milliseconds)
    {
        var subject = new ServerMonitoringBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.HeartbeatTimeout = value;

        subject.HeartbeatTimeout.Should().Be(value);
    }

    [Fact]
    public void HeartbeatTimeout_should_accept_Timeout_InfiniteTimeSpan()
    {
        var subject = new ServerMonitoringBuilder { HeartbeatTimeout = TimeSpan.FromSeconds(1) };

        subject.HeartbeatTimeout = Timeout.InfiniteTimeSpan;

        subject.HeartbeatTimeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void HeartbeatTimeout_should_throw_when_value_is_zero_or_negative_and_not_infinite(int milliseconds)
    {
        var subject = new ServerMonitoringBuilder();

        var exception = Record.Exception(() => subject.HeartbeatTimeout = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("HeartbeatTimeout");
    }

    [Theory]
    [InlineData(ServerMonitoringMode.Auto)]
    [InlineData(ServerMonitoringMode.Poll)]
    [InlineData(ServerMonitoringMode.Stream)]
    public void Mode_should_be_settable(ServerMonitoringMode mode)
    {
        var subject = new ServerMonitoringBuilder();

        subject.Mode = mode;

        subject.Mode.Should().Be(mode);
    }

    [Fact]
    public void ApplyConnectionString_should_apply_the_options()
    {
        var subject = new ServerMonitoringBuilder();
        var connectionString = new ConnectionString(
            "mongodb://host1/?heartbeatFrequencyMS=2000&heartbeatTimeoutMS=3000&serverMonitoringMode=poll");

        subject.ApplyConnectionString(connectionString);

        subject.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(2));
        subject.HeartbeatTimeout.Should().Be(TimeSpan.FromSeconds(3));
        subject.Mode.Should().Be(ServerMonitoringMode.Poll);
    }

    [Fact]
    public void ApplyConnectionString_should_keep_defaults_when_options_are_not_specified()
    {
        var subject = new ServerMonitoringBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1"));

        subject.HeartbeatInterval.Should().Be(ServerSettings.DefaultHeartbeatInterval);
        subject.HeartbeatTimeout.Should().Be(ServerSettings.DefaultHeartbeatTimeout);
        subject.Mode.Should().Be(ServerMonitoringMode.Auto);
    }
}
