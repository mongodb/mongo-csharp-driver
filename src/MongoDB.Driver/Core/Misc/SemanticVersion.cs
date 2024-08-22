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
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Represents a semantic version number.
    /// </summary>
    public class SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
    {
        #region static
        private static void LookForPreReleaseNumericSuffix(string preRelease, out string preReleasePrefix, out int? preReleaseNumericSuffix)
        {
            var pattern = @"^(?<prefix>[^\d]+)(?<numericSuffix>\d+)$";
            var match = Regex.Match(preRelease, pattern);
            if (match.Success)
            {
                preReleasePrefix = match.Groups["prefix"].Value;
                preReleaseNumericSuffix = int.Parse(match.Groups["numericSuffix"].Value);
            }
            else
            {
                preReleasePrefix = preRelease;
                preReleaseNumericSuffix = null;
            }
        }

        private static bool TryParseInternalPreRelease(string preReleaseIn, out string preReleaseOut, out int? commitsAfterRelease, out string commitHash)
        {
            if (preReleaseIn != null)
            {
                var internalBuildPattern = @"^((?<preRelease>.+)-)?(?<commitsAfterRelease>\d+)-g(?<commitHash>[0-9a-fA-F]{4,40})$";
                var match = Regex.Match(preReleaseIn, internalBuildPattern);
                if (match.Success)
                {
                    var preReleaseGroup = match.Groups["preRelease"];
                    preReleaseOut = preReleaseGroup.Success ? preReleaseGroup.Value : null;
                    commitsAfterRelease = int.Parse(match.Groups["commitsAfterRelease"].Value);
                    commitHash = match.Groups["commitHash"].Value;
                    return true;
                }
            }

            preReleaseOut = preReleaseIn;
            commitsAfterRelease = null;
            commitHash = null;
            return false;
        }
        #endregion

        // fields
        private readonly string _commitHash;
        private readonly int? _commitsAfterRelease;
        private readonly int _major;
        private readonly int _minor;
        private readonly int _patch;
        private readonly string _preRelease;
        private readonly int? _preReleaseNumericSuffix;
        private readonly string _preReleasePrefix;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        public SemanticVersion(int major, int minor, int patch)
            : this(major, minor, patch, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="preRelease">The pre release version.</param>
        public SemanticVersion(int major, int minor, int patch, string preRelease)
        {
            _major = Ensure.IsGreaterThanOrEqualToZero(major, nameof(major));
            _minor = Ensure.IsGreaterThanOrEqualToZero(minor, nameof(minor));
            _patch = Ensure.IsGreaterThanOrEqualToZero(patch, nameof(patch));

            if (TryParseInternalPreRelease(preRelease, out var preReleaseOut, out var commitsAfterRelease, out var commitHash))
            {
                _preRelease = preReleaseOut; // can be null
                _commitsAfterRelease = commitsAfterRelease;
                _commitHash = commitHash;
            }
            else
            {
                _preRelease = preRelease; // can be null
                _commitsAfterRelease = null;
                _commitHash = null;
            }

            if (_preRelease != null)
            {
                LookForPreReleaseNumericSuffix(_preRelease, out _preReleasePrefix, out _preReleaseNumericSuffix);
            }
        }

        // properties
        /// <summary>
        /// Gets the internal build commit hash.
        /// </summary>
        public string CommitHash
        {
            get { return _commitHash; }
        }

        /// <summary>
        /// Gets the number of commits after release.
        /// </summary>
        public int? CommitsAfterRelease
        {
            get { return _commitsAfterRelease; }
        }

        /// <summary>
        /// Gets the major version.
        /// </summary>
        /// <value>
        /// The major version.
        /// </value>
        public int Major
        {
            get { return _major; }
        }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        /// <value>
        /// The minor version.
        /// </value>
        public int Minor
        {
            get { return _minor; }
        }

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        /// <value>
        /// The patch version.
        /// </value>
        public int Patch
        {
            get { return _patch; }
        }

        /// <summary>
        /// Gets the pre release version.
        /// </summary>
        /// <value>
        /// The pre release version.
        /// </value>
        public string PreRelease
        {
            get { return _preRelease; }
        }

        // methods
        /// <inheritdoc/>
        public int CompareTo(SemanticVersion other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }

            var result = _major.CompareTo(other._major);
            if (result != 0)
            {
                return result;
            }

            result = _minor.CompareTo(other._minor);
            if (result != 0)
            {
                return result;
            }

            result = _patch.CompareTo(other._patch);
            if (result != 0)
            {
                return result;
            }

            result = ComparePreReleases();
            if (result != 0)
            {
                return result;
            }

            result = CompareCommitsAfterRelease();
            if (result != 0)
            {
                return result;
            }

            // ignore _commitHash for comparison purposes
            return 0;

            int ComparePreReleases()
            {
                if (_preRelease == null && other._preRelease == null)
                {
                    return 0;
                }
                else if (_preRelease == null)
                {
                    return 1;
                }
                else if (other._preRelease == null)
                {
                    return -1;
                }

                result = _preReleasePrefix.CompareTo(other._preReleasePrefix);
                if (result != 0)
                {
                    return result;
                }

                if (_preReleaseNumericSuffix == null && other._preReleaseNumericSuffix == null)
                {
                    return 0;
                }
                else if (_preReleaseNumericSuffix == null)
                {
                    return -1;
                }
                else if (other._preReleaseNumericSuffix == null)
                {
                    return 1;
                }

                return _preReleaseNumericSuffix.Value.CompareTo(other._preReleaseNumericSuffix.Value);
            }

            int CompareCommitsAfterRelease()
            {
                if (_commitsAfterRelease == null && other._commitsAfterRelease == null)
                {
                    return 0;
                }
                else if (_commitsAfterRelease == null)
                {
                    return -1;
                }
                else if (other._commitsAfterRelease == null)
                {
                    return 1;
                }

                return _commitsAfterRelease.Value.CompareTo(other._commitsAfterRelease.Value);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersion);
        }

        /// <inheritdoc/>
        public bool Equals(SemanticVersion other)
        {
            return CompareTo(other) == 0;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder($"{_major}.{_minor}.{_patch}");
            if (_preRelease != null)
            {
                sb.Append($"-{_preRelease}");
            }
            if (_commitsAfterRelease != null)
            {
                sb.Append($"-{_commitsAfterRelease}-g{_commitHash}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses a string representation of a semantic version.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <returns>A semantic version.</returns>
        public static SemanticVersion Parse(string value)
        {
            if (TryParse(value, out var result))
            {
                return result;
            }

            throw new FormatException($"Invalid SemanticVersion string: '{value}'.");
        }

        /// <summary>
        /// Tries to parse a string representation of a semantic version.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">The result.</param>
        /// <returns>True if the string representation was parsed successfully; otherwise false.</returns>
        public static bool TryParse(string value, out SemanticVersion result)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var pattern = @"(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+)(-(?<preRelease>.*))?)?";
                var match = Regex.Match((string)value, pattern);
                if (match.Success)
                {
                    var major = int.Parse(match.Groups["major"].Value);
                    var minor = int.Parse(match.Groups["minor"].Value);
                    var patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
                    var preRelease = match.Groups["preRelease"].Success ? match.Groups["preRelease"].Value : null;

                    result = new SemanticVersion(major, minor, patch, preRelease);
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Determines whether two specified semantic versions have the same value.
        /// </summary>
        /// <param name="a">The first semantic version to compare, or null.</param>
        /// <param name="b">The second semantic version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is the same as the value of b; otherwise false.
        /// </returns>
        public static bool operator ==(SemanticVersion a, SemanticVersion b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return object.ReferenceEquals(b, null);
            }

            return a.CompareTo(b) == 0;
        }

        /// <summary>
        /// Determines whether two specified semantic versions have different values.
        /// </summary>
        /// <param name="a">The first semantic version to compare, or null.</param>
        /// <param name="b">The second semantic version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is different from the value of b; otherwise false.
        /// </returns>
        public static bool operator !=(SemanticVersion a, SemanticVersion b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Determines whether the first specified SemanticVersion is greater than the second specified SemanticVersion.
        /// </summary>
        /// <param name="a">The first semantic version to compare, or null.</param>
        /// <param name="b">The second semantic version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is greater than b; otherwise false.
        /// </returns>
        public static bool operator >(SemanticVersion a, SemanticVersion b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return false;
            }

            return a.CompareTo(b) > 0;
        }

        /// <summary>
        /// Determines whether the first specified SemanticVersion is greater than or equal to the second specified SemanticVersion.
        /// </summary>
        /// <param name="a">The first semantic version to compare, or null.</param>
        /// <param name="b">The second semantic version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is greater than or equal to b; otherwise false.
        /// </returns>
        public static bool operator >=(SemanticVersion a, SemanticVersion b)
        {
            return !(a < b);
        }

        /// <summary>
        /// Determines whether the first specified SemanticVersion is less than the second specified SemanticVersion.
        /// </summary>
        /// <param name="a">The first semantic version to compare, or null.</param>
        /// <param name="b">The second semantic version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is less than b; otherwise false.
        /// </returns>
        public static bool operator <(SemanticVersion a, SemanticVersion b)
        {
            return b > a;
        }

        /// <summary>
        /// Determines whether the first specified SemanticVersion is less than or equal to the second specified SemanticVersion.
        /// </summary>
        /// <param name="a">The first semantic version to compare, or null.</param>
        /// <param name="b">The second semantic version to compare, or null.</param>
        /// <returns>
        /// True if the value of a is less than or equal to b; otherwise false.
        /// </returns>
        public static bool operator <=(SemanticVersion a, SemanticVersion b)
        {
            return !(b < a);
        }
    }
}
