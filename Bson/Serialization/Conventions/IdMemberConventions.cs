/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents an Id member convention.
    /// </summary>
    public interface IIdMemberConvention
    {
        /// <summary>
        /// Finds the Id member of a class.
        /// </summary>
        /// <param name="type">The class.</param>
        /// <returns>The name of the Id member.</returns>
        string FindIdMember(Type type);
    }

    /// <summary>
    /// Represents an Id member convention where the Id member name is one of a set of possible Id member names.
    /// </summary>
    public class NamedIdMemberConvention : IIdMemberConvention
    {
        /// <summary>
        /// Gets the set of possible Id member names.
        /// </summary>
        public string[] Names { get; private set; }

        /// <summary>
        /// Initializes a new instance of the NamedIdMemberConvention class.
        /// </summary>
        /// <param name="names">A set of possible Id member names.</param>
        public NamedIdMemberConvention(params string[] names)
        {
            Names = names;
        }

        /// <summary>
        /// Finds the Id member of a class.
        /// </summary>
        /// <param name="type">The class.</param>
        /// <returns>The name of the Id member.</returns>
        public string FindIdMember(Type type)
        {
            foreach (string name in Names)
            {
                var memberInfo = type.GetMember(name).SingleOrDefault(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);
                if (memberInfo != null)
                {
                    return name;
                }
            }
            return null;
        }
    }
}
