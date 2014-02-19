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

using System.Collections.Generic;
using MongoDB.Driver.Communication.Security.Mechanisms.Gsasl;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// A base class for implementing a mechanism using Libgsasl.
    /// </summary>
    internal abstract class GsaslImplementationBase : SaslImplementationBase, ISaslStep
    {
        // private fields
        private readonly string _name;
        private readonly byte[] _output;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslImplementationBase" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="output">The output.</param>
        protected GsaslImplementationBase(string name, byte[] output)
        {
            _name = name;
            _output = output;
        }

        // public properties
        /// <summary>
        /// The bytes that should be sent to ther server before calling Transition.
        /// </summary>
        public byte[] BytesToSendToServer
        {
            get { return _output; }
        }

        // public methods
        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="input">The input.</param>
        /// <returns>An ISaslStep.</returns>
        /// <exception cref="MongoSecurityException">Unable to initialize context.</exception>
        public ISaslStep Transition(SaslConversation conversation, byte[] input)
        {
            GsaslContext context;
            try
            {
                context = GsaslContext.Initialize();
                conversation.RegisterItemForDisposal(context);
            }
            catch (GsaslException ex)
            {
                throw new MongoSecurityException("Unable to initialize context.", ex);
            }

            GsaslSession session;
            try
            {
                session = context.BeginSession(_name);
                conversation.RegisterItemForDisposal(session);
            }
            catch (GsaslException ex)
            {
                throw new MongoSecurityException("Unable to start a session.", ex);
            }

            foreach (var property in GetProperties())
            {
                session.SetProperty(property.Key, property.Value);
            }

            return new GsaslAuthenticateStep(session, null)
                .Transition(conversation, input);
        }

        // protected methods
        /// <summary>
        /// Gets the properties that should be used in the specified mechanism.
        /// </summary>
        /// <returns>The properties.</returns>
        protected abstract IEnumerable<KeyValuePair<string, string>> GetProperties();

        // nested classes
        private class GsaslAuthenticateStep : ISaslStep
        {
            // private fields
            private readonly byte[] _bytesToSendToServer;
            private GsaslSession _session;

            // constructors
            public GsaslAuthenticateStep(GsaslSession session, byte[] bytesToSendToServer)
            {
                _session = session;
                _bytesToSendToServer = bytesToSendToServer;
            }

            // public properties
            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            // public methods
            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                try
                {
                    var bytesToSendToServer = _session.Step(bytesReceivedFromServer);
                    if (_session.IsComplete)
                    {
                        return new SaslCompletionStep(bytesToSendToServer);
                    }

                    return new GsaslAuthenticateStep(_session, bytesToSendToServer);
                }
                catch (GsaslException ex)
                {
                    throw new MongoSecurityException("Unable to authenticate.", ex);
                }
            }
        }
    }
}