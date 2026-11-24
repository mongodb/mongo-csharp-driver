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

    [Theory]
    [InlineData(10, 5)]
    [InlineData(10, 10)]
    [InlineData(1, 0)]
    public void Validate_should_accept_max_size_greater_than_or_equal_to_min_size(int maxSize, int minSize)
    {
        var subject = new ConnectionPoolBuilder { MaxSize = maxSize, MinSize = minSize };

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeNull();
    }

    [Fact]
    public void Validate_should_throw_when_max_size_is_less_than_min_size()
    {
        var subject = new ConnectionPoolBuilder { MaxSize = 5, MinSize = 10 };

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain("MaxSize must be greater than or equal to MinSize");
    }

    [Fact]
    public void Constructor_should_initialize_defaults()
    {
        var subject = new ConnectionPoolBuilder();

        subject.MaxConnecting.Should().Be(MongoInternalDefaults.ConnectionPool.MaxConnecting);
        subject.MaxConnectionIdleTime.Should().Be(MongoDefaults.MaxConnectionIdleTime);
        subject.MaxConnectionLifeTime.Should().Be(MongoDefaults.MaxConnectionLifeTime);
        subject.MaxSize.Should().Be(MongoDefaults.MaxConnectionPoolSize);
        subject.MinSize.Should().Be(MongoDefaults.MinConnectionPoolSize);
        subject.WaitQueueTimeout.Should().Be(MongoDefaults.WaitQueueTimeout);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void MaxConnecting_should_accept_positive_values(int value)
    {
        var subject = new ConnectionPoolBuilder();

        subject.MaxConnecting = value;

        subject.MaxConnecting.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxConnecting_should_throw_when_value_is_zero_or_negative(int value)
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.MaxConnecting = value);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("MaxConnecting");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void MaxSize_should_accept_positive_values(int value)
    {
        var subject = new ConnectionPoolBuilder();

        subject.MaxSize = value;

        subject.MaxSize.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxSize_should_throw_when_value_is_zero_or_negative(int value)
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.MaxSize = value);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("MaxSize");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void MinSize_should_accept_zero_and_positive_values(int value)
    {
        var subject = new ConnectionPoolBuilder();

        subject.MinSize = value;

        subject.MinSize.Should().Be(value);
    }

    [Fact]
    public void MinSize_should_throw_when_value_is_negative()
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.MinSize = -1);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("MinSize");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60000)]
    public void MaxConnectionIdleTime_should_accept_zero_and_positive_values(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.MaxConnectionIdleTime = value;

        subject.MaxConnectionIdleTime.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    public void MaxConnectionIdleTime_should_throw_when_value_is_negative(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.MaxConnectionIdleTime = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("MaxConnectionIdleTime");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60000)]
    public void MaxConnectionLifeTime_should_accept_positive_values(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.MaxConnectionLifeTime = value;

        subject.MaxConnectionLifeTime.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxConnectionLifeTime_should_throw_when_value_is_zero_or_negative(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.MaxConnectionLifeTime = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("MaxConnectionLifeTime");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void WaitQueueTimeout_should_accept_zero_and_positive_values(int milliseconds)
    {
        var subject = new ConnectionPoolBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.WaitQueueTimeout = value;

        subject.WaitQueueTimeout.Should().Be(value);
    }

    [Fact]
    public void WaitQueueTimeout_should_accept_Timeout_InfiniteTimeSpan()
    {
        var subject = new ConnectionPoolBuilder { WaitQueueTimeout = TimeSpan.FromSeconds(1) };

        subject.WaitQueueTimeout = Timeout.InfiniteTimeSpan;

        subject.WaitQueueTimeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void WaitQueueTimeout_should_throw_when_value_is_negative_and_not_infinite()
    {
        var subject = new ConnectionPoolBuilder();

        var exception = Record.Exception(() => subject.WaitQueueTimeout = TimeSpan.FromMilliseconds(-2));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("WaitQueueTimeout");
    }

    [Fact]
    public void ApplyConnectionString_should_apply_the_options()
    {
        var subject = new ConnectionPoolBuilder();
        var connectionString = new ConnectionString(
            "mongodb://host1/?maxPoolSize=50&minPoolSize=5&maxConnecting=3&maxIdleTimeMS=1000&maxLifeTimeMS=60000&waitQueueTimeoutMS=500");

        subject.ApplyConnectionString(connectionString);

        subject.MaxSize.Should().Be(50);
        subject.MinSize.Should().Be(5);
        subject.MaxConnecting.Should().Be(3);
        subject.MaxConnectionIdleTime.Should().Be(TimeSpan.FromSeconds(1));
        subject.MaxConnectionLifeTime.Should().Be(TimeSpan.FromMinutes(1));
        subject.WaitQueueTimeout.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void ApplyConnectionString_should_treat_a_zero_max_pool_size_as_no_limit()
    {
        var subject = new ConnectionPoolBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1/?maxPoolSize=0"));

        subject.MaxSize.Should().Be(int.MaxValue);
    }

    [Fact]
    public void ApplyConnectionString_should_accept_a_zero_max_idle_time()
    {
        var subject = new ConnectionPoolBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1/?maxIdleTimeMS=0"));

        subject.MaxConnectionIdleTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ApplyConnectionString_should_keep_defaults_when_options_are_not_specified()
    {
        var subject = new ConnectionPoolBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1"));

        subject.MaxSize.Should().Be(MongoDefaults.MaxConnectionPoolSize);
        subject.MinSize.Should().Be(MongoDefaults.MinConnectionPoolSize);
        subject.MaxConnecting.Should().Be(MongoInternalDefaults.ConnectionPool.MaxConnecting);
        subject.MaxConnectionIdleTime.Should().Be(MongoDefaults.MaxConnectionIdleTime);
        subject.MaxConnectionLifeTime.Should().Be(MongoDefaults.MaxConnectionLifeTime);
        subject.WaitQueueTimeout.Should().Be(MongoDefaults.WaitQueueTimeout);
    }
}
