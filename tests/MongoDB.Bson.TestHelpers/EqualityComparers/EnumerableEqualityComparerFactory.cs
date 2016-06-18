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
    public class EnumerableEqualityComparerFactory : IEqualityComparerFactory
    {
        // fields
        private readonly IEqualityComparerFactory _next;

        // constructors
        public EnumerableEqualityComparerFactory(IEqualityComparerFactory next)
        {
            _next = next;
        }

        // methods
        public IEqualityComparer CreateComparer(Type valueType, IEqualityComparerSource source)
        {
            foreach (var implementedInterface in valueType.GetTypeInfo().GetInterfaces())
            {
                if (implementedInterface == typeof(IEnumerable))
                {
                    return new EnumerableEqualityComparer(source);
                }
            }

            return _next.CreateComparer(valueType, source);
        }

        public IEqualityComparer<T> CreateComparer<T>(IEqualityComparerSource source)
        {
            var valueType = typeof(T);
            foreach (var implementedInterface in valueType.GetInterfaces())
            {
                if (IsGenericIEnumerableInterface(implementedInterface))
                {
                    var itemType = implementedInterface.GetGenericArguments()[0];
                    var genericComparerType = typeof(EnumerableEqualityComparer<,>).MakeGenericType(new[] { valueType, itemType });
                    var genericComparerConstructor = genericComparerType.GetConstructor(new[] { typeof(IEqualityComparerSource) });
                    var genericComparer = (IEqualityComparer<T>)genericComparerConstructor.Invoke(new object[] { source });
                    return genericComparer;
                }
            }

            return _next.CreateComparer<T>(source);
        }

        private bool IsGenericIEnumerableInterface(Type implementedInterface)
        {
            return implementedInterface.IsConstructedGenericType && implementedInterface.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }
    }
}
