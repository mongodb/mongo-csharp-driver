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
    /// Represents an ignore if null convention.
    /// </summary>
    public interface IIgnoreIfNullConvention {
        /// <summary>
        /// Determines whether to ignore nulls for a member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>Whether to ignore nulls.</returns>
        bool IgnoreIfNull(MemberInfo memberInfo);
    }

    /// <summary>
    /// Represents an ignore if null convention where nulls are never ignored.
    /// </summary>
    public class NeverIgnoreIfNullConvention : IIgnoreIfNullConvention {
        /// <summary>
        /// Determines whether to ignore nulls for a member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>Whether to ignore nulls.</returns>
        public bool IgnoreIfNull(
            MemberInfo memberInfo
        ) {
            return false;
        }
    }

    /// <summary>
    /// Represents an ignore if null convention where nulls are always ignored.
    /// </summary>
    public class AlwaysIgnoreIfNullConvention : IIgnoreIfNullConvention {
        /// <summary>
        /// Determines whether to ignore nulls for a member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>Whether to ignore nulls.</returns>
        public bool IgnoreIfNull(
            MemberInfo memberInfo
        ) {
            return true;
        }
    }
}
