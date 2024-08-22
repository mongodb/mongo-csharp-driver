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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents server versions.
    /// </summary>
    /// <note>Only major/minor versions are represented.</note>
    public enum ServerVersion
    {
        /// <summary>
        /// Server version 2.6.
        /// </summary>
        Server26,

        /// <summary>
        /// Server version 3.0.
        /// </summary>
        Server30,

        /// <summary>
        /// Server version 3.2.
        /// </summary>
        Server32,

        /// <summary>
        /// Server version 2.6.
        /// </summary>
        Server34,

        /// <summary>
        /// Server version 3.6.
        /// </summary>
        Server36,

        /// <summary>
        /// Server version 4.0.
        /// </summary>
        Server40,

        /// <summary>
        /// Server version 4.2.
        /// </summary>
        Server42,

        /// <summary>
        /// Server version 4.4.
        /// </summary>
        Server44,

        /// <summary>
        /// Server version 4.7.
        /// </summary>
        Server47,

        /// <summary>
        /// Server version 4.8.
        /// </summary>
        Server48,

        /// <summary>
        /// Server version 4.9.
        /// </summary>
        Server49,

        /// <summary>
        /// Server version 5.0.
        /// </summary>
        Server50,

        /// <summary>
        /// Server version 5.1.
        /// </summary>
        Server51,

        /// <summary>
        /// Server version 5.2.
        /// </summary>
        Server52,

        /// <summary>
        /// Server version 5.3.
        /// </summary>
        Server53,

        /// <summary>
        /// Server version 6.0.
        /// </summary>
        Server60,

        /// <summary>
        /// Server version 6.1.
        /// </summary>
        Server61,

        /// <summary>
        /// Server version 6.2.
        /// </summary>
        Server62,

        /// <summary>
        /// Server version 6.3.
        /// </summary>
        Server63,

        /// <summary>
        /// Server version 7.0.
        /// </summary>
        Server70,

        /// <summary>
        /// Server version 7.1.
        /// </summary>
        Server71,

        /// <summary>
        /// Server version 7.2.
        /// </summary>
        Server72,

        /// <summary>
        /// Server version 7.3.
        /// </summary>
        Server73,

        /// <summary>
        /// Server version 8.0.
        /// </summary>
        Server80

        // note: keep Server.cs and WireVersion.cs in sync as well as the extension methods below
    }

    internal static class ServerVersionExtensions
    {
        public static ServerVersion ToServerVersion(this int wireVersion)
        {
            return wireVersion switch
            {
                WireVersion.Server26 => ServerVersion.Server26,
                WireVersion.Server30 => ServerVersion.Server30,
                WireVersion.Server32 => ServerVersion.Server32,
                WireVersion.Server34 => ServerVersion.Server34,
                WireVersion.Server36 => ServerVersion.Server36,
                WireVersion.Server40 => ServerVersion.Server40,
                WireVersion.Server42 => ServerVersion.Server42,
                WireVersion.Server44 => ServerVersion.Server44,
                WireVersion.Server47 => ServerVersion.Server47,
                WireVersion.Server48 => ServerVersion.Server48,
                WireVersion.Server49 => ServerVersion.Server49,
                WireVersion.Server50 => ServerVersion.Server50,
                WireVersion.Server51 => ServerVersion.Server51,
                WireVersion.Server52 => ServerVersion.Server52,
                WireVersion.Server53 => ServerVersion.Server53,
                WireVersion.Server60 => ServerVersion.Server60,
                WireVersion.Server61 => ServerVersion.Server61,
                WireVersion.Server62 => ServerVersion.Server62,
                WireVersion.Server63 => ServerVersion.Server63,
                WireVersion.Server70 => ServerVersion.Server70,
                WireVersion.Server71 => ServerVersion.Server71,
                WireVersion.Server72 => ServerVersion.Server72,
                WireVersion.Server73 => ServerVersion.Server73,
                WireVersion.Server80 => ServerVersion.Server80,
                _ => throw new ArgumentException($"Invalid write version: {wireVersion}.", nameof(wireVersion))
            };
        }

        public static int ToWireVersion(this ServerVersion? serverVersion)
        {
            return serverVersion switch
            {
                null => WireVersion.Server40,
                ServerVersion.Server26 => WireVersion.Server26,
                ServerVersion.Server30 => WireVersion.Server30,
                ServerVersion.Server32 => WireVersion.Server32,
                ServerVersion.Server34 => WireVersion.Server34,
                ServerVersion.Server36 => WireVersion.Server36,
                ServerVersion.Server40 => WireVersion.Server40,
                ServerVersion.Server42 => WireVersion.Server42,
                ServerVersion.Server44 => WireVersion.Server44,
                ServerVersion.Server47 => WireVersion.Server47,
                ServerVersion.Server48 => WireVersion.Server48,
                ServerVersion.Server49 => WireVersion.Server49,
                ServerVersion.Server50 => WireVersion.Server50,
                ServerVersion.Server51 => WireVersion.Server51,
                ServerVersion.Server52 => WireVersion.Server52,
                ServerVersion.Server53 => WireVersion.Server53,
                ServerVersion.Server60 => WireVersion.Server60,
                ServerVersion.Server61 => WireVersion.Server61,
                ServerVersion.Server62 => WireVersion.Server62,
                ServerVersion.Server63 => WireVersion.Server63,
                ServerVersion.Server70 => WireVersion.Server70,
                ServerVersion.Server71 => WireVersion.Server71,
                ServerVersion.Server72 => WireVersion.Server72,
                ServerVersion.Server73 => WireVersion.Server73,
                ServerVersion.Server80 => WireVersion.Server80,
                _ => throw new ArgumentException($"Invalid server version: {serverVersion}.", nameof(serverVersion))
            };
        }
    }
}
