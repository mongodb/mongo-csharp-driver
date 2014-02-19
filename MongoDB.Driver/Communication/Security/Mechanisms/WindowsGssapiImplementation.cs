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

using System;
using System.Text;
using MongoDB.Driver.Communication.Security.Mechanisms.Sspi;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// Implements the GSS API specification on Windows utilizing the native sspi libraries.
    /// </summary>
    internal class WindowsGssapiImplementation : ISaslStep
    {
        // private fields
        private readonly string _authorizationId;
        private readonly MongoIdentityEvidence _evidence;
        private readonly string _servicePrincipalName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsGssapiImplementation" /> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="hostRealm">The domain.</param>
        /// <param name="username">The username.</param>
        /// <param name="evidence">The evidence.</param>
        public WindowsGssapiImplementation(string serviceName, string hostName, string hostRealm, string username, MongoIdentityEvidence evidence)
        {
            _authorizationId = username;
            _evidence = evidence;
            _servicePrincipalName = string.Format("{0}/{1}", serviceName, hostName);
            if (!string.IsNullOrEmpty(hostRealm))
            {
                _servicePrincipalName += "@" + hostRealm;
            }
        }

        // properties
        /// <summary>
        /// The bytes that should be sent to ther server before calling Transition.
        /// </summary>
        public byte[] BytesToSendToServer
        {
            get { return new byte[0]; }
        }

        // public methods
        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from the server.</param>
        /// <returns>An ISaslStep.</returns>
        public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
        {
            SecurityCredential securityCredential;
            try
            {
                securityCredential = SecurityCredential.Acquire(SspiPackage.Kerberos, _authorizationId, _evidence);
                conversation.RegisterItemForDisposal(securityCredential);
            }
            catch (Win32Exception ex)
            {
                throw new MongoSecurityException("Unable to acquire security credential.", ex);
            }

            byte[] bytesToSendToServer;
            SecurityContext context;
            try
            {
                context = SecurityContext.Initialize(securityCredential, _servicePrincipalName, bytesReceivedFromServer, out bytesToSendToServer);
            }
            catch (Win32Exception ex)
            {
                if (_evidence is PasswordEvidence)
                {
                    throw new MongoSecurityException("Unable to initialize security context. Ensure the username and password are correct.", ex);
                }
                else
                {
                    throw new MongoSecurityException("Unable to initialize security context.", ex);
                }
            }

            if (!context.IsInitialized)
            {
                return new SspiInitializeStep(_servicePrincipalName, _authorizationId, context, bytesToSendToServer);
            }

            return new SspiNegotiateStep(_authorizationId, context, bytesToSendToServer);
        }

        // nested classes
        private class SspiInitializeStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly SecurityContext _context;
            private readonly byte[] _bytesReceivedFromServer;
            private readonly string _servicePrincipalName;

            public SspiInitializeStep(string servicePrincipalName, string authorizationId, SecurityContext context, byte[] bytesToSendToServer)
            {
                _servicePrincipalName = servicePrincipalName;
                _authorizationId = authorizationId;
                _context = context;
                _bytesReceivedFromServer = bytesToSendToServer ?? new byte[0];
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesReceivedFromServer; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                byte[] bytesToSendToServer;
                try
                {
                    _context.Initialize(_servicePrincipalName, bytesReceivedFromServer, out bytesToSendToServer);
                }
                catch (Win32Exception ex)
                {
                    throw new MongoSecurityException("Unable to initialize security context", ex);
                }

                if (!_context.IsInitialized)
                {
                    return new SspiInitializeStep(_servicePrincipalName, _authorizationId, _context, bytesToSendToServer);
                }

                return new SspiNegotiateStep(_authorizationId, _context, bytesToSendToServer);
            }
        }

        private class SspiNegotiateStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly SecurityContext _context;
            private readonly byte[] _bytesToSendToServer;

            public SspiNegotiateStep(string authorizationId, SecurityContext context, byte[] bytesToSendToServer)
            {
                _authorizationId = authorizationId;
                _context = context;
                _bytesToSendToServer = bytesToSendToServer ?? new byte[0];
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                // Even though RFC says that clients should specifically check this and raise an error
                // if it isn't true, this breaks on Windows XP, so we are skipping the check for windows
                // XP, identified as Win32NT 5.1: http://msdn.microsoft.com/en-us/library/windows/desktop/ms724832(v=vs.85).aspx
                if (Environment.OSVersion.Platform != PlatformID.Win32NT ||
                    Environment.OSVersion.Version.Major != 5)
                {
                    if (bytesReceivedFromServer == null || bytesReceivedFromServer.Length != 32) //RFC specifies this must be 4 octets
                    {
                        throw new MongoSecurityException("Invalid server response.");
                    }
                }

                byte[] decryptedBytes;
                try
                {
                    _context.DecryptMessage(0, bytesReceivedFromServer, out decryptedBytes);
                }
                catch (Win32Exception ex)
                {
                    throw new MongoSecurityException("Unabled to decrypt message.", ex);
                }

                int length = 4;
                if (_authorizationId != null)
                {
                    length += _authorizationId.Length;
                }

                bytesReceivedFromServer = new byte[length];
                bytesReceivedFromServer[0] = 0x1; // NO_PROTECTION
                bytesReceivedFromServer[1] = 0x0; // NO_PROTECTION
                bytesReceivedFromServer[2] = 0x0; // NO_PROTECTION
                bytesReceivedFromServer[3] = 0x0; // NO_PROTECTION

                if (_authorizationId != null)
                {
                    var authorizationIdBytes = Encoding.UTF8.GetBytes(_authorizationId);
                    authorizationIdBytes.CopyTo(bytesReceivedFromServer, 4);
                }

                byte[] bytesToSendToServer;
                try
                {
                    _context.EncryptMessage(bytesReceivedFromServer, out bytesToSendToServer);
                }
                catch (Win32Exception ex)
                {
                    throw new MongoSecurityException("Unabled to encrypt message.", ex);
                }

                return new SaslCompletionStep(bytesToSendToServer);
            }
        }
    }
}