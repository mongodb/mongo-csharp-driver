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
    internal static class PropertyInfoExtensions
    {
        public static bool Is(this PropertyInfo property, PropertyInfo comparand)
        {
            if (property == comparand)
            {
                return true;
            }

            var declaringType = property.DeclaringType;
            if (declaringType.IsConstructedGenericType)
            {
                var typeDefinition = declaringType.GetGenericTypeDefinition();
                var typeDefinitionProperty = typeDefinition.GetProperty(property.Name);
                if (typeDefinitionProperty == comparand)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsOneOf(this PropertyInfo property, PropertyInfo comparand1, PropertyInfo comparand2)
        {
            return property.Is(comparand1) || property.Is(comparand2);
        }

        public static bool IsOneOf(this PropertyInfo property, PropertyInfo comparand1, PropertyInfo comparand2, PropertyInfo comparand3)
        {
            return property.Is(comparand1) || property.Is(comparand2) || property.Is(comparand3);
        }

        public static bool IsOneOf(this PropertyInfo property, PropertyInfo comparand1, PropertyInfo comparand2, PropertyInfo comparand3, PropertyInfo comparand4)
        {
            return property.Is(comparand1) || property.Is(comparand2) || property.Is(comparand3) || property.Is(comparand4);
        }

        public static bool IsOneOf(this PropertyInfo property, params PropertyInfo[] comparands)
        {
            for (var i = 0; i < comparands.Length; i++)
            {
                if (property.Is(comparands[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
