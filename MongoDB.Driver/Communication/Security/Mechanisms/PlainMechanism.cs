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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// Implements the SASL-PLAIN rfc: http://tools.ietf.org/html/rfc4616.
    /// </summary>
    internal class PlainMechanism : ISaslMechanism
    {
        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "PLAIN"; }
        }

        // public methods
        /// <summary>
        /// Determines whether this instance can authenticate with the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can authenticate with the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredential credential)
        {
            return credential.Mechanism.Equals(Name, StringComparison.InvariantCultureIgnoreCase) &&
                credential.Evidence is PasswordEvidence;
        }

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>
        /// The initial step.
        /// </returns>
        public ISaslStep Initialize(MongoConnection connection, MongoCredential credential)
        {
            var securePassword = ((PasswordEvidence)credential.Evidence).SecurePassword;

            var dataString = string.Format("\0{0}\0{1}",
                credential.Username,
                MongoUtils.ToInsecureString(securePassword));

            var bytes = new UTF8Encoding(false, true).GetBytes(dataString);
            return new SaslCompletionStep(bytes);
        }
    }
}