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
using System.Reflection;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that looks up an id generator for the id member.
    /// </summary>
    public class LookupIdGeneratorConvention : ConventionBase, IPostProcessingConvention
    {
        /// <inheritdoc/>
        public void PostProcess(BsonClassMap classMap) => PostProcess(classMap, BsonSerializer.DefaultSerializationDomain);

        /// <inheritdoc/>
        public void PostProcess(BsonClassMap classMap, IBsonSerializationDomain domain)
        {
            var idMemberMap = classMap.IdMemberMap;
            if (idMemberMap != null)
            {
                if (idMemberMap.IdGenerator == null)
                {
                    //or we pass the domain to the BsonClassMap. The first probably makes more sense, but it's messier.
                    var idGenerator = domain.LookupIdGenerator(idMemberMap.MemberType);
                    if (idGenerator != null)
                    {
                        idMemberMap.SetIdGenerator(idGenerator);
                    }
                }
            }
        }
    }
}
