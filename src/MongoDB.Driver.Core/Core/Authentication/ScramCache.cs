/* Copyright 2019–present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// A cache for Client and Server keys, to be used during authentication.
    /// </summary>
    internal class ScramCache
    {
        private ScramCacheKey _cacheKey;
        private ScramCacheEntry _cachedEntry;

        /// <summary>
        /// Try to get a cached entry.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entry">The entry.</param>
        /// <returns>True if the cache contained an entry for the key.</returns>
        public bool TryGet(ScramCacheKey key, out ScramCacheEntry entry)
        {
            if (key.Equals(_cacheKey))
            {
                entry = _cachedEntry;
                return true;
            }
            else
            {
                entry = null;
                return false;
            }
        }

        /// <summary>
        /// Add a cached entry.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="entry">The entry.</param>
        public void Add(ScramCacheKey key, ScramCacheEntry entry)
        {
            _cacheKey = key;
            _cachedEntry = entry;
        }
    }

    internal class ScramCacheKey
    {
        private int _iterationCount;
        private SecureString _password;
        private byte[] _salt;

        internal ScramCacheKey(SecureString password, byte[] salt, int iterationCount)
        {
            _iterationCount = iterationCount;
            _password = password;
            _salt = salt;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null || obj.GetType() != obj.GetType())
            {
                return false;
            }

            ScramCacheKey other = (ScramCacheKey)obj;

            return
                Equals(_password, other._password) &&
                _iterationCount == other._iterationCount &&
                _salt.SequenceEqual(other._salt);
        }

        public override int GetHashCode()
        {
            // ignore _password when computing the hash code
            return new Hasher()
                .Hash(_iterationCount)
                .Hash(_salt)
                .GetHashCode();
        }

        // private methods
        private bool Equals(SecureString x, SecureString y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;

            }
            using (var dx = new DecryptedSecureString(x))
            using (var dy = new DecryptedSecureString(y))
            {
                var xchars = dx.GetChars();
                var ychars = dy.GetChars();
                return Equals(xchars, ychars);
            }
        }

        private bool Equals(char[] x, char[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

    }

    internal class ScramCacheEntry
    {
        private byte[] _clientKey;
        private byte[] _serverKey;

        public ScramCacheEntry(byte[] clientKey, byte[] serverKey)
        {
            _clientKey = clientKey;
            _serverKey = serverKey;
        }

        public byte[] ClientKey => _clientKey;

        public byte[] ServerKey => _serverKey;
    }
}
