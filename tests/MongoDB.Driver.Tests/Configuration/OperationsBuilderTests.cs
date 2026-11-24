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
using FluentAssertions;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class OperationsBuilderTests
{
    [Fact]
    public void ServerSelectors_should_default_to_null()
    {
        var subject = new OperationsBuilder();

        subject.PreServerSelector.Should().BeNull();
        subject.PostServerSelector.Should().BeNull();
    }

    [Fact]
    public void ServerSelectors_should_be_settable_independently()
    {
        var preSelector = Mock.Of<IServerSelector>();
        var postSelector = Mock.Of<IServerSelector>();
        var subject = new OperationsBuilder();

        subject.PreServerSelector = preSelector;
        subject.PostServerSelector = postSelector;

        subject.PreServerSelector.Should().BeSameAs(preSelector);
        subject.PostServerSelector.Should().BeSameAs(postSelector);
    }

    [Fact]
    public void ApplyConnectionString_should_throw_when_read_preference_tags_are_given_without_a_mode()
    {
        var subject = new OperationsBuilder();
        var connectionString = new ConnectionString("mongodb://host1/?readPreferenceTags=dc:east");

        var exception = Record.Exception(() => subject.ApplyConnectionString(connectionString));

        exception.Should().BeOfType<MongoConfigurationException>();
    }

    [Fact]
    public void ApplyConnectionString_should_keep_defaults_when_options_are_not_specified()
    {
        var subject = new OperationsBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1"));

        subject.ReadConcern.Should().Be(ReadConcern.Default);
        subject.ReadPreference.Should().Be(ReadPreference.Primary);
        subject.WriteConcern.Should().Be(WriteConcern.Acknowledged);
        subject.RetryReads.Should().BeTrue();
        subject.RetryWrites.Should().BeTrue();
    }

    [Fact]
    public void Constructor_should_initialize_the_same_defaults_as_MongoClientSettings()
    {
        var expected = new MongoClientSettings();

        var subject = new OperationsBuilder();

        subject.EnableOverloadRetargeting.Should().Be(expected.EnableOverloadRetargeting);
        subject.LocalThreshold.Should().Be(expected.LocalThreshold);
        subject.MaxAdaptiveRetries.Should().Be(expected.MaxAdaptiveRetries);
        subject.ReadConcern.Should().Be(expected.ReadConcern);
        subject.ReadPreference.Should().Be(expected.ReadPreference);
        subject.RetryReads.Should().Be(expected.RetryReads);
        subject.RetryWrites.Should().Be(expected.RetryWrites);
        subject.ServerApi.Should().BeNull();
        subject.ServerSelectionTimeout.Should().Be(expected.ServerSelectionTimeout);
        subject.WriteConcern.Should().Be(expected.WriteConcern);
    }

    [Fact]
    public void Properties_should_be_settable()
    {
        var serverApi = new ServerApi(ServerApiVersion.V1);
        var subject = new OperationsBuilder();

        subject.EnableOverloadRetargeting = true;
        subject.LocalThreshold = TimeSpan.FromMilliseconds(30);
        subject.MaxAdaptiveRetries = 4;
        subject.ReadConcern = ReadConcern.Majority;
        subject.ReadPreference = ReadPreference.Secondary;
        subject.RetryReads = false;
        subject.RetryWrites = false;
        subject.ServerApi = serverApi;
        subject.ServerSelectionTimeout = TimeSpan.FromSeconds(9);
        subject.WriteConcern = WriteConcern.WMajority;

        subject.EnableOverloadRetargeting.Should().BeTrue();
        subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(30));
        subject.MaxAdaptiveRetries.Should().Be(4);
        subject.ReadConcern.Should().BeSameAs(ReadConcern.Majority);
        subject.ReadPreference.Should().BeSameAs(ReadPreference.Secondary);
        subject.RetryReads.Should().BeFalse();
        subject.RetryWrites.Should().BeFalse();
        subject.ServerApi.Should().BeSameAs(serverApi);
        subject.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(9));
        subject.WriteConcern.Should().BeSameAs(WriteConcern.WMajority);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(-1)]
    public void LocalThreshold_should_accept_zero_positive_and_infinite_values(int milliseconds)
    {
        var subject = new OperationsBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.LocalThreshold = value;

        subject.LocalThreshold.Should().Be(value);
    }

    [Fact]
    public void LocalThreshold_should_throw_when_value_is_negative_and_not_infinite()
    {
        var subject = new OperationsBuilder();

        var exception = Record.Exception(() => subject.LocalThreshold = TimeSpan.FromMilliseconds(-2));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("LocalThreshold");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void MaxAdaptiveRetries_should_accept_zero_and_positive_values(int value)
    {
        var subject = new OperationsBuilder();

        subject.MaxAdaptiveRetries = value;

        subject.MaxAdaptiveRetries.Should().Be(value);
    }

    [Fact]
    public void MaxAdaptiveRetries_should_throw_when_value_is_negative()
    {
        var subject = new OperationsBuilder();

        var exception = Record.Exception(() => subject.MaxAdaptiveRetries = -1);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("MaxAdaptiveRetries");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    public void ServerSelectionTimeout_should_accept_zero_and_positive_values(int milliseconds)
    {
        var subject = new OperationsBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.ServerSelectionTimeout = value;

        subject.ServerSelectionTimeout.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    public void ServerSelectionTimeout_should_throw_when_value_is_negative(int milliseconds)
    {
        var subject = new OperationsBuilder();

        var exception = Record.Exception(() => subject.ServerSelectionTimeout = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("ServerSelectionTimeout");
    }

    [Fact]
    public void ReadConcern_should_throw_when_value_is_null()
    {
        var subject = new OperationsBuilder();

        var exception = Record.Exception(() => subject.ReadConcern = null);

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("ReadConcern");
    }

    [Fact]
    public void ReadPreference_should_throw_when_value_is_null()
    {
        var subject = new OperationsBuilder();

        var exception = Record.Exception(() => subject.ReadPreference = null);

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("ReadPreference");
    }

    [Fact]
    public void WriteConcern_should_throw_when_value_is_null()
    {
        var subject = new OperationsBuilder();

        var exception = Record.Exception(() => subject.WriteConcern = null);

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("WriteConcern");
    }

    [Fact]
    public void ApplyConnectionString_should_apply_the_options()
    {
        var subject = new OperationsBuilder();
        var connectionString = new ConnectionString(
            "mongodb://host1/?localThresholdMS=20&serverSelectionTimeoutMS=5000&retryReads=false&retryWrites=false" +
            "&enableOverloadRetargeting=true&maxAdaptiveRetries=2" +
            "&readConcernLevel=majority&w=majority&wtimeoutMS=100&journal=true" +
            "&readPreference=secondary&readPreferenceTags=dc:east&maxStalenessSeconds=120");

        subject.ApplyConnectionString(connectionString);

        subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(20));
        subject.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(5));
        subject.RetryReads.Should().BeFalse();
        subject.RetryWrites.Should().BeFalse();
        subject.EnableOverloadRetargeting.Should().BeTrue();
        subject.MaxAdaptiveRetries.Should().Be(2);
        subject.ReadConcern.Level.Should().Be(ReadConcernLevel.Majority);
        subject.WriteConcern.W.Should().Be(WriteConcern.WMode.Majority);
        subject.WriteConcern.WTimeout.Should().Be(TimeSpan.FromMilliseconds(100));
        subject.WriteConcern.Journal.Should().BeTrue();
        subject.ReadPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
        subject.ReadPreference.TagSets.Should().HaveCount(1);
        subject.ReadPreference.MaxStaleness.Should().Be(TimeSpan.FromSeconds(120));
    }
}
