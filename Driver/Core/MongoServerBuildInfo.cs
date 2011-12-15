/* Copyright 2010-2011 10gen Inc.
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
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents build info about a server instance.
    /// </summary>
    public class MongoServerBuildInfo
    {
        // private fields
        private int bits;
        private string gitVersion;
        private string sysInfo;
        private Version version;
        private string versionString;

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
            this.bits = bits;
            this.gitVersion = gitVersion;
            this.sysInfo = sysInfo;
            this.version = ParseVersion(versionString);
            this.versionString = versionString;
        }

        // public properties
        /// <summary>
        /// Gets the number of bits (32 or 64).
        /// </summary>
        public int Bits
        {
            get { return bits; }
        }

        /// <summary>
        /// Gets the GIT version.
        /// </summary>
        public string GitVersion
        {
            get { return gitVersion; }
        }

        /// <summary>
        /// Gets the sysInfo.
        /// </summary>
        public string SysInfo
        {
            get { return sysInfo; }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public Version Version
        {
            get { return version; }
        }

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public string VersionString
        {
            get { return versionString; }
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
    }
}
