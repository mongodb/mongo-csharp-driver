/* Copyright 2010-2015 MongoDB Inc.
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
        private Version _version;
        private string _versionString;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoServerBuildInfo.
        /// </summary>
        /// <param name="versionString">The version string.</param>
        public MongoServerBuildInfo(string versionString)
        {
            _version = ParseVersion(versionString);
            _versionString = versionString;
        }

        // public properties
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
            var match = Regex.Match(versionString, @"^(?<version>\d+\.\d+(\.\d+(\.\d+)?)?)");
            if (match.Success)
            {
                var version = match.Groups["version"].Value;
                return new Version(version);
            }
            return new Version(0, 0, 0);
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
            return new MongoServerBuildInfo(document["version"].AsString);
        }
    }
}
