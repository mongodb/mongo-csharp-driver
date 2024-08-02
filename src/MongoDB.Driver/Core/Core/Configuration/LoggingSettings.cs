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
using Microsoft.Extensions.Logging;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents the settings for logging.
    /// </summary>
    public sealed class LoggingSettings : IEquatable<LoggingSettings>
    {
        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        [CLSCompliant(false)]
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the maximum document size in chars.
        /// </summary>
        public int MaxDocumentSize { get; }

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingSettings"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="maxDocumentSize">The maximum document size in chars.</param>
        [CLSCompliant(false)]
        public LoggingSettings(
            ILoggerFactory loggerFactory = default,
            Optional<int> maxDocumentSize = default)
        {
            LoggerFactory = loggerFactory;
            MaxDocumentSize = maxDocumentSize.WithDefault(MongoInternalDefaults.Logging.MaxDocumentSize);
        }

        // public operators
        /// <summary>
        /// Determines whether two <see cref="LoggingSettings"/> instances are equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(LoggingSettings lhs, LoggingSettings rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        /// <summary>
        /// Determines whether two <see cref="LoggingSettings"/> instances are not equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is not equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(LoggingSettings lhs, LoggingSettings rhs)
        {
            return !(lhs == rhs);
        }

        // public methods
        /// <summary>
        /// Determines whether the specified <see cref="LoggingSettings" /> is equal to this instance.
        /// </summary>
        /// <param name="rhs">The <see cref="LoggingSettings" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="LoggingSettings" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(LoggingSettings rhs)
        {
            return
                rhs != null &&
                LoggerFactory == rhs.LoggerFactory &&
                MaxDocumentSize == rhs.MaxDocumentSize;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as LoggingSettings);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
    }
}
