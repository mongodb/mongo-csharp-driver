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
    /// A step indicating the expected completion of a sasl conversation. Calling Transition
    /// on this step will result in an exception indicating a communication failure between
    /// the client and server.
    /// </summary>
    internal class SaslCompletionStep : ISaslStep
    {
        // private fields
        private readonly byte[] _output;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslCompletionStep" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public SaslCompletionStep(byte[] output)
        {
            _output = output;
        }

        // public properties
        /// <summary>
        /// Gets the output from the transition. The output will be used to send back to the server.
        /// </summary>
        public byte[] BytesToSendToServer
        {
            get { return _output; }
        }

        /// <summary>
        /// Gets a value indicating whether the conversation is complete.
        /// </summary>
        public bool IsComplete
        {
            get { return true; }
        }

        // public methods
        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="input">The input.</param>
        /// <returns>An ISaslStep.</returns>
        public ISaslStep Transition(SaslConversation conversation, byte[] input)
        {
            throw new MongoException("No more transitions available.  The conversation is completed.");
        }
    }
}