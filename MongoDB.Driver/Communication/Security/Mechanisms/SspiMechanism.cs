using MongoDB.Driver.Security.Sspi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MongoDB.Driver.Security.Mechanisms
{
    /// <summary>
    /// A mechanism implementing the GSS API specification on Windows utilizing the native sspi libraries.
    /// </summary>
    internal class SspiMechanism : ISaslMechanism
    {
        // private fields
        private readonly string _authorizationId;
        private readonly MongoClientIdentity _identity;
        private readonly string _servicePrincipalName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SspiMechanism" /> class.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="identity">The identity.</param>
        public SspiMechanism(string serverName, MongoClientIdentity identity)
        {
            _authorizationId = identity.Username;
            _servicePrincipalName = "mongodb/" + serverName;
            _identity = identity;
        }

        // properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "GSSAPI"; }
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
            SecurityCredentials securityCredentials;
            try
            {
                securityCredentials = SecurityCredentials.Acquire(SspiPackage.Kerberos, _identity);
                conversation.RegisterUnmanagedResourceForDisposal(securityCredentials);
            }
            catch (Win32Exception ex)
            {
                throw new MongoSecurityException("Unable to acquire security credentials.", ex);
            }

            byte[] output;
            SecurityContext context;
            try
            {
                context = SecurityContext.Initialize(securityCredentials, _servicePrincipalName, input, out output);
            }
            catch (Win32Exception ex)
            {
                if (!(_identity is SystemMongoClientIdentity))
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
                return new SspiInitializeStep(_servicePrincipalName, _authorizationId, context, output);
            }

            return new SspiNegotiateStep(_authorizationId, context, output);
        }

        // nested classes
        private class SspiInitializeStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly SecurityContext _context;
            private readonly byte[] _output;
            private readonly string _servicePrincipalName;

            public SspiInitializeStep(string servicePrincipalName, string authorizationId, SecurityContext context, byte[] output)
            {
                _servicePrincipalName = servicePrincipalName;
                _authorizationId = authorizationId;
                _context = context;
                _output = output ?? new byte[0];
            }

            public byte[] Output
            {
                get { return _output; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] input)
            {
                byte[] output;
                try
                {
                    _context.Initialize(_servicePrincipalName, input, out output);
                }
                catch (Win32Exception ex)
                {
                    throw new MongoSecurityException("Unable to initialize security context", ex);
                }

                if (!_context.IsInitialized)
                {
                    return new SspiInitializeStep(_servicePrincipalName, _authorizationId, _context, output);
                }

                return new SspiNegotiateStep(_authorizationId, _context, output);
            }
        }

        private class SspiNegotiateStep : ISaslStep
        {
            private readonly string _authorizationId;
            private readonly SecurityContext _context;
            private readonly byte[] _output;

            public SspiNegotiateStep(string authorizationId, SecurityContext context, byte[] output)
            {
                _authorizationId = authorizationId;
                _context = context;
                _output = output ?? new byte[0];
            }

            public byte[] Output
            {
                get { return _output; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] input)
            {
                if (input == null || input.Length != 32) //RFC specifies this must be 4 octets
                {
                    throw new MongoSecurityException("Invalid server response.");
                }

                byte[] decryptedBytes;
                try
                {
                    _context.DecryptMessage(0, input, out decryptedBytes);
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

                input = new byte[length];
                input[0] = 0x1; // NO_PROTECTION
                input[1] = 0x0; // NO_PROTECTION
                input[2] = 0x0; // NO_PROTECTION
                input[3] = 0x0; // NO_PROTECTION

                if (_authorizationId != null)
                {
                    var authorizationIdBytes = Encoding.UTF8.GetBytes(_authorizationId);
                    authorizationIdBytes.CopyTo(input, 4);
                }

                byte[] output;
                try
                {
                    _context.EncryptMessage(input, out output);
                }
                catch(Win32Exception ex)
                {
                    throw new MongoSecurityException("Unabled to encrypt message.", ex);
                }

                return new SaslCompletionStep(output);
            }
        }
    }
}