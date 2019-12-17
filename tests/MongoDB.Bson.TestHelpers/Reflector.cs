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
        
        public static object GetStaticFieldValue(Type type, string name, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static)
        {
            var fieldInfo = GetDeclaredOrInheritedField(type, name, flags);
            return fieldInfo.GetValue(null);
        }

        public static object Invoke(object obj, string name, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            try
            {
                var methodInfo = obj.GetType().GetMethods(flags)
                    .Where(m => m.Name == name && m.GetParameters().Length == 0)
                    .Single();
                return methodInfo.Invoke(obj, new object[] { });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object Invoke<T1>(object obj, string name, T1 arg1)
        {
            var parameterTypes = new[] { typeof(T1) };
            var methodInfo = obj.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .Single();
            try
            {
                return methodInfo.Invoke(obj, new object[] { arg1 });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object Invoke<T1, T2>(object obj, string name, out T1 arg1, out T2 arg2)
        {
            arg1 = default;
            arg2 = default;
            var parameterTypes = new[] { typeof(T1), typeof(T2) }.Select(t => t.FullName);
            var methodInfo = obj
                .GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => 
                    m.Name == name && 
                    m.GetParameters()
                        .Select(p => p.ParameterType.FullName.TrimEnd('&'))
                        .SequenceEqual(parameterTypes))
                .Single();
            try
            {
                var arguments = new object[] { arg1, arg2 };
                var result = methodInfo.Invoke(obj, arguments);
                arg1 = (T1)arguments[0];
                arg2 = (T2)arguments[1];
                return result;
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object Invoke<T1, T2, T3>(object obj, string name, T1 arg1, T2 arg2, T3 arg3)
        {
            var parameterTypes = new[] { typeof(T1), typeof(T2), typeof(T3) };
            var methodInfo = obj.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .Single();
            try
            {
                return methodInfo.Invoke(obj, new object[] { arg1, arg2, arg3 });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object InvokeStatic(Type type, string name, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static)
        {
            var methodInfo = type.GetMethods(flags)
                .Where(m => m.Name == name && m.GetParameters().Length == 0)
                .Single();
            try
            {
                return methodInfo.Invoke(null, new object[] { });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object InvokeStatic<T1>(Type type, string name, T1 arg1)
        {
            var parameterTypes = new[] { typeof(T1) };
            var methodInfo = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .Single();
            try
            {
                return methodInfo.Invoke(null, new object[] { arg1 });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object InvokeStatic<T1, T2, T3>(Type type, string name, T1 arg1, T2 arg2, T3 arg3)
        {
            var parameterTypes = new[] { typeof(T1), typeof(T2), typeof(T3) };
            var methodInfo = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .Single();
            try
            {
                return methodInfo.Invoke(null, new object[] { arg1, arg2, arg3 });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static object InvokeStatic<T1, T2, T3, T4>(Type type, string name, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var parameterTypes = new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
            var methodInfo = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .Single();
            try
            {
                return methodInfo.Invoke(null, new object[] { arg1, arg2, arg3, arg4 });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }

        public static void SetFieldValue(object obj, string name, object value, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            var fieldInfo = GetDeclaredOrInheritedField(obj.GetType(), name, flags);
            fieldInfo.SetValue(obj, value);
        }

        public static void SetStaticFieldValue(Type type, string name, object value)
        {
            var fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            fieldInfo.SetValue(null, value);
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
