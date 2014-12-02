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
        void BeforeClosing(ConnectionPoolBeforeClosingEvent @event);
        void AfterClosing(ConnectionPoolAfterClosingEvent @event);

        void BeforeOpening(ConnectionPoolBeforeOpeningEvent @event);
        void AfterOpening(ConnectionPoolAfterOpeningEvent @event);

        void BeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event);
        void AfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event);

        void BeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event);
        void AfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event);

        void BeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event);
        void AfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event);
        void ErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event);

        void BeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event);
        void AfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event);
        void ErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event);

        void BeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event);
        void AfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event);
    }
}
