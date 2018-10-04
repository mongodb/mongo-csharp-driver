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

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that sets whether to use an existing instance during deserialization.
    /// </summary>
    public class UseExistingInstanceConvention : ConventionBase, IMemberMapConvention
    {
        // private fields
        private readonly bool _useExistingInstance;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreIfNullConvention" /> class.
        /// </summary>
        /// <param name="useExistingInstance">Whether to use and existing instance during deserialization.</param>
        public UseExistingInstanceConvention(bool useExistingInstance)
        {
            _useExistingInstance = useExistingInstance;
        }

        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            if (!memberMap.MemberType.IsValueType)
            {
                memberMap.SetUseExistingInstance(_useExistingInstance);
            }
        }
    }
}
