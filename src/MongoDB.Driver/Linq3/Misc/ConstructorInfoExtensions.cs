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

using System.Reflection;

namespace MongoDB.Driver.Linq3.Misc
{
    internal static class ConstructorInfoExtensions
    {
        public static bool Is(this ConstructorInfo constructor, ConstructorInfo comparand)
        {
            if (constructor == comparand)
            {
                return true;
            }

            return false;
        }

        public static bool IsOneOf(this ConstructorInfo constructor, ConstructorInfo comparand1, ConstructorInfo comparand2)
        {
            return constructor.Is(comparand1) || constructor.Is(comparand2);
        }

        public static bool IsOneOf(this ConstructorInfo constructor, ConstructorInfo comparand1, ConstructorInfo comparand2, ConstructorInfo comparand3)
        {
            return constructor.Is(comparand1) || constructor.Is(comparand2) || constructor.Is(comparand3);
        }

        public static bool IsOneOf(this ConstructorInfo constructor, ConstructorInfo comparand1, ConstructorInfo comparand2, ConstructorInfo comparand3, ConstructorInfo comparand4)
        {
            return constructor.Is(comparand1) || constructor.Is(comparand2) || constructor.Is(comparand3) || constructor.Is(comparand4);
        }

        public static bool IsOneOf(this ConstructorInfo constructor, params ConstructorInfo[] comparands)
        {
            for (var i = 0; i < comparands.Length; i++)
            {
                if (constructor.Is(comparands[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
