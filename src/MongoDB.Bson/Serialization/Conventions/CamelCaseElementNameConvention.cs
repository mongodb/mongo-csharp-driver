/* Copyright 2010-2014 MongoDB Inc.
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
using System.Reflection;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that sets the element name the same as the member name with the first character lower cased.
    /// </summary>
    public class CamelCaseElementNameConvention : ConventionBase, IMemberMapConvention
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="skipWhenAllCaps">
        /// When set to true, this convention will skip members in capital letters (for example: "ALLCAPS")
        /// in order to preserve acronyms or member names designed to be in upper-case.
        /// </param>
        public CamelCaseElementNameConvention(bool skipWhenAllCaps = false)
        {
            SkipWhenAllCaps = skipWhenAllCaps;
        }

        /// <summary>
        /// Gets if this convention will skip members in capital letters (for example: "ALLCAPS")
        /// in order to preserve acronyms or member names designed to be in upper-case.
        /// </summary>
        private bool SkipWhenAllCaps { get; }

        // public methods
        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            string name = memberMap.MemberName;
            name = GetElementName(name);
            memberMap.SetElementName(name);
        }

        // private methods
        private string GetElementName(string memberName)
        {
            if (memberName.Length == 0)
            {
                return "";
            }
            else if (SkipWhenAllCaps && memberName == memberName.ToUpper())
            {
                return memberName;
            }
            else if(memberName.Length == 1)
            {
                return Char.ToLowerInvariant(memberName[0]).ToString();
            }
            else
            {
                return Char.ToLowerInvariant(memberName[0]) + memberName.Substring(1);
            }
        }
    }
}