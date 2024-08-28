/* Copyright 2013-present MongoDB Inc.
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
using System.Security;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication
{
    internal sealed class UsernamePasswordCredential
    {
        private readonly Lazy<SecureString> _saslPreppedPassword;
        private string _source;
        private SecureString _password;
        private string _username;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UsernamePasswordCredential"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public UsernamePasswordCredential(string source, string username, string password)
            : this(source, username, SecureStringHelper.ToSecureString(password))
        {
            // Compute saslPreppedPassword immediately and store it securely while the password is already in
            // managed memory. We don't create a closure over the password so that it will hopefully get
            // garbage-collected sooner rather than later.
            var saslPreppedPassword = SecureStringHelper.ToSecureString(SaslPrepHelper.SaslPrepStored(password));
            _saslPreppedPassword = new Lazy<SecureString>(() => saslPreppedPassword);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsernamePasswordCredential"/> class.
        /// Less secure when used in conjunction with SCRAM-SHA-256, due to the need to store the password in a managed
        /// string in order to SaslPrep it.
        /// See <a href="https://github.com/mongodb/specifications/blob/master/source/auth/auth.rst#scram-sha-256">Driver Authentication: SCRAM-SHA-256</a>
        /// for additional details.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public UsernamePasswordCredential(string source, string username, SecureString password)
        {
            _source = Ensure.IsNotNullOrEmpty(source, nameof(source));
            _username = Ensure.IsNotNullOrEmpty(username, nameof(username));
            _password = Ensure.IsNotNull(password, nameof(password));
            // defer computing the saslPreppedPassword until we need to since this will leak the password into managed
            // memory
            _saslPreppedPassword = new Lazy<SecureString>(
                () => SecureStringHelper.ToSecureString(SaslPrepHelper.SaslPrepStored(GetInsecurePassword())));
        }

        public SecureString Password
        {
            get { return _password; }
        }

        public SecureString SaslPreppedPassword
        {
            get { return _saslPreppedPassword.Value; }
        }

        public string Source
        {
            get { return _source; }
        }

        public string Username
        {
            get { return _username; }
        }

        public string GetInsecurePassword()
        {
            return SecureStringHelper.ToInsecureString(_password);
        }
    }
}
