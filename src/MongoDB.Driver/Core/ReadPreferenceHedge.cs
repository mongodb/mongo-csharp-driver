/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a read preference hedge.
    /// </summary>
    public sealed class ReadPreferenceHedge : IEquatable<ReadPreferenceHedge>
    {
        #region static
        // private static fields
        private static readonly ReadPreferenceHedge __disabled = new ReadPreferenceHedge(isEnabled: false);
        private static readonly ReadPreferenceHedge __enabled = new ReadPreferenceHedge(isEnabled: true);

        // public static properties
        /// <summary>
        /// Gets a disabled read preference hedge.
        /// </summary>
        public static ReadPreferenceHedge Disabled => __disabled;

        /// <summary>
        /// Gets an enabled read preference hedge.
        /// </summary>
        public static ReadPreferenceHedge Enabled => __enabled;
        #endregion

        // private fields
        private readonly bool _isEnabled;

        // constructors
        /// <summary>
        /// Initializes an instance of ReadPreferenceHedge.
        /// </summary>
        /// <param name="isEnabled">Whether hedged reads are enabled.</param>
        public ReadPreferenceHedge(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        // public properties
        /// <summary>
        /// Gets whether hedged reads are enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;

        // public methods
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReadPreferenceHedge);
        }

        /// <inheritdoc/>
        public bool Equals(ReadPreferenceHedge other)
        {
            return other != null && _isEnabled == other._isEnabled;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _isEnabled.GetHashCode();
        }

        /// <summary>
        /// Converts the read preference hedge to a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public BsonDocument ToBsonDocument()
        {
            return new BsonDocument("enabled", _isEnabled);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToBsonDocument().ToJson();
        }
    }
}
