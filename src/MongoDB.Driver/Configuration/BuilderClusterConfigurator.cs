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
using System.Linq;
using System.Net.Sockets;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Configuration;

// The options of MongoClientBuilder that MongoClientSettings has no counterpart for, so they are applied to the
// ClusterBuilder instead. ClusterKey compares cluster configurators by reference, which would give every Build() its
// own cluster. Exposing the configuration as the target of the delegate lets ClusterKey recognize two builders with the
// same configuration (see ClusterKey.ClusterConfiguratorsAreEqual) so that their clients share a cluster.
internal sealed class BuilderClusterConfigurator : IEquatable<BuilderClusterConfigurator>
{
    private readonly TimeSpan _maintenanceInterval;
    private readonly IServerSelector _postServerSelector;
    private readonly IServerSelector _preServerSelector;
    private readonly Action<Socket> _socketConfigurator;
    private readonly IReadOnlyList<IEventSubscriber> _subscribers;

    public BuilderClusterConfigurator(
        IReadOnlyList<IEventSubscriber> subscribers,
        IServerSelector preServerSelector,
        IServerSelector postServerSelector,
        Action<Socket> socketConfigurator,
        TimeSpan maintenanceInterval)
    {
        _subscribers = Ensure.IsNotNull(subscribers, nameof(subscribers));
        _preServerSelector = preServerSelector;
        _postServerSelector = postServerSelector;
        _socketConfigurator = socketConfigurator;
        _maintenanceInterval = maintenanceInterval;
    }

    // true when there is nothing to apply
    public bool IsEmpty =>
        _subscribers.Count == 0 &&
        _preServerSelector == null &&
        _postServerSelector == null &&
        _socketConfigurator == null &&
        _maintenanceInterval == ConnectionPoolSettings.DefaultMaintenanceInterval;

    public Action<ClusterBuilder> ToAction() => Configure;

    public bool Equals(BuilderClusterConfigurator other)
    {
        if (other is null)
        {
            return false;
        }

        // subscribers are compared by reference on purpose: a client sharing a cluster that is already opened would not
        // receive the events that were raised before it was built
        return
            _maintenanceInterval == other._maintenanceInterval &&
            Equals(_postServerSelector, other._postServerSelector) &&
            Equals(_preServerSelector, other._preServerSelector) &&
            Equals(_socketConfigurator, other._socketConfigurator) &&
            _subscribers.SequenceEqual(other._subscribers);
    }

    public override bool Equals(object obj)
        => Equals(obj as BuilderClusterConfigurator);

    public override int GetHashCode()
        => new Hasher()
            .Hash(_maintenanceInterval)
            .Hash(_postServerSelector)
            .Hash(_preServerSelector)
            .Hash(_socketConfigurator)
            .HashElements(_subscribers)
            .GetHashCode();

    private void Configure(ClusterBuilder builder)
    {
        foreach (var subscriber in _subscribers)
        {
            builder.Subscribe(subscriber);
        }

        if (_preServerSelector != null || _postServerSelector != null)
        {
            var preServerSelector = _preServerSelector;
            var postServerSelector = _postServerSelector;
            builder.ConfigureCluster(clusterSettings => clusterSettings.With(
                preServerSelector: preServerSelector != null ? Optional.Create(preServerSelector) : default,
                postServerSelector: postServerSelector != null ? Optional.Create(postServerSelector) : default));
        }

        if (_socketConfigurator != null)
        {
            var socketConfigurator = _socketConfigurator;
            builder.ConfigureTcp(tcpSettings => tcpSettings.With(socketConfigurator: Optional.Create(socketConfigurator)));
        }

        if (_maintenanceInterval != ConnectionPoolSettings.DefaultMaintenanceInterval)
        {
            var maintenanceInterval = _maintenanceInterval;
            builder.ConfigureConnectionPool(poolSettings => poolSettings.With(maintenanceInterval: maintenanceInterval));
        }
    }
}
