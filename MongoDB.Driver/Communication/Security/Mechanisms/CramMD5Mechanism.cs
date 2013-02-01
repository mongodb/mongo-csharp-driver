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
using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// A mechanism for CRAM-MD5 (http://tools.ietf.org/html/draft-ietf-sasl-crammd5-10).
    /// </summary>
    internal class CramMD5Mechanism : ISaslMechanism
    {
        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "CRAM-MD5"; }
        }

        // public methods
        /// <summary>
        /// Determines whether this instance can authenticate with the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can authenticate with the specified credential; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool CanUse(MongoCredential credential)
        {
            return credential.Mechanism == MongoAuthenticationMechanism.CRAM_MD5 &&
                credential.Evidence is PasswordEvidence;
        }

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>The initial step.</returns>
        public ISaslStep Initialize(MongoConnection connection, MongoCredential credential)
        {
            return new ManagedCramMD5Implementation(credential.Username, ((PasswordEvidence)credential.Evidence).Password);
        }
        
        // nested classes
        private class ManagedCramMD5Implementation : SaslImplementationBase, ISaslStep
        {
            // private fields
            private readonly string _username;
            private readonly string _password;
    
            // constructors
            public ManagedCramMD5Implementation(string username, string password)
            {
                _username = username;
                _password = password;
            }
    
            // public methods
            public byte[] BytesToSendToServer
            {
                get { return new byte[0]; }
            }
    
            // public methods
            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                var encoding = Encoding.UTF8;
                var mongoPassword = _username + ":mongo:" + _password;
                byte[] password;
                using (var md5 = MD5.Create())
                {
                    password = GetMongoPassword(md5, encoding, _username, _password);
                    var temp = ToHexString(password);
                    password = encoding.GetBytes(temp);
                }
    
                byte[] digest;
                using (var hmacMd5 = new HMACMD5(password))
                {
                    digest = hmacMd5.ComputeHash(bytesReceivedFromServer);
                }
    
                var response = _username + " " + ToHexString(digest);
                var bytesToSendToServer = encoding.GetBytes(response);
    
                return new SaslCompletionStep(bytesToSendToServer);
            }
        }
    }
}
