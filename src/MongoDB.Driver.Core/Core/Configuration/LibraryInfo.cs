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

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryInfo"/> class.
        /// </summary>
        /// <param name="name">The library name.</param>
        /// <param name="version">The library version.</param>
        public LibraryInfo(string name, string version = default)
        {
            Name = Ensure.IsNotNullOrEmpty(name, nameof(name));
            Version = version;
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
                Version == rhs.Version;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as LibraryInfo);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"{Name}-{Version}";
    }
}
