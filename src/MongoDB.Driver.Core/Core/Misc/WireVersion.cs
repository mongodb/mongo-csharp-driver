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
        /// <summary>
        /// Wire version 0.
        /// </summary>
        public const int ServerBefore26 = 0;
        /// <summary>
        /// Wire version 2.
        /// </summary>
        public const int Server26 = 2;
        /// <summary>
        /// Wire version 3.
        /// </summary>
        public const int Server30 = 3;
        /// <summary>
        /// Wire version 4.
        /// </summary>
        public const int Server32 = 4;
        /// <summary>
        /// Wire version 5.
        /// </summary>
        public const int Server34 = 5;
        /// <summary>
        /// Wire version 6.
        /// </summary>
        public const int Server36 = 6;
        /// <summary>
        /// Wire version 7.
        /// </summary>
        public const int Server40 = 7;
        /// <summary>
        /// Wire version 8.
        /// </summary>
        public const int Server42 = 8;
        /// <summary>
        /// Wire version 9.
        /// </summary>
        public const int Server44 = 9;
        /// <summary>
        /// Wire version 10.
        /// </summary>
        public const int Server47 = 10;
        /// <summary>
        /// Wire version 11.
        /// </summary>
        public const int Server48 = 11;
        /// <summary>
        /// Wire version 12.
        /// </summary>
        public const int Server49 = 12;
        /// <summary>
        /// Wire version 13.
        /// </summary>
        public const int Server50 = 13;
        /// <summary>
        /// Wire version 14.
        /// </summary>
        public const int Server51 = 14;

        #region static
        private static List<WireVersionInfo> __wireVersionSemanticVersionsSet = new()
        {
            new WireVersionInfo(maxWireVersion: 0, major: 0, minor: 0),
            new WireVersionInfo(maxWireVersion: 1, major: 0, minor: 0),
            new WireVersionInfo(maxWireVersion: 2, major: 2, minor: 6),
            new WireVersionInfo(maxWireVersion: 3, major: 3, minor: 0),
            new WireVersionInfo(maxWireVersion: 4, major: 3, minor: 2),
            new WireVersionInfo(maxWireVersion: 5, major: 3, minor: 4),
            new WireVersionInfo(maxWireVersion: 6, major: 3, minor: 6),
            new WireVersionInfo(maxWireVersion: 7, major: 4, minor: 0),
            new WireVersionInfo(maxWireVersion: 8, major: 4, minor: 2),
            new WireVersionInfo(maxWireVersion: 9, major: 4, minor: 4),
            new WireVersionInfo(maxWireVersion: 10, major: 4, minor: 7),
            new WireVersionInfo(maxWireVersion: 11, major: 4, minor: 8),
            new WireVersionInfo(maxWireVersion: 12, major: 4, minor: 9),
            new WireVersionInfo(maxWireVersion: 13, major: 5, minor: 0),
            new WireVersionInfo(maxWireVersion: 14, major: 5, minor: 1),
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
                return __wireVersionSemanticVersionsSet[SupportedWireRange.Min].FirstSupportedServerVersion;
            }
        }

        // public static methods
        public static SemanticVersion ToServerVersion(int maxWireVersion)
        {
            var wireVersionInfo = __wireVersionSemanticVersionsSet.FirstOrDefault(w => w.MaxWireVersion == maxWireVersion)
                    // take the last supported wire protocol and rely on server selecting compatibility check
                    ?? __wireVersionSemanticVersionsSet.Last();
            return wireVersionInfo.FirstSupportedServerVersion;
        }

        public static int ToWireVersion(SemanticVersion semanticVersion)
        {
            return __wireVersionSemanticVersionsSet.Last(w => w.FirstSupportedServerVersion <= semanticVersion).MaxWireVersion;
        }
        #endregion

        private class WireVersionInfo
        {
            public WireVersionInfo(int maxWireVersion, int major, int minor)
            {
                MaxWireVersion = maxWireVersion;
                FirstSupportedServerVersion = new SemanticVersion(major, minor, 0);
            }

            public int MaxWireVersion { get; }
            public SemanticVersion FirstSupportedServerVersion { get; }
        }
    }
}
