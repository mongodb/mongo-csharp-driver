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
using MongoDB.Driver.Core.Servers;
namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents a listener to server events.
    /// </summary>
    public interface IServerListener : IListener
    {
        // methods
        /// <summary>
        /// An event that occurs before closing a server. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerBeforeClosing(ServerBeforeClosingEvent @event);

        /// <summary>
        /// An event that occurs after server has been closed. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerAfterClosing(ServerAfterClosingEvent @event);

        /// <summary>
        /// An event that occurs before opening a server. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerBeforeOpening(ServerBeforeOpeningEvent @event);

        /// <summary>
        /// An event that occurs after a server has been opened. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerAfterOpening(ServerAfterOpeningEvent @event);

        /// <summary>
        /// An event that occurs before sending a heartbeat to a server. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerBeforeHeartbeating(ServerBeforeHeartbeatingEvent @event);

        /// <summary>
        /// An event that occurs after a heartbeat has been sent to a server. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerAfterHeartbeating(ServerAfterHeartbeatingEvent @event);

        /// <summary>
        /// An event that occurs when there is an error while sending a heartbeat to a server.
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerErrorHeartbeating(ServerErrorHeartbeatingEvent @event);

        /// <summary>
        /// An event that occurs after a server's description has changed. 
        /// </summary>
        /// <param name="event">The event.</param>
        void ServerAfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event);
    }
}
