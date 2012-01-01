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
    /// Represents an extra elements member convention.
    /// </summary>
    public interface IExtraElementsMemberConvention
    {
        /// <summary>
        /// Finds the extra elements member of a class.
        /// </summary>
        /// <param name="type">The class.</param>
        /// <returns>The extra elements member.</returns>
        string FindExtraElementsMember(Type type);
    }

    /// <summary>
    /// Represents an extra elements member convention where the extra elements member has a certain name.
    /// </summary>
    public class NamedExtraElementsMemberConvention : IExtraElementsMemberConvention
    {
        /// <summary>
        /// Gets the name of the extra elements member.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the NamedExtraElementsMemberConvention class.
        /// </summary>
        /// <param name="name">The name of the extra elements member.</param>
        public NamedExtraElementsMemberConvention(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Finds the extra elements member of a class.
        /// </summary>
        /// <param name="type">The class.</param>
        /// <returns>The extra elements member.</returns>
        public string FindExtraElementsMember(Type type)
        {
            var memberInfo = type.GetMember(Name).SingleOrDefault(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);
            return (memberInfo != null) ? Name : null;
        }
    }
}
