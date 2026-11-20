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
}
