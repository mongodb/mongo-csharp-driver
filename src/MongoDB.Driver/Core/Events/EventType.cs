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

namespace MongoDB.Driver.Core.Events
{
    internal enum EventType
    {
        ClusterAddedServer = 0,
        ClusterAddingServer,
        ClusterClosed,
        ClusterClosing,
        ClusterDescriptionChanged,
        ClusterOpened,
        ClusterOpening,
        ClusterRemovedServer,
        ClusterRemovingServer,
        ClusterSelectedServer,
        ClusterSelectingServer,
        ClusterEnteredSelectionWaitQueue,
        ClusterSelectingServerFailed,
        CommandFailed,
        CommandStarted,
        CommandSucceeded,
        ConnectionClosed,
        ConnectionClosing,
        ConnectionCreated,
        ConnectionFailed,
        ConnectionOpened,
        ConnectionOpening,
        ConnectionOpeningFailed,
        ConnectionPoolAddedConnection,
        ConnectionPoolAddingConnection,
        ConnectionPoolCheckedInConnection,
        ConnectionPoolCheckedOutConnection,
        ConnectionPoolCheckingInConnection,
        ConnectionPoolCheckingOutConnection,
        ConnectionPoolCheckingOutConnectionFailed,
        ConnectionPoolCleared,
        ConnectionPoolClearing,
        ConnectionPoolClosed,
        ConnectionPoolClosing,
        ConnectionPoolOpened,
        ConnectionPoolOpening,
        ConnectionPoolReady,
        ConnectionPoolRemovedConnection,
        ConnectionPoolRemovingConnection,
        ConnectionReceivedMessage,
        ConnectionReceivingMessage,
        ConnectionReceivingMessageFailed,
        ConnectionSendingMessages,
        ConnectionSendingMessagesFailed,
        ConnectionSentMessages,
        SdamInformation,
        ServerClosed,
        ServerClosing,
        ServerDescriptionChanged,
        ServerHeartbeatFailed,
        ServerHeartbeatStarted,
        ServerHeartbeatSucceeded,
        ServerOpened,
        ServerOpening
    }
}
