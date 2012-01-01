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

using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents an Id generator convention.
    /// </summary>
    public interface IIdGeneratorConvention
    {
        /// <summary>
        /// Gets the Id generator for an Id member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>An Id generator.</returns>
        IIdGenerator GetIdGenerator(MemberInfo memberInfo);
    }

    /// <summary>
    /// Represents an Id generator convention where the Id generator is looked up based on the member type.
    /// </summary>
    public class LookupIdGeneratorConvention : IIdGeneratorConvention
    {
        /// <summary>
        /// Gets the Id generator for an Id member.
        /// </summary>
        /// <param name="memberInfo">The member.</param>
        /// <returns>An Id generator.</returns>
        public IIdGenerator GetIdGenerator(MemberInfo memberInfo)
        {
            return BsonSerializer.LookupIdGenerator(BsonClassMap.GetMemberInfoType(memberInfo));
        }
    }
}
