﻿/* Copyright 2010-2013 10gen Inc.
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
    /// Represents a default value convention.
    /// </summary>
    [Obsolete("Use IMemberMapConvention instead.")]
    public interface IDefaultValueConvention
    {
        /// <summary>
        /// Gets the default value for a member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>The default value.</returns>
        object GetDefaultValue(MemberInfo memberInfo);
    }

    /// <summary>
    /// Represents a default value convention of null.
    /// </summary>
    [Obsolete("Use DelegateMemberMapConvention instead.")]
    public class NullDefaultValueConvention : IDefaultValueConvention
    {
        /// <summary>
        /// Gets the default value for a member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>null.</returns>
        public object GetDefaultValue(MemberInfo memberInfo)
        {
            return null;
        }
    }
}