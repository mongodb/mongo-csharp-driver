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

namespace MongoDB.Driver.Core.Misc
{
    internal static class WireVersion
    {
        /// <summary>
        /// Wire version 0.
        /// </summary>
        public const int Zero = 0;
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
        /// <summary>
        /// Wire version 15.
        /// </summary>
        public const int Server52 = 15;
        /// <summary>
        /// Wire version 16.
        /// </summary>
        public const int Server53 = 16;
        /// <summary>
        /// Wire version 17.
        /// </summary>
        public const int Server60 = 17;
        /// <summary>
        /// Wire version 18.
        /// </summary>
        public const int Server61 = 18;
        /// <summary>
        /// Wire version 19.
        /// </summary>
        public const int Server62 = 19;
        /// <summary>
        /// Wire version 20.
        /// </summary>
        public const int Server63 = 20;
        /// <summary>
        /// Wire version 21.
        /// </summary>
        public const int Server70 = 21;
        /// <summary>
        /// Wire version 22.
        /// </summary>
        public const int Server71 = 22;
        /// <summary>
        /// Wire version 23.
        /// </summary>
        public const int Server72 = 23;
        /// <summary>
        /// Wire version 24.
        /// </summary>
        public const int Server73 = 24;
        /// <summary>
        /// Wire version 25.
        /// </summary>
        public const int Server80 = 25;

        // note: keep WireVersion.cs and ServerVersion.cs in sync

        #region static
        private static readonly List<WireVersionInfo> __knownWireVersions = new()
        {
            // 1. Make sure that wireVersion value matches to the item index.
            // 2. The below list contains all wire versions ever existed.
            // 3. Wire versions less than 6 are not supported anymore.
            // Wire versions 10-12 were only used in pre-release versions and will never be encountered in released versions of the server.
            // They aren't necessary to be included here but are included for completeness
            new WireVersionInfo(wireVersion: 0, major: 0, minor: 0),
            new WireVersionInfo(wireVersion: 1, major: 0, minor: 0),
            new WireVersionInfo(wireVersion: 2, major: 2, minor: 6),
            new WireVersionInfo(wireVersion: 3, major: 3, minor: 0),
            new WireVersionInfo(wireVersion: 4, major: 3, minor: 2),
            new WireVersionInfo(wireVersion: 5, major: 3, minor: 4),
            new WireVersionInfo(wireVersion: 6, major: 3, minor: 6),
            new WireVersionInfo(wireVersion: 7, major: 4, minor: 0),
            new WireVersionInfo(wireVersion: 8, major: 4, minor: 2),
            new WireVersionInfo(wireVersion: 9, major: 4, minor: 4),
            new WireVersionInfo(wireVersion: 10, major: 4, minor: 7),
            new WireVersionInfo(wireVersion: 11, major: 4, minor: 8),
            new WireVersionInfo(wireVersion: 12, major: 4, minor: 9),
            new WireVersionInfo(wireVersion: 13, major: 5, minor: 0),
            new WireVersionInfo(wireVersion: 14, major: 5, minor: 1),
            new WireVersionInfo(wireVersion: 15, major: 5, minor: 2),
            new WireVersionInfo(wireVersion: 16, major: 5, minor: 3),
            new WireVersionInfo(wireVersion: 17, major: 6, minor: 0),
            new WireVersionInfo(wireVersion: 18, major: 6, minor: 1),
            new WireVersionInfo(wireVersion: 19, major: 6, minor: 2),
            new WireVersionInfo(wireVersion: 20, major: 6, minor: 3),
            new WireVersionInfo(wireVersion: 21, major: 7, minor: 0),
            new WireVersionInfo(wireVersion: 22, major: 7, minor: 1),
            new WireVersionInfo(wireVersion: 23, major: 7, minor: 2),
            new WireVersionInfo(wireVersion: 24, major: 7, minor: 3),
            new WireVersionInfo(wireVersion: 25, major: 8, minor: 0),
        };

        private static Range<int> __supportedWireVersionRange = CreateSupportedWireVersionRange(minWireVersion: Server40, maxWireVersion: Server80);

        private static Range<int> CreateSupportedWireVersionRange(int minWireVersion, int maxWireVersion)
        {
            if (!__knownWireVersions.Exists(w => w.WireVersion == minWireVersion) ||
                !__knownWireVersions.Exists(w => w.WireVersion == maxWireVersion))
            {
                throw new ApplicationException("Min or Max supported wire version is invalid.");
            }

            return new Range<int>(minWireVersion, maxWireVersion);
        }

        // public static properties
        public static Range<int> SupportedWireVersionRange => __supportedWireVersionRange;

        // public static methods
        public static string GetServerVersionForErrorMessage(int wireVersion)
        {
            var approximateServerVersion = WireVersion.ToServerVersion(wireVersion);
            return approximateServerVersion != null
                ? $"{approximateServerVersion.Major}.{approximateServerVersion.Minor}"
                : $"Unknown (wire version {wireVersion})";
        }

        public static SemanticVersion ToServerVersion(int wireVersion)
        {
            Ensure.IsGreaterThanOrEqualToZero(wireVersion, nameof(wireVersion));

            if (wireVersion > __knownWireVersions.Count - 1)
            {
                return null;
            }

            return __knownWireVersions[wireVersion].FirstSupportedServerVersion;
        }
        #endregion

        private class WireVersionInfo
        {
            public WireVersionInfo(int wireVersion, int major, int minor)
            {
                WireVersion = wireVersion;
                FirstSupportedServerVersion = new SemanticVersion(major, minor, 0);
            }

            public int WireVersion { get; }
            public SemanticVersion FirstSupportedServerVersion { get; }
        }
    }
}
