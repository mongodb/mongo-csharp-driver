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
    public interface IIdMemberConvention {
        string FindIdMember(Type type); 
    }

    public class NamedIdMemberConvention : IIdMemberConvention {
        public string[] Names { get; private set; }

        public NamedIdMemberConvention(
            params string[] names
        ) {
            Names = names;
        }

        public string FindIdMember(
            Type type
        ) {
            foreach (string name in Names) {
                var memberInfo = type.GetMember(name).SingleOrDefault(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);
                if (memberInfo != null) {
                    return name;
                }
            }
            return null;
        }
    }
}
