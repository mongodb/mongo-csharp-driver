/* Copyright 2010-2013 10gen Inc.
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
using System.Text.RegularExpressions;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents build info about a server instance.
    /// </summary>
    public class MongoServerBuildInfo
    {
        // private fields
        private int _bits;
        private string _gitVersion;
        private string _sysInfo;
        private Version _version;
        private string _versionString;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoServerBuildInfo.
        /// </summary>
        /// <param name="bits">The number of bits (32 or 64).</param>
        /// <param name="gitVersion">The GIT version.</param>
        /// <param name="sysInfo">The sysInfo.</param>
        /// <param name="versionString">The version string.</param>
        public MongoServerBuildInfo(int bits, string gitVersion, string sysInfo, string versionString)
        {
            _bits = bits;
            _gitVersion = gitVersion;
            _sysInfo = sysInfo;
            _version = ParseVersion(versionString);
            _versionString = versionString;
        }

        // public properties
        /// <summary>
        /// Gets the number of bits (32 or 64).
        /// </summary>
        public int Bits
        {
            get { return _bits; }
        }

        /// <summary>
        /// Gets the GIT version.
        /// </summary>
        public string GitVersion
        {
            get { return _gitVersion; }
        }

        /// <summary>
        /// Gets the sysInfo.
        /// </summary>
        public string SysInfo
        {
            get { return _sysInfo; }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public string VersionString
        {
            get { return _versionString; }
        }

        // private methods
        private Version ParseVersion(string versionString)
        {
            var match = Regex.Match(versionString, @"^(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(\.(?<revision>\d+))?(-.*)?$");
            if (match.Success)
            {
                var majorString = match.Groups["major"].Value;
                var minorString = match.Groups["minor"].Value;
                var buildString = match.Groups["build"].Value;
                var revisionString = match.Groups["revision"].Value;
                if (revisionString == "") { revisionString = "0"; }
                int major, minor, build, revision;
                if (int.TryParse(majorString, out major) &&
                    int.TryParse(minorString, out minor) &&
                    int.TryParse(buildString, out build) &&
                    int.TryParse(revisionString, out revision))
                {
                    return new Version(major, minor, build, revision);
                }
            }
            return new Version(0, 0, 0, 0);
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of MongoServerBuildInfo initialized from the result of a buildinfo command.
        /// </summary>
        /// <param name="result">A CommandResult.</param>
        /// <returns>A MongoServerBuildInfo.</returns>
        public static MongoServerBuildInfo FromCommandResult(CommandResult result)
        {
            var document = result.Response;
            return new MongoServerBuildInfo(
                document["bits"].ToInt32(),
                document["gitVersion"].AsString,
                document["sysInfo"].AsString,
                document["version"].AsString
            );
        }
    }
}
