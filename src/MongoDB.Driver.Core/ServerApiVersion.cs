/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a server API version.
    /// </summary>
    public sealed class ServerApiVersion : IEquatable<ServerApiVersion>
    {
        #region static
        // static fields
        private static readonly ServerApiVersion __v1 = new ServerApiVersion("1");

        // static properties
        /// <summary>
        /// Gets an instance of server API version 1.
        /// </summary>
        public static ServerApiVersion V1 => __v1;
        #endregion

        // private fields
        private readonly string _versionString;

        // constructors
        internal ServerApiVersion(string versionString)
        {
            _versionString = Ensure.IsNotNull(versionString, nameof(versionString));
        }

        // operators
        /// <inheritdoc/>
        public static bool operator !=(ServerApiVersion lhs, ServerApiVersion rhs)
        {
            return !(lhs == rhs);
        }

        /// <inheritdoc/>
        public static bool operator ==(ServerApiVersion lhs, ServerApiVersion rhs)
        {
            return object.Equals(lhs, rhs);
        }

        // methods
        /// <inheritdoc/>
        public bool Equals(ServerApiVersion other)
        {
            if (other == null)
            {
                return false;
            }

            return _versionString == other._versionString;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ServerApiVersion);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_versionString)
                .GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _versionString;
        }
    }
}
