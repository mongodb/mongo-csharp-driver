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
using System.Linq;
using System.Security;
using System.Security.Principal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Credentials to access a MongoDB database.
    /// </summary>
    [Serializable]
    public class MongoCredentials : IEquatable<MongoCredentials>
    {
        // private fields
        private readonly MongoAuthenticationProtocol _authenticationProtocol;
        private readonly MongoIdentity _identity;
        private readonly MongoIdentityEvidence _evidence;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCredentials" /> class.
        /// </summary>
        /// <param name="authenticationProtocol">Protocol to authenticate with.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="evidence">The evidence.</param>
        public MongoCredentials(MongoAuthenticationProtocol authenticationProtocol, MongoIdentity identity, MongoIdentityEvidence evidence)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            if (evidence == null)
            {
                throw new ArgumentNullException("evidence");
            }

            _authenticationProtocol = authenticationProtocol;
            _identity = identity;
            _evidence = evidence;
        }

        // public properties
        /// <summary>
        /// Gets the protocol to authenticate with.
        /// </summary>
        public MongoAuthenticationProtocol AuthenticationProtocol
        {
            get { return _authenticationProtocol; }
        }

        /// <summary>
        /// Gets the evidence.
        /// </summary>
        public MongoIdentityEvidence Evidence
        {
            get { return _evidence; }
        }

        /// <summary>
        /// Gets the identity.
        /// </summary>
        public MongoIdentity Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        [Obsolete("Use Evidence instead.")]
        public string Password
        {
            get
            {
                var passwordEvidence = _evidence as PasswordEvidence;
                if (passwordEvidence != null)
                {
                    return passwordEvidence.Password;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        public string Source
        {
            get { return _identity.Source; }
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username
        {
            get { return _identity.Username; }
        }

        // public operators
        /// <summary>
        /// Compares two MongoCredentials.
        /// </summary>
        /// <param name="lhs">The first MongoCredentials.</param>
        /// <param name="rhs">The other MongoCredentials.</param>
        /// <returns>True if the two MongoCredentials are equal (or both null).</returns>
        public static bool operator ==(MongoCredentials lhs, MongoCredentials rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two MongoCredentials.
        /// </summary>
        /// <param name="lhs">The first MongoCredentials.</param>
        /// <param name="rhs">The other MongoCredentials.</param>
        /// <returns>True if the two MongoCredentials are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoCredentials lhs, MongoCredentials rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Creates GSSAPI credentials.
        /// </summary>
        /// <returns>Credentials for GSSAPI.</returns>
        public static MongoCredentials CreateGssapiCredentials()
        {
            var username = string.Format("{0}@{1}", Environment.UserName, Environment.UserDomainName);
            return new MongoCredentials(
                MongoAuthenticationProtocol.Gssapi,
                new MongoExternalIdentity(username),
                new ProcessEvidence());
        }

        /// <summary>
        /// Creates GSSAPI credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Credentials for GSSAPI.</returns>
        public static MongoCredentials CreateGssapiCredentials(string username, string password)
        {
            return new MongoCredentials(
                MongoAuthenticationProtocol.Gssapi,
                new MongoExternalIdentity(username),
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates GSSAPI credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Credentials for GSSAPI.</returns>
        public static MongoCredentials CreateGssapiCredentials(string username, SecureString password)
        {
            return new MongoCredentials(
                MongoAuthenticationProtocol.Gssapi,
                new MongoExternalIdentity(username),
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates credentials used in negotiated authentication.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static MongoCredentials CreateStrongestCredentials(string databaseName, string username, string password)
        {
            return new MongoCredentials(
                MongoAuthenticationProtocol.Strongest,
                new MongoInternalIdentity(databaseName, username),
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates credentials used in negotiated authentication.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static MongoCredentials CreateStrongestCredentials(string databaseName, string username, SecureString password)
        {
            return new MongoCredentials(
                MongoAuthenticationProtocol.Strongest,
                new MongoInternalIdentity(databaseName, username),
                new PasswordEvidence(password));
        }

        // public methods
        /// <summary>
        /// Compares this MongoCredentials to another MongoCredentials.
        /// </summary>
        /// <param name="rhs">The other credentials.</param>
        /// <returns>True if the two credentials are equal.</returns>
        public bool Equals(MongoCredentials rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return _identity == rhs._identity && _evidence == rhs._evidence && _authenticationProtocol == rhs._authenticationProtocol;
        }

        /// <summary>
        /// Compares this MongoCredentials to another MongoCredentials.
        /// </summary>
        /// <param name="obj">The other credentials.</param>
        /// <returns>True if the two credentials are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoCredentials); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hashcode for the credentials.
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _identity.GetHashCode();
            hash = 37 * hash + _evidence.GetHashCode();
            hash = 37 * hash + _authenticationProtocol.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the credentials.
        /// </summary>
        /// <returns>A string representation of the credentials.</returns>
        public override string ToString()
        {
            return string.Format("{0}@{1}", _identity.Username, _identity.Source);
        }

        // internal static methods
        internal static MongoCredentials FromComponents(MongoAuthenticationProtocol protocol, string source, string databaseName, string username, string password)
        {
            source = source ?? databaseName ?? "admin";
            switch (protocol)
            {
                case MongoAuthenticationProtocol.Strongest:
                    // it is allowed for a password to be an empty string, but not a username
                    if(string.IsNullOrEmpty(username) || password == null)
                    {
                        return null;
                    }
                    return MongoCredentials.CreateStrongestCredentials(source, username, password);
                case MongoAuthenticationProtocol.Gssapi:
                    if (source != null && source != "$external")
                    {
                        throw new ArgumentException("Cannot specify source for GSSAPI Credentials.");
                    }
                    // it is allowed for a password to be an empty string, but not a username
                    if (string.IsNullOrEmpty(username) || password == null)
                    {
                        return MongoCredentials.CreateGssapiCredentials();
                    }
                    return MongoCredentials.CreateGssapiCredentials(username, password);
                default:
                    throw new NotSupportedException(string.Format("Unsupported MongoAuthenticationProtocol {0}.", protocol));
            }
        }

        // private methods
        private void ValidatePassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            if (password.Any(c => (int)c >= 128))
            {
                throw new ArgumentException("Password must contain only ASCII characters.");
            }
        }
    }
}
