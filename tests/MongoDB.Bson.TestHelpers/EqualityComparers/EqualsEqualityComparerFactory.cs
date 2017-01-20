/* Copyright 2010-2016 MongoDB Inc.
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
using System.Reflection;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public class EqualsEqualityComparerFactory : IEqualityComparerFactory
    {
        // fields
        private readonly IEqualityComparerFactory _next;

        // constructors
        public EqualsEqualityComparerFactory(IEqualityComparerFactory next)
        {
            _next = next;
        }

        // methods
        public IEqualityComparer CreateComparer(Type valueType, IEqualityComparerSource source)
        {
            if (OverridesEquals(valueType))
            {
                return new EqualsEqualityComparer();
            }

            return _next.CreateComparer(valueType, source);
        }

        public IEqualityComparer<T> CreateComparer<T>(IEqualityComparerSource source)
        {
            var valueType = typeof(T);

            foreach (var implementedInterface in valueType.GetTypeInfo().GetInterfaces())
            {
                if (IsGenericIEquatableInterface(implementedInterface, valueType))
                {
                    var genericComparerType = typeof(EqualsEqualityComparer<>).MakeGenericType(valueType);
                    var genericComparerConstructor = genericComparerType.GetConstructor(new Type[0]);
                    var genericComparer = (IEqualityComparer<T>)genericComparerConstructor.Invoke(new object[0]);
                    return genericComparer;
                }
            }

            if (OverridesEquals(valueType))
            {
                var nonGenericComparer = new EqualsEqualityComparer();
                var genericComparer = new GenericEqualityComparerAdapter<T>(nonGenericComparer);
                return genericComparer;
            }

            return _next.CreateComparer<T>(source);
        }

        private bool IsGenericIEquatableInterface(Type implementedInterface, Type valueType)
        {
            return
                implementedInterface.IsConstructedGenericType &&
                implementedInterface.GetGenericTypeDefinition() == typeof(IEquatable<>) &&
                implementedInterface.GetGenericArguments()[0] == valueType;
        }

        private bool OverridesEquals(Type valueType)
        {
            var equalsMethod = valueType.GetMethod("Equals", new[] { typeof(object) });
            return equalsMethod.DeclaringType != typeof(object);
        }
    }
}
