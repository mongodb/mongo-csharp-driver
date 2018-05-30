/* Copyright 2018-present MongoDB Inc.
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
using System.Linq;
using System.Reflection;

namespace MongoDB.Bson.TestHelpers
{
    public static class Reflector
    {
        public static object GetFieldValue(object obj, string name, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            var fieldInfo = GetDeclaredOrInheritedField(obj.GetType(), name, flags);
            return fieldInfo.GetValue(obj);
        }

        public static object GetPropertyValue(object obj, string name, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            var propertyInfo = obj.GetType().GetProperty(name, flags);
            return propertyInfo.GetValue(obj);
        }

        public static object Invoke(object obj, string name, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            var methodInfo = obj.GetType().GetMethods(flags)
                .Where(m => m.Name == name && m.GetParameters().Length == 0)
                .Single();
            return methodInfo.Invoke(obj, new object[] { });
        }

        public static object Invoke<T1>(object obj, string name, T1 arg1)
        {
            var parameterTypes = new[] { typeof(T1) };
            var methodInfo = obj.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .Single();
            return methodInfo.Invoke(obj, new object[] { arg1 });
        }

        public static void SetFieldValue(object obj, string name, object value, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            var fieldInfo = GetDeclaredOrInheritedField(obj.GetType(), name, flags);
            fieldInfo.SetValue(obj, value);
        }

        // private methods
        private static FieldInfo GetDeclaredOrInheritedField(Type type, string name, BindingFlags bindingFlags)
        {
            return
                type == null ?
                    null :
                    type.GetField(name, bindingFlags) ?? GetDeclaredOrInheritedField(type.GetTypeInfo().BaseType, name, bindingFlags);
        }
    }
}
