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
using System.Collections.Generic;
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class BuilderClusterConfiguratorTests
{
    private static readonly IReadOnlyList<IEventSubscriber> NoSubscribers = [];

    [Fact]
    public void IsEmpty_should_be_true_when_nothing_is_configured()
    {
        var subject = Create();

        subject.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_should_be_false_when_anything_is_configured()
    {
        Create(subscribers: [Mock.Of<IEventSubscriber>()]).IsEmpty.Should().BeFalse();
        Create(preServerSelector: Mock.Of<IServerSelector>()).IsEmpty.Should().BeFalse();
        Create(postServerSelector: Mock.Of<IServerSelector>()).IsEmpty.Should().BeFalse();
        Create(socketConfigurator: _ => { }).IsEmpty.Should().BeFalse();
        Create(maintenanceInterval: TimeSpan.FromSeconds(1)).IsEmpty.Should().BeFalse();
        Create(maintenanceInterval: System.Threading.Timeout.InfiniteTimeSpan).IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ToAction_should_apply_everything_that_is_configured()
    {
        var preServerSelector = Mock.Of<IServerSelector>();
        var postServerSelector = Mock.Of<IServerSelector>();
        Action<Socket> socketConfigurator = _ => { };
        var subject = Create([Mock.Of<IEventSubscriber>()], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10));
        var clusterBuilder = new ClusterBuilder();

        subject.ToAction()(clusterBuilder);

        ((ClusterSettings)Reflector.GetFieldValue(clusterBuilder, "_clusterSettings")).PreServerSelector.Should().BeSameAs(preServerSelector);
        ((ClusterSettings)Reflector.GetFieldValue(clusterBuilder, "_clusterSettings")).PostServerSelector.Should().BeSameAs(postServerSelector);
        ((TcpStreamSettings)Reflector.GetFieldValue(clusterBuilder, "_tcpStreamSettings")).SocketConfigurator.Should().BeSameAs(socketConfigurator);
        ((ConnectionPoolSettings)Reflector.GetFieldValue(clusterBuilder, "_connectionPoolSettings")).MaintenanceInterval.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void ToAction_should_only_apply_one_of_the_server_selectors_when_only_one_is_configured()
    {
        var postServerSelector = Mock.Of<IServerSelector>();
        var clusterBuilder = new ClusterBuilder();

        Create(postServerSelector: postServerSelector).ToAction()(clusterBuilder);

        var clusterSettings = (ClusterSettings)Reflector.GetFieldValue(clusterBuilder, "_clusterSettings");
        clusterSettings.PreServerSelector.Should().BeNull();
        clusterSettings.PostServerSelector.Should().BeSameAs(postServerSelector);
    }

    [Fact]
    public void Equals_should_return_true_for_equal_configurations()
    {
        var subscriber = Mock.Of<IEventSubscriber>();
        var preServerSelector = Mock.Of<IServerSelector>();
        var postServerSelector = Mock.Of<IServerSelector>();
        Action<Socket> socketConfigurator = _ => { };

        var subject1 = Create([subscriber], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10));
        var subject2 = Create([subscriber], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10));

        subject1.Equals(subject2).Should().BeTrue();
        subject1.Equals((object)subject2).Should().BeTrue();
        subject1.GetHashCode().Should().Be(subject2.GetHashCode());
    }

    [Fact]
    public void Equals_should_return_false_when_any_part_differs()
    {
        var subscriber = Mock.Of<IEventSubscriber>();
        var preServerSelector = Mock.Of<IServerSelector>();
        var postServerSelector = Mock.Of<IServerSelector>();
        Action<Socket> socketConfigurator = _ => { };
        var subject = Create([subscriber], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10));

        subject.Equals(Create([Mock.Of<IEventSubscriber>()], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10))).Should().BeFalse();
        subject.Equals(Create([], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10))).Should().BeFalse();
        subject.Equals(Create([subscriber], Mock.Of<IServerSelector>(), postServerSelector, socketConfigurator, TimeSpan.FromSeconds(10))).Should().BeFalse();
        subject.Equals(Create([subscriber], preServerSelector, Mock.Of<IServerSelector>(), socketConfigurator, TimeSpan.FromSeconds(10))).Should().BeFalse();
        subject.Equals(Create([subscriber], preServerSelector, postServerSelector, _ => { }, TimeSpan.FromSeconds(10))).Should().BeFalse();
        subject.Equals(Create([subscriber], preServerSelector, postServerSelector, socketConfigurator, TimeSpan.FromSeconds(11))).Should().BeFalse();
    }

    [Fact]
    public void Equals_should_return_false_for_null_and_other_types()
    {
        var subject = Create();

        subject.Equals((BuilderClusterConfigurator)null).Should().BeFalse();
        subject.Equals((object)null).Should().BeFalse();
        subject.Equals("not a configurator").Should().BeFalse();
    }

    [Fact]
    public void Constructor_should_throw_when_subscribers_is_null()
    {
        var exception = Record.Exception(() => new BuilderClusterConfigurator(null, null, null, null, ConnectionPoolSettings.DefaultMaintenanceInterval));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("subscribers");
    }

    private static BuilderClusterConfigurator Create(
        IReadOnlyList<IEventSubscriber> subscribers = null,
        IServerSelector preServerSelector = null,
        IServerSelector postServerSelector = null,
        Action<Socket> socketConfigurator = null,
        TimeSpan? maintenanceInterval = null)
        => new(
            subscribers ?? NoSubscribers,
            preServerSelector,
            postServerSelector,
            socketConfigurator,
            maintenanceInterval ?? ConnectionPoolSettings.DefaultMaintenanceInterval);
}
