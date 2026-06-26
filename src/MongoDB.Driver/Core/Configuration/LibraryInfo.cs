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
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents information about a library using the .NET driver.
    /// </summary>
    public sealed class LibraryInfo : IEquatable<LibraryInfo>
    {
        /// <summary>
        /// Gets the library name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the library version.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the library platform. This is a free-form description of the runtime the library targets;
        /// it is appended to the platform information (for example, the .NET runtime description) reported in
        /// the connection handshake. It is not the operating system, which the handshake reports separately.
        /// </summary>
        public string Platform { get; }

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryInfo"/> class.
        /// </summary>
        /// <param name="name">The library name.</param>
        /// <param name="version">The library version.</param>
        public LibraryInfo(string name, string version = default)
            : this(name, version, platform: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryInfo"/> class.
        /// </summary>
        /// <param name="name">The library name.</param>
        /// <param name="version">The library version.</param>
        /// <param name="platform">The library platform.</param>
        public LibraryInfo(string name, string version, string platform)
        {
            Name = EnsureNoSeparator(Ensure.IsNotNullOrEmpty(name, nameof(name)), nameof(name));
            Version = NormalizeOptionalValue(version, nameof(version));
            Platform = NormalizeOptionalValue(platform, nameof(platform));
        }

        // public operators
        /// <summary>
        /// Determines whether two <see cref="LibraryInfo"/> instances are equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(LibraryInfo lhs, LibraryInfo rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        /// <summary>
        /// Determines whether two <see cref="LibraryInfo"/> instances are not equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is not equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(LibraryInfo lhs, LibraryInfo rhs)
        {
            return !(lhs == rhs);
        }

        // public methods
        /// <summary>
        /// Determines whether the specified <see cref="LibraryInfo" /> is equal to this instance.
        /// </summary>
        /// <param name="rhs">The <see cref="LibraryInfo" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="LibraryInfo" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(LibraryInfo rhs)
        {
            return
                rhs != null &&
                Name == rhs.Name &&
                Version == rhs.Version &&
                Platform == rhs.Platform;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as LibraryInfo);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            new Hasher()
                .Hash(Name)
                .Hash(Version)
                .Hash(Platform)
                .GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => Platform == null ? $"{Name}-{Version}" : $"{Name}-{Version}-{Platform}";

        // private methods
        private static string EnsureNoSeparator(string value, string paramName)
        {
            if (value != null && value.Contains("|"))
            {
                throw new ArgumentException("LibraryInfo values must not contain the '|' character.", paramName);
            }

            return value;
        }

        // empty or whitespace optional values are treated as unset and normalized to null
        private static string NormalizeOptionalValue(string value, string paramName)
        {
            EnsureNoSeparator(value, paramName);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
