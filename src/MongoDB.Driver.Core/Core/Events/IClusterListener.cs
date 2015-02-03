/* Copyright 2013-2014 MongoDB Inc.
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
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents a listener to cluster events.
    /// </summary>
    public interface IClusterListener : IListener
    {
        // methods
        /// <summary>
        /// An event that occurs before closing a cluster.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterBeforeClosing(ClusterBeforeClosingEvent @event);

        /// <summary>
        /// An event that occurs after a cluster has been closed.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterAfterClosing(ClusterAfterClosingEvent @event);

        /// <summary>
        /// An event that occurs before opening a cluster.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event);

        /// <summary>
        /// An event that occurs after a cluster has been opened.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterAfterOpening(ClusterAfterOpeningEvent @event);

        /// <summary>
        /// An event that occurs before adding a server to a cluster.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event);

        /// <summary>
        /// An event that occurs after a server has been added to a cluster.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event);

        /// <summary>
        /// An event that occurs before removing a server from a cluster.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event);

        /// <summary>
        /// An event that occurs after a server has been removed from a cluster.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event);

        /// <summary>
        /// An event that occurs after a cluster's description has changed.
        /// </summary>
        /// <param name="event">The event.</param>
        void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event);
    }
}