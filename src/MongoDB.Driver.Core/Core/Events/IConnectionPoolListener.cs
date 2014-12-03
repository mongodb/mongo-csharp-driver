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
    public interface IConnectionPoolListener : IListener
    {
        void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event);
        void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event);

        void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event);
        void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event);

        void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event);
        void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event);

        void ConnectionPoolBeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event);
        void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event);

        void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event);
        void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event);
        void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event);

        void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event);
        void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event);
        void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event);

        void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event);
        void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event);
    }
}
