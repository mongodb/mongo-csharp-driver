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
    internal class ConnectionPoolListenerPair : IConnectionPoolListener
    {
        // static
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
        public ConnectionPoolListenerPair(IConnectionPoolListener first, IConnectionPoolListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        // methods
        public void BeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
            _first.BeforeClosing(@event);
            _second.BeforeClosing(@event);
        }

        public void AfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            _first.AfterClosing(@event);
            _second.AfterClosing(@event);
        }

        public void BeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
            _first.BeforeOpening(@event);
            _second.BeforeOpening(@event);
        }

        public void AfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            _first.AfterOpening(@event);
            _second.AfterOpening(@event);
        }

        public void BeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
            _first.BeforeAddingAConnection(@event);
            _second.BeforeAddingAConnection(@event);
        }

        public void AfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            _first.AfterAddingAConnection(@event);
            _second.AfterAddingAConnection(@event);
        }

        public void BeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
            _first.BeforeRemovingAConnection(@event);
            _second.BeforeRemovingAConnection(@event);
        }

        public void AfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            _first.AfterRemovingAConnection(@event);
            _second.AfterRemovingAConnection(@event);
        }

        public void BeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
            _first.BeforeEnteringWaitQueue(@event);
            _second.BeforeEnteringWaitQueue(@event);
        }

        public void AfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
            _first.AfterEnteringWaitQueue(@event);
            _second.AfterEnteringWaitQueue(@event);
        }

        public void ErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
            _first.ErrorEnteringWaitQueue(@event);
            _second.ErrorEnteringWaitQueue(@event);
        }

        public void BeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
            _first.BeforeCheckingOutAConnection(@event);
            _second.BeforeCheckingOutAConnection(@event);
        }

        public void AfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            _first.AfterCheckingOutAConnection(@event);
            _second.AfterCheckingOutAConnection(@event);
        }

        public void ErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
            _first.ErrorCheckingOutAConnection(@event);
            _second.ErrorCheckingOutAConnection(@event);
        }

        public void BeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
            _first.BeforeCheckingInAConnection(@event);
            _second.BeforeCheckingInAConnection(@event);
        }

        public void AfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            _first.AfterCheckingInAConnection(@event);
            _second.AfterCheckingInAConnection(@event);
        }
    }
}