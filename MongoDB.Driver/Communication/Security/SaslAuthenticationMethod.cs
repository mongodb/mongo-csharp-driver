/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Driver.Internal;
using MongoDB.Driver.Communication.Security;
using MongoDB.Driver.Communication.Security.Mechanisms;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authenticates a credential using the SASL protocol.
    /// </summary>
    internal class SaslAuthenticationMethod : IAuthenticationMethod
    {
        // private fields
        private readonly ISaslMechanism _mechanism;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticationMethod" /> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        public SaslAuthenticationMethod(ISaslMechanism mechanism)
        {
            _mechanism = mechanism;
        }

        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _mechanism.Name; }
        }

        // public methods
        /// <summary>
        /// Authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public void Authenticate(MongoConnection connection, MongoCredential credential)
        {
            using (var conversation = new SaslConversation())
            {
                var currentStep = _mechanism.Initialize(connection, credential);

                var command = new CommandDocument
                {
                    { "saslStart", 1 },
                    { "mechanism", _mechanism.Name },
                    { "payload", currentStep.BytesToSendToServer }
                };

                while (true)
                {
                    var result = connection.RunCommand(credential.Source, QueryFlags.SlaveOk, command, true);
                    var code = result.Response["code"].AsInt32;
                    if (code != 0)
                    {
                        HandleError(result, code);
                    }
                    if (result.Response["done"].AsBoolean)
                    {
                        break;
                    }

                    currentStep = currentStep.Transition(conversation, result.Response["payload"].AsByteArray);

                    command = new CommandDocument
                    {
                        { "saslContinue", 1 },
                        { "conversationId", result.Response["conversationId"].AsInt32 },
                        { "payload", currentStep.BytesToSendToServer }
                    };
                }
            }
        }

        /// <summary>
        /// Determines whether this instance can use the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can use the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredential credential)
        {
            return _mechanism.CanUse(credential);
        }

        // private methods
        private void HandleError(CommandResult result, int code)
        {
            throw new MongoSecurityException(string.Format("Error: {0} - {1}", code, result.Response["errmsg"].AsString));
        }
    }
}