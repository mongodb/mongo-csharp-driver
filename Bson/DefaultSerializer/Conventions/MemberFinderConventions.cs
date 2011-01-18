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
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions {
    public interface IMemberFinderConvention{
        IEnumerable<MemberInfo> FindMembers(Type type);
    }

    public class PublicMemberFinderConvention : IMemberFinderConvention {
        public IEnumerable<MemberInfo> FindMembers(
            Type type
        ) {
            foreach (var fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral) { // we can't write
                    continue;
                }

                yield return fieldInfo;
            }

            foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                if (!propertyInfo.CanRead || (!propertyInfo.CanWrite && type.Namespace != null)) { // we can't write or it is anonymous...
                    continue;
                }

                // skip indexers
                if (propertyInfo.GetIndexParameters().Length != 0) {
                    continue;
                }

                // skip overridden properties (they are already included by the base class)
                var getMethodInfo = propertyInfo.GetGetMethod(true);
                if (getMethodInfo.IsVirtual && getMethodInfo.GetBaseDefinition().DeclaringType != type) {
                    continue;
                }

                yield return propertyInfo;
            }
        }
    }
}
