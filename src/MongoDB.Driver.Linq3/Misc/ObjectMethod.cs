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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Driver.Linq3.Misc
{
    public static class ObjectMethod
    {
        // private static fields
        private static readonly MethodInfo __equals;
        private static readonly MethodInfo __toString;

        // static constructor
        static ObjectMethod()
        {
            __equals = new Func<object, bool>(new object().Equals).Method;
            __toString = new Func<string>(new object().ToString).Method;
        }

        // public properties
        public static new MethodInfo Equals => __equals;
        public static new MethodInfo ToString => __toString;
    }
}
