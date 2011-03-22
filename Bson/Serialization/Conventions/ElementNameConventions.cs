﻿/* Copyright 2010-2011 10gen Inc.
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
using System.Reflection;

namespace MongoDB.Bson.Serialization.Conventions {
    /// <summary>
    /// Represents an element name convention.
    /// </summary>
    public interface IElementNameConvention {
        /// <summary>
        /// Gets the element name for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The element name.</returns>
        string GetElementName(MemberInfo member);
    }

    /// <summary>
    /// Represents an element name convention where the element name is the same as the member name.
    /// </summary>
    public class MemberNameElementNameConvention : IElementNameConvention {
        /// <summary>
        /// Gets the element name for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The element name.</returns>
        public string GetElementName(
            MemberInfo member
        ) {
            return member.Name;
        }
    }

    /// <summary>
    /// Represents an element name convention where the element name is the member name with the first character lower cased.
    /// </summary>
    public class CamelCaseElementNameConvention : IElementNameConvention {
        /// <summary>
        /// Gets the element name for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The element name.</returns>
        public string GetElementName(
            MemberInfo member
        ) {
            string name = member.Name;
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }

}
