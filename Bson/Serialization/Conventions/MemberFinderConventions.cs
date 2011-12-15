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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents a member finder convention.
    /// </summary>
    public interface IMemberFinderConvention
    {
        /// <summary>
        /// Finds the members of a class that are serialized.
        /// </summary>
        /// <param name="type">The class.</param>
        /// <returns>The members that are serialized.</returns>
        IEnumerable<MemberInfo> FindMembers(Type type);
    }

    /// <summary>
    /// Represents a base member finder convention where all read/write fields and properties are serialized based on binding flags.
    /// </summary>
    public abstract class BindingFlagsMemberFinderConvention : IMemberFinderConvention
    {
        // private fields
        private const BindingFlags ValidMemberBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly BindingFlags memberBindingFlags;

        // constructors
        /// <summary>
        /// Initializes an instance of the BindingFlagsMemberFinderConvention class.
        /// </summary>
        /// <param name="memberBindingFlags">The member binding flags.</param>
        protected BindingFlagsMemberFinderConvention(BindingFlags memberBindingFlags)
        {
            if ((memberBindingFlags & ~ValidMemberBindingFlags) != 0)
            {
                throw new ArgumentException("Invalid binding flags '" + memberBindingFlags + "'", "memberBindingFlags");
            }

            this.memberBindingFlags = memberBindingFlags;
        }

        /// <summary>
        /// Finds the members of a class that are serialized.
        /// </summary>
        /// <param name="type">The class.</param>
        /// <returns>The members that are serialized.</returns>
        public IEnumerable<MemberInfo> FindMembers(Type type)
        {
            foreach (var fieldInfo in type.GetFields(memberBindingFlags | BindingFlags.DeclaredOnly))
            {
                if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
                {
                    // we can't write
                    continue;
                }

                if (fieldInfo.IsPrivate && fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), false))
                {
                    // skip private compiler generated backing fields
                    continue;
                }

                yield return fieldInfo;
            }

            foreach (var propertyInfo in type.GetProperties(memberBindingFlags | BindingFlags.DeclaredOnly))
            {
                if (!propertyInfo.CanRead || (!propertyInfo.CanWrite && type.Namespace != null))
                {
                    // we can't read, or we can't write and it is not anonymous
                    continue;
                }

                // skip indexers
                if (propertyInfo.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                // skip overridden properties (they are already included by the base class)
                var getMethodInfo = propertyInfo.GetGetMethod(true);
                if (getMethodInfo.IsVirtual && getMethodInfo.GetBaseDefinition().DeclaringType != type)
                {
                    continue;
                }

                yield return propertyInfo;
            }
        }
    }

    /// <summary>
    /// Represents a member finder convention where all public read/write fields and properties are serialized.
    /// </summary>
    public class PublicMemberFinderConvention : BindingFlagsMemberFinderConvention
    {
        /// <summary>
        /// Initializes an instance of the PublicMemberFinderConvention class.
        /// </summary>
        public PublicMemberFinderConvention()
            : base(BindingFlags.Public | BindingFlags.Instance)
        {
        }
    }

    /// <summary>
    /// Represents a member finder convention where all public, internal, and private read/write fields and properties are serialized.
    /// </summary>
    public class PrivateMemberFinderConvention : BindingFlagsMemberFinderConvention
    {
        /// <summary>
        /// Initializes an instance of the PrivateMemberFinderConvention class.
        /// </summary>
        public PrivateMemberFinderConvention()
            : base(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
        }
    }
}
