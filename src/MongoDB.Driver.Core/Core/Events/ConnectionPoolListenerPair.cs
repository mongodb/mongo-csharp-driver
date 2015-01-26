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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents a pair of connection pool listeners. All events will be forwarded to both listeners.
    /// </summary>
    public class ConnectionPoolListenerPair : IConnectionPoolListener
    {
        // static
        /// <summary>
        /// Combines two connection pool listeners.
        /// </summary>
        /// <param name="first">The first connection pool listener, or null.</param>
        /// <param name="second">The second connection pool listener, or null.</param>
        /// <returns>A combined connection pool listener.</returns>
        public static IConnectionPoolListener Create(IConnectionPoolListener first, IConnectionPoolListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ConnectionPoolListenerPair(first, second);
        }

        // fields
        private readonly IConnectionPoolListener _first;
        private readonly IConnectionPoolListener _second;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolListenerPair"/> class.
        /// </summary>
        /// <param name="first">The first connection pool.</param>
        /// <param name="second">The second connection pool.</param>
        public ConnectionPoolListenerPair(IConnectionPoolListener first, IConnectionPoolListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        // methods
        /// <inheritdoc/>
        public void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
            _first.ConnectionPoolBeforeClosing(@event);
            _second.ConnectionPoolBeforeClosing(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            _first.ConnectionPoolAfterClosing(@event);
            _second.ConnectionPoolAfterClosing(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
            _first.ConnectionPoolBeforeOpening(@event);
            _second.ConnectionPoolBeforeOpening(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            _first.ConnectionPoolAfterOpening(@event);
            _second.ConnectionPoolAfterOpening(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
            _first.ConnectionPoolBeforeAddingAConnection(@event);
            _second.ConnectionPoolBeforeAddingAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            _first.ConnectionPoolAfterAddingAConnection(@event);
            _second.ConnectionPoolAfterAddingAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
            _first.ConnectionPoolBeforeRemovingAConnection(@event);
            _second.ConnectionPoolBeforeRemovingAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            _first.ConnectionPoolAfterRemovingAConnection(@event);
            _second.ConnectionPoolAfterRemovingAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
            _first.ConnectionPoolBeforeEnteringWaitQueue(@event);
            _second.ConnectionPoolBeforeEnteringWaitQueue(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
            _first.ConnectionPoolAfterEnteringWaitQueue(@event);
            _second.ConnectionPoolAfterEnteringWaitQueue(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
            _first.ConnectionPoolErrorEnteringWaitQueue(@event);
            _second.ConnectionPoolErrorEnteringWaitQueue(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
            _first.ConnectionPoolBeforeCheckingOutAConnection(@event);
            _second.ConnectionPoolBeforeCheckingOutAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            _first.ConnectionPoolAfterCheckingOutAConnection(@event);
            _second.ConnectionPoolAfterCheckingOutAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
            _first.ConnectionPoolErrorCheckingOutAConnection(@event);
            _second.ConnectionPoolErrorCheckingOutAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
            _first.ConnectionPoolBeforeCheckingInAConnection(@event);
            _second.ConnectionPoolBeforeCheckingInAConnection(@event);
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            _first.ConnectionPoolAfterCheckingInAConnection(@event);
            _second.ConnectionPoolAfterCheckingInAConnection(@event);
        }
    }
}