/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Misc
{
    public class SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
    {
        // fields
        private readonly int _major;
        private readonly int _minor;
        private readonly int _patch;
        private readonly string _preRelease;

        // constructors
        public SemanticVersion(int major, int minor, int patch)
            : this(major, minor, patch, null)
        {
        }

        public SemanticVersion(int major, int minor, int patch, string preRelease)
        {
            _major = Ensure.IsGreaterThanOrEqualToZero(major, "major");
            _minor = Ensure.IsGreaterThanOrEqualToZero(minor, "minor");
            _patch = Ensure.IsGreaterThanOrEqualToZero(patch, "patch");
            _preRelease = preRelease; // can be null
        }

        // properties
        public int Major
        {
            get { return _major; }
        }

        public int Minor
        {
            get { return _minor; }
        }

        public int Patch
        {
            get { return _patch; }
        }

        public string PreRelease
        {
            get { return _preRelease; }
        }

        // methods
        public int CompareTo(SemanticVersion other)
        {
            if (other == null)
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

            return _preRelease.CompareTo(other._preRelease);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersion);
        }

        public bool Equals(SemanticVersion other)
        {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            if (_preRelease == null)
            {
                return string.Format("{0}.{1}.{2}", _major, _minor, _patch);
            }
            else
            {
                return string.Format("{0}.{1}.{2}-{3}", _major, _minor, _patch, _preRelease);
            }
        }

        public static SemanticVersion Parse(string s)
        {
            SemanticVersion value;
            if (TryParse(s, out value))
            {
                return value;
            }

            throw new FormatException(string.Format(
                "Invalid SemanticVersion string: '{0}'.", s));
        }

        public static bool TryParse(string s, out SemanticVersion value)
        {
            if (!string.IsNullOrEmpty(s))
            {
                var pattern = @"(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+)(-(?<preRelease>.+))?)?";
                var match = Regex.Match((string)s, pattern);
                if (match.Success)
                {
                    var major = int.Parse(match.Groups["major"].Value);
                    var minor = int.Parse(match.Groups["minor"].Value);
                    var patch = int.Parse(match.Groups["patch"].Value);
                    var preRelease = match.Groups["preRelease"].Success ? match.Groups["preRelease"].Value : null;

                    value = new SemanticVersion(major, minor, patch, preRelease);
                    return true;
                }
            }

            value = null;
            return false;
        }

        public static bool operator ==(SemanticVersion lhs, SemanticVersion rhs)
        {
            if (object.ReferenceEquals(lhs, null))
            {
                return object.ReferenceEquals(rhs, null);
            }

            return lhs.CompareTo(rhs) == 0;
        }

        public static bool operator !=(SemanticVersion lhs, SemanticVersion rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator >(SemanticVersion lhs, SemanticVersion rhs)
        {
            if (lhs == null)
            {
                if (rhs == null)
                {
                    return true;
                }

                return false;
            }

            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator >=(SemanticVersion lhs, SemanticVersion rhs)
        {
            return !(lhs < rhs);
        }

        public static bool operator <(SemanticVersion lhs, SemanticVersion rhs)
        {
            return rhs > lhs;
        }

        public static bool operator <=(SemanticVersion lhs, SemanticVersion rhs)
        {
            return !(rhs < lhs);
        }
    }
}