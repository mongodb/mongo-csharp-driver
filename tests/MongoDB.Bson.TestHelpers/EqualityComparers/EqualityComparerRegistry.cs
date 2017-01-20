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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public class EqualityComparerRegistry : IEqualityComparerSource
    {
        #region static
        // static fields
        private static readonly EqualityComparerRegistry __default;

        // static constructor
        static EqualityComparerRegistry()
        {
            var factory =
                new EnumerableEqualityComparerFactory(
                new EqualsEqualityComparerFactory(
                new FieldsEqualityComparerFactory()));
            __default = new EqualityComparerRegistry(factory);
        }

        // static properties
        public static EqualityComparerRegistry Default
        {
            get { return __default; }
        }
        #endregion

        // fields
        private readonly IEqualityComparerFactory _factory;
        private readonly ConcurrentDictionary<Type, ComparerPair> _registry = new ConcurrentDictionary<Type, ComparerPair>();

        // constructors
        public EqualityComparerRegistry(IEqualityComparerFactory factory)
        {
            _factory = factory;
        }

        // methods
        public IEqualityComparer GetComparer(Type valueType)
        {
            return _registry.GetOrAdd(valueType, CreateComparerPair).NonGenericComparer;
        }

        public IEqualityComparer<T> GetComparer<T>()
        {
            return (IEqualityComparer<T>)_registry.GetOrAdd(typeof(T), CreateComparerPair<T>).GenericComparer;
        }

        public void RegisterComparer(Type valueType, IEqualityComparer nonGenericComparer)
        {
            var comparerPair = CreateComparerPair(valueType, nonGenericComparer);
            RegisterComparerPair(valueType, comparerPair);
        }

        public void RegisterComparer<T>(IEqualityComparer<T> genericComparer)
        {
            var comparerPair = CreateComparerPair<T>(genericComparer);
            RegisterComparerPair(typeof(T), comparerPair);
        }

        private ComparerPair CreateComparerPair<T>(IEqualityComparer<T> genericComparer)
        {
            var nonGenericComparer = new NonGenericEqualityComparerAdapter<T>(genericComparer);
            return new ComparerPair { NonGenericComparer = nonGenericComparer, GenericComparer = genericComparer };
        }

        private ComparerPair CreateComparerPair<T>(Type valueType)
        {
            var genericComparer = _factory.CreateComparer<T>(this);
            return CreateComparerPair(genericComparer);
        }

        private ComparerPair CreateComparerPair(Type valueType)
        {
            var nonGenericComparer = _factory.CreateComparer(valueType, this);
            return CreateComparerPair(valueType, nonGenericComparer);
        }

        private ComparerPair CreateComparerPair(Type valueType, IEqualityComparer nonGenericComparer)
        {
            var genericComparerType = typeof(GenericEqualityComparerAdapter<>).MakeGenericType(valueType);
            var genericComparerConstructor = genericComparerType.GetTypeInfo().GetConstructor(new[] { typeof(IEqualityComparer) });
            var genericComparer = genericComparerConstructor.Invoke(new object[] { nonGenericComparer });
            return new ComparerPair { NonGenericComparer = nonGenericComparer, GenericComparer = genericComparer };
        }

        private void RegisterComparerPair(Type valueType, ComparerPair comparerPair)
        {
            if (!_registry.TryAdd(valueType, comparerPair))
            {
                throw new ArgumentException("A comparer has already been registerd for this type.");
            }
        }

        // nested types
        private struct ComparerPair
        {
            public object GenericComparer;
            public IEqualityComparer NonGenericComparer;
        }
    }
}
