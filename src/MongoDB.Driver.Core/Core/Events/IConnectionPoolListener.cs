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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents a listener to connection pool events.
    /// </summary>
    public interface IConnectionPoolListener : IListener
    {
        /// <summary>
        /// An event that occurs before closing a connection pool.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event);

        /// <summary>
        /// An event that occurs after a connection pool has been closed. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event);

        /// <summary>
        /// An event that occurs before opening a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event);

        /// <summary>
        /// An event that occurs after a connection pool has been opened. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event);

        /// <summary>
        /// An event that occurs before adding a connection to a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event);

        /// <summary>
        /// An event that occurs after a connection has been added to a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event);

        /// <summary>
        /// An event that occurs before removing a connection from a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event);

        /// <summary>
        /// An event that occurs after a connection has been removed from a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event);

        /// <summary>
        /// An event that occurs before a task enters a connection pool's wait queue. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event);

        /// <summary>
        /// An event that occurs after a task has entered a connection pool's wait queue. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event);

        /// <summary>
        /// An event that occurs when there is an error while a task was entering the connection pool's wait queue.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event);

        /// <summary>
        /// An event that occurs before checking out a connection from a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event);

        /// <summary>
        /// An event that occurs after a connection has been checked out from a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event);

        /// <summary>
        /// An event that occurs when an error occurred while checking out a connection from a connection pool.
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event);

        /// <summary>
        /// An event that occurs before checking in a connection to a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event);

        /// <summary>
        /// An event that occurs after a connection has been checked in to a connection pool. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event);
    }
}
