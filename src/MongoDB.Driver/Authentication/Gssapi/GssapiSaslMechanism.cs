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
using System.Net;
using System.Security;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.Gssapi
{
    internal sealed class GssapiSaslMechanism : ISaslMechanism
    {
        public const string MechanismName = "GSSAPI";

        private const string DefaultServiceName = "mongodb";
        private const string CanonicalizeHostNamePropertyName = "CANONICALIZE_HOST_NAME";
        private const string RealmPropertyName = "REALM";
        private const string ServiceNamePropertyName = "SERVICE_NAME";
        private const string ServiceRealmPropertyName = "SERVICE_REALM";

        public static GssapiSaslMechanism Create(SaslContext context)
        {
            Ensure.IsNotNull(context, nameof(context));
            if (context.Mechanism != MechanismName)
            {
                throw new InvalidOperationException($"Unexpected authentication mechanism: {context.Mechanism}");
            }

            SecureString password = null;
            if (context.IdentityEvidence is PasswordEvidence passwordEvidence)
            {
                password = passwordEvidence.SecurePassword;
            }

            var serviceName = DefaultServiceName;
            var canonicalizeHostName = false;
            string realm = null;
            if (context.MechanismProperties != null)
            {
                foreach (var pair in context.MechanismProperties)
                {
                    switch (pair.Key.ToUpperInvariant())
                    {
                        case ServiceNamePropertyName:
                            serviceName = (string)pair.Value;
                            break;
                        case ServiceRealmPropertyName:
                        case RealmPropertyName:
                            realm = (string)pair.Value;
                            break;
                        case CanonicalizeHostNamePropertyName:
                            canonicalizeHostName = bool.Parse((string)pair.Value);
                            break;
                        default:
                            var message = string.Format("Unknown GSSAPI property '{0}'.", pair.Key);
                            throw new ArgumentException(message, "properties");
                    }
                }
            }

            return new GssapiSaslMechanism(serviceName, canonicalizeHostName, realm, context.Identity.Username, password);
        }

        private readonly SecureString _password;
        private readonly string _username;

        public GssapiSaslMechanism(string serviceName, bool canonicalizeHostName, string realm, string username, SecureString password)
        {
            ServiceName = serviceName;
            CanonicalizeHostName = canonicalizeHostName;
            Realm = realm;
            _username = username;
            _password = password;
        }

        public bool CanonicalizeHostName { get; }

        public string DatabaseName => "$external";

        public string Name => MechanismName;

        public string Realm { get; }

        public string ServiceName { get; }

        public ISaslStep CreateSpeculativeAuthenticationStep() => null;

        public BsonDocument CustomizeSaslStartCommand(BsonDocument startCommand) => startCommand;

        public ISaslStep Initialize(SaslConversation conversation, ConnectionDescription description)
        {
            var hostName = conversation.EndPoint.GetHostAndPort().Host;
            if (string.IsNullOrEmpty(hostName))
            {
                throw new MongoAuthenticationException(conversation.ConnectionId, "Only DnsEndPoint and IPEndPoint are supported for GSSAPI authentication.");
            }

            if (CanonicalizeHostName)
            {
                var entry = Dns.GetHostEntry(hostName);
                if (entry != null)
                {
                    hostName = entry.HostName;
                }
            }

            return new GssapiFirstSaslStep(ServiceName, hostName, Realm, _username, _password);
        }

        public void OnReAuthenticationRequired()
        {
        }

        public bool TryHandleAuthenticationException(
            MongoException exception,
            ISaslStep step,
            SaslConversation conversation,
            ConnectionDescription description,
            out ISaslStep nextStep)
        {
            nextStep = null;
            return false;
        }
    }
}
