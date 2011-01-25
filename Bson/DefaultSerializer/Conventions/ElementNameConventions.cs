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
    public interface IElementNameConvention {
        string GetElementName(MemberInfo member);
    }

    public class MemberNameElementNameConvention : IElementNameConvention {
        public string GetElementName(
            MemberInfo member
        ) {
            return member.Name;
        }
    }

    public class CamelCaseElementNameConvention : IElementNameConvention {
        public string GetElementName(
            MemberInfo member
        ) {
            string name = member.Name;
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }

}
