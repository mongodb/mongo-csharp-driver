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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Core.Misc
{
    internal class WireVersion
    {
        #region static
        private static List<WireVersion> __wireVersionSemanticVersionsSet = new()
        {
            new WireVersion(maxWireVersion: 0, major: 0, minor: 0),
            new WireVersion(maxWireVersion: 1, major: 0, minor: 0),
            new WireVersion(maxWireVersion: 2, major: 2, minor: 6),
            new WireVersion(maxWireVersion: 3, major: 3, minor: 0),
            new WireVersion(maxWireVersion: 4, major: 3, minor: 2),
            new WireVersion(maxWireVersion: 5, major: 3, minor: 4),
            new WireVersion(maxWireVersion: 6, major: 3, minor: 6),
            new WireVersion(maxWireVersion: 7, major: 4, minor: 0),
            new WireVersion(maxWireVersion: 8, major: 4, minor: 2),
            new WireVersion(maxWireVersion: 9, major: 4, minor: 4),
            new WireVersion(maxWireVersion: 10, major: 4, minor: 7),
            new WireVersion(maxWireVersion: 11, major: 4, minor: 8),
            new WireVersion(maxWireVersion: 12, major: 4, minor: 9),
            new WireVersion(maxWireVersion: 13, major: 5, minor: 0),
            new WireVersion(maxWireVersion: 14, major: 5, minor: 1),
        };
        private static Range<int> __supportedWireRange = new Range<int>(6, 14);

        // public static properties
        public static Range<int> SupportedWireRange
        {
            get
            {
                Ensure.That(
                    __wireVersionSemanticVersionsSet.Exists(w => __supportedWireRange.Min == w.MaxWireVersion) &&
                    __wireVersionSemanticVersionsSet.Exists(w => __supportedWireRange.Max == w.MaxWireVersion),
                    "Incorrect supported wire range configuration."); // should not be reached

                return __supportedWireRange;
            }
        }

        public static SemanticVersion FirstSupportedServerVersion
        {
            get
            {
                return __wireVersionSemanticVersionsSet[SupportedWireRange.Min].FirstSupportedVersion;
            }
        }

        /// <summary>
        /// Mongo server version 0.
        /// </summary>
        public static WireVersion Zero => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 0);

        /// <summary>
        /// Mongo server version 3.4.
        /// </summary>
        public static WireVersion Five => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 5);
        /// <summary>
        /// Mongo server version 3.6.
        /// </summary>
        public static WireVersion Six => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 6);
        /// <summary>
        /// Mongo server version 4.0.
        /// </summary>
        public static WireVersion Seven => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 7);
        /// <summary>
        /// Mongo server version 4.2.
        /// </summary>
        public static WireVersion Eight => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 8);
        /// <summary>
        /// Mongo server version 4.4.
        /// </summary>
        public static WireVersion Nine => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 9);
        /// <summary>
        /// Mongo server version 4.7.
        /// </summary>
        public static WireVersion Ten => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 10);
        /// <summary>
        /// Mongo server version 4.8.
        /// </summary>
        public static WireVersion Eleven => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 11);
        /// <summary>
        /// Mongo server version 4.9.
        /// </summary>
        public static WireVersion Twelve => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 12);
        /// <summary>
        /// Mongo server version 5.0.
        /// </summary>
        public static WireVersion Thirteen => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 13);
        /// <summary>
        /// Mongo server version 5.1.
        /// </summary>
        public static WireVersion Fourteen => __wireVersionSemanticVersionsSet.First(w => w.MaxWireVersion == 14);

        // public static methods
        public static WireVersion GetWireVersion(int maxWireVersion)
        {
            return __wireVersionSemanticVersionsSet.FirstOrDefault(w => w.MaxWireVersion == maxWireVersion)
                    // take the last supported wire protocol and rely on server selecting compatibility check
                    ?? __wireVersionSemanticVersionsSet.Last();
        }

        public static WireVersion GetWireVersion(Range<int> wireVersionRange)
        {
            return GetWireVersion(wireVersionRange.Max);
        }

        public static WireVersion GetWireVersion(SemanticVersion semanticVersion)
        {
            return __wireVersionSemanticVersionsSet.Last(w => w.FirstSupportedVersion <= semanticVersion);
        }

        public static void ThrowNotSupportedException(Range<int> wireVersionRange, string featureName)
        {
            var emulateServerVersion = WireVersion.GetWireVersion(wireVersionRange).FirstSupportedVersion;
            throw new NotSupportedException($"Server with reported max wire version {wireVersionRange.Max} (Supported starting from MongoDB {emulateServerVersion.Major}.{emulateServerVersion.Minor}) does not support the {featureName} feature.");
        }
        #endregion

        private readonly int _maxWireVersion;
        private readonly SemanticVersion _firstSupportedServerVersion;

        public WireVersion(int maxWireVersion, int major, int minor)
        {
            _firstSupportedServerVersion = new SemanticVersion(
                major: Ensure.IsGreaterThanOrEqualToZero(major, nameof(major)),
                minor: Ensure.IsGreaterThanOrEqualToZero(minor, nameof(minor)),
                patch: 0);
            _maxWireVersion = Ensure.IsGreaterThanOrEqualToZero(maxWireVersion, nameof(maxWireVersion));
        }

        public SemanticVersion FirstSupportedVersion => _firstSupportedServerVersion;
        public int MaxWireVersion => _maxWireVersion;
    }
}
