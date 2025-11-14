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
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class EnumerableProperty
    {
        // public methods
        public static bool IsCountProperty(MemberExpression expression)
        {
            // Count is not actually a property defined by IEnumerable but rather defined by several sub-interfaces
            return
                expression.Expression != null &&
                expression.Member is PropertyInfo propertyInfo &&
                propertyInfo.Name == "Count" &&
                propertyInfo.PropertyType == typeof(int) &&
                propertyInfo.GetGetMethod().GetParameters().Length == 0 &&
                ImplementsCollectionInterface(expression.Expression.Type);

            static bool ImplementsCollectionInterface(Type type)
                =>
                    type.ImplementsInterface(typeof(ICollection)) ||
                    type.ImplementsInterface(typeof(ICollection<>)) ||
                    type.ImplementsInterface(typeof(IReadOnlyCollection<>));
        }
    }
}
