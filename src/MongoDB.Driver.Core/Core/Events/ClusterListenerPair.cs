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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents a pair of cluster listeners. All events will be forwarded to both listeners.
    /// </summary>
    public class ClusterListenerPair : IClusterListener
    {
        // static
        /// <summary>
        /// Combines two cluster listeners.
        /// </summary>
        /// <param name="first">The first cluster listener, or null.</param>
        /// <param name="second">The second cluster listener, or null.</param>
        /// <returns>A combined cluster listener.</returns>
        public static IClusterListener Create(IClusterListener first, IClusterListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ClusterListenerPair(first, second);
        }

        // fields
        private readonly IClusterListener _first;
        private readonly IClusterListener _second;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterListenerPair"/> class.
        /// </summary>
        /// <param name="first">The first cluster listener.</param>
        /// <param name="second">The second cluster listener.</param>
        public ClusterListenerPair(IClusterListener first, IClusterListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        /// <inheritdoc/>
        public void ClusterBeforeClosing(ClusterBeforeClosingEvent @event)
        {
            _first.ClusterBeforeClosing(@event);
            _second.ClusterBeforeClosing(@event);
        }

        /// <inheritdoc/>
        public void ClusterAfterClosing(ClusterAfterClosingEvent @event)
        {
            _first.ClusterAfterClosing(@event);
            _second.ClusterAfterClosing(@event);
        }

        /// <inheritdoc/>
        public void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event)
        {
            _first.ClusterBeforeOpening(@event);
            _second.ClusterBeforeOpening(@event);
        }

        /// <inheritdoc/>
        public void ClusterAfterOpening(ClusterAfterOpeningEvent @event)
        {
            _first.ClusterAfterOpening(@event);
            _second.ClusterAfterOpening(@event);
        }

        /// <inheritdoc/>
        public void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
            _first.ClusterBeforeAddingServer(@event);
            _second.ClusterBeforeAddingServer(@event);
        }

        /// <inheritdoc/>
        public void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
            _first.ClusterAfterAddingServer(@event);
            _second.ClusterAfterAddingServer(@event);
        }

        /// <inheritdoc/>
        public void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
            _first.ClusterBeforeRemovingServer(@event);
            _second.ClusterBeforeRemovingServer(@event);
        }

        /// <inheritdoc/>
        public void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
            _first.ClusterAfterRemovingServer(@event);
            _second.ClusterAfterRemovingServer(@event);
        }

        /// <inheritdoc/>
        public void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
            _first.ClusterAfterDescriptionChanged(@event);
            _second.ClusterAfterDescriptionChanged(@event);
        }
    }
}