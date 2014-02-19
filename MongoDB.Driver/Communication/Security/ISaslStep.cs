/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// A step in a Sasl Conversation.
    /// </summary>
    internal interface ISaslStep
    {
        /// <summary>
        /// The bytes that should be sent to ther server before calling Transition.
        /// </summary>
        byte[] BytesToSendToServer { get; }

        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from the server.</param>
        /// <returns>An ISaslStep.</returns>
        ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer);
    }
}