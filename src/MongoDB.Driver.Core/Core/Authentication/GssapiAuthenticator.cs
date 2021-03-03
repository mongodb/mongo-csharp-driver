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

using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Text;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// A GSSAPI SASL authenticator.
    /// </summary>
    public sealed class GssapiAuthenticator : SaslAuthenticator
    {
        // constants
        private const string __canonicalizeHostNamePropertyName = "CANONICALIZE_HOST_NAME";
        private const string __realmPropertyName = "REALM";
        private const string __serviceNamePropertyName = "SERVICE_NAME";
        private const string __serviceRealmPropertyName = "SERVICE_REALM";

        // static properties
        /// <summary>
        /// Gets the name of the canonicalize host name property.
        /// </summary>
        /// <value>
        /// The name of the canonicalize host name property.
        /// </value>
        public static string CanonicalizeHostNamePropertyName
        {
            get { return __canonicalizeHostNamePropertyName; }
        }

        /// <summary>
        /// Gets the default service name.
        /// </summary>
        /// <value>
        /// The default service name.
        /// </value>
        public static string DefaultServiceName
        {
            get { return "mongodb"; }
        }

        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name of the mechanism.
        /// </value>
        public static string MechanismName
        {
            get { return "GSSAPI"; }
        }

        /// <summary>
        /// Gets the name of the realm property.
        /// </summary>
        /// <value>
        /// The name of the realm property.
        /// </value>
        [Obsolete("Use ServiceRealmPropertyName")]
        public static string RealmPropertyName
        {
            get { return __realmPropertyName; }
        }

        /// <summary>
        /// Gets the name of the service name property.
        /// </summary>
        /// <value>
        /// The name of the service name property.
        /// </value>
        public static string ServiceNamePropertyName
        {
            get { return __serviceNamePropertyName; }
        }

        /// <summary>
        /// Gets the name of the service realm property.
        /// </summary>
        /// <value>
        /// The name of the service realm property.
        /// </value>
        public static string ServiceRealmPropertyName
        {
            get { return __serviceRealmPropertyName; }
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <param name="properties">The properties.</param>
        [Obsolete("Use the newest overload instead.")]
        public GssapiAuthenticator(UsernamePasswordCredential credential, IEnumerable<KeyValuePair<string, string>> properties)
            : this(credential, properties, serverApi: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="serverApi">The server API.</param>
        public GssapiAuthenticator(
            UsernamePasswordCredential credential,
            IEnumerable<KeyValuePair<string, string>> properties,
            ServerApi serverApi)
            : base(CreateMechanism(credential, properties), serverApi)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiAuthenticator"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="properties">The properties.</param>
        [Obsolete("Use the newest overload instead.")]
        public GssapiAuthenticator(string username, IEnumerable<KeyValuePair<string, string>> properties)
            : this(username, properties, serverApi: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiAuthenticator"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="serverApi">The server API.</param>
        public GssapiAuthenticator(
            string username,
            IEnumerable<KeyValuePair<string, string>> properties,
            ServerApi serverApi)
            : base(CreateMechanism(username, null, properties), serverApi)
        {
        }

        /// <inheritdoc/>
        public override string DatabaseName
        {
            get { return "$external"; }
        }

        private static GssapiMechanism CreateMechanism(UsernamePasswordCredential credential, IEnumerable<KeyValuePair<string, string>> properties)
        {
            if (credential.Source != "$external")
            {
                throw new ArgumentException("GSSAPI authentication may only use the $external source.", "credential");
            }

            return CreateMechanism(credential.Username, credential.Password, properties);
        }

        private static GssapiMechanism CreateMechanism(string username, SecureString password, IEnumerable<KeyValuePair<string, string>> properties)
        {
            var serviceName = DefaultServiceName;
            var canonicalizeHostName = false;
            string realm = null;
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    switch (pair.Key.ToUpperInvariant())
                    {
                        case __serviceNamePropertyName:
                            serviceName = (string)pair.Value;
                            break;
                        case __serviceRealmPropertyName:
                        case __realmPropertyName:
                            realm = (string)pair.Value;
                            break;
                        case __canonicalizeHostNamePropertyName:
                            canonicalizeHostName = bool.Parse(pair.Value);
                            break;
                        default:
                            var message = string.Format("Unknown GSSAPI property '{0}'.", pair.Key);
                            throw new ArgumentException(message, "properties");
                    }
                }
            }

            return new GssapiMechanism(serviceName, canonicalizeHostName, realm, username, password);
        }

        // nested classes
        private class GssapiMechanism : ISaslMechanism
        {
            // fields
            private readonly bool _canonicalizeHostName;
            private readonly SecureString _password;
            private readonly string _realm;
            private readonly string _serviceName;
            private readonly string _username;

            public GssapiMechanism(string serviceName, bool canonicalizeHostName, string realm, string username, SecureString password)
            {
                _serviceName = serviceName;
                _canonicalizeHostName = canonicalizeHostName;
                _realm = realm;
                _username = Ensure.IsNotNullOrEmpty(username, nameof(username));
                _password = password;
            }

            public string Name
            {
                get { return MechanismName; }
            }

            public ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description)
            {
                Ensure.IsNotNull(connection, nameof(connection));
                Ensure.IsNotNull(description, nameof(description));

                string hostName;
                var dnsEndPoint = connection.EndPoint as DnsEndPoint;
                if (dnsEndPoint != null)
                {
                    hostName = dnsEndPoint.Host;
                }
                else if (connection.EndPoint is IPEndPoint)
                {
                    hostName = ((IPEndPoint)connection.EndPoint).Address.ToString();
                }
                else
                {
                    throw new MongoAuthenticationException(connection.ConnectionId, "Only DnsEndPoint and IPEndPoint are supported for GSSAPI authentication.");
                }

                if (_canonicalizeHostName)
                {
#if NETSTANDARD1_5
                    var entry = Dns.GetHostEntryAsync(hostName).GetAwaiter().GetResult();
#else
                    var entry = Dns.GetHostEntry(hostName);
#endif
                    if (entry != null)
                    {
                        hostName = entry.HostName;
                    }
                }

                return new FirstStep(_serviceName, hostName, _realm, _username, _password, conversation);
            }
        }

        private class FirstStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly byte[] _bytesToSendToServer;
            private readonly ISecurityContext _context;

            public FirstStep(string serviceName, string hostname, string realm, string username, SecureString password, SaslConversation conversation)
            {
                _authorizationId = username;

                try
                {
                    _context = SecurityContextFactory.InitializeSecurityContext(serviceName, hostname, realm, _authorizationId, password);
                    conversation.RegisterItemForDisposal(_context);
                    _bytesToSendToServer = _context.Next(null);
                }
                catch (GssapiException ex)
                {
                    if (password != null)
                    {
                        throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context. Ensure the username and password are correct.", ex);
                    }
                    else
                    {
                        throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context.", ex);
                    }
                }
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                byte[] bytesToSendToServer;
                try
                {
                    bytesToSendToServer = _context.Next(bytesReceivedFromServer);
                }
                catch (GssapiException ex)
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context", ex);
                }

                if (!_context.IsInitialized)
                {
                    return new InitializeStep(_authorizationId, _context, bytesToSendToServer);
                }

                return new NegotiateStep(_authorizationId, _context, bytesToSendToServer);
            }
        }

        private class InitializeStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly ISecurityContext _context;
            private readonly byte[] _bytesToSendToServer;

            public InitializeStep(string authorizationId, ISecurityContext context, byte[] bytesToSendToServer)
            {
                _authorizationId = authorizationId;
                _context = context;
                _bytesToSendToServer = bytesToSendToServer ?? new byte[0];
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                byte[] bytesToSendToServer;
                try
                {
                    bytesToSendToServer = _context.Next(bytesReceivedFromServer);
                }
                catch (GssapiException ex)
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to initialize security context", ex);
                }

                if (!_context.IsInitialized)
                {
                    return new InitializeStep(_authorizationId, _context, bytesToSendToServer);
                }

                return new NegotiateStep(_authorizationId, _context, bytesToSendToServer);
            }
        }

        private class NegotiateStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly ISecurityContext _context;
            private readonly byte[] _bytesToSendToServer;

            public NegotiateStep(string authorizationId, ISecurityContext context, byte[] bytesToSendToServer)
            {
                _authorizationId = authorizationId;
                _context = context;
                _bytesToSendToServer = bytesToSendToServer ?? new byte[0];
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                try
                {
                    // NOTE: We simply check whether we can successfully decrypt the message,
                    //       but don't do anything with the decrypted plaintext
                    _ = _context.DecryptMessage(0, bytesReceivedFromServer);
                }
                catch (GssapiException ex)
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to decrypt message.", ex);
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
                    bytesToSendToServer = _context.EncryptMessage(bytesReceivedFromServer);
                }
                catch (GssapiException ex)
                {
                    throw new MongoAuthenticationException(conversation.ConnectionId, "Unable to encrypt message.", ex);
                }

                return new CompletedStep(bytesToSendToServer);
            }
        }
    }
}
