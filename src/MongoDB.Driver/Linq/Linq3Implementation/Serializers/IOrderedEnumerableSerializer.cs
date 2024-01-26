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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal class IOrderedEnumerableSerializer<TItem> : IEnumerableSerializerBase<IOrderedEnumerable<TItem>, TItem>
    {
        // constructors
        public IOrderedEnumerableSerializer(IBsonSerializer<TItem> itemSerializer)
            : base(itemSerializer)
        {
        }

        // protected methods
        protected override IOrderedEnumerable<TItem> CreateDeserializedValue(List<TItem> items) => new IOrderedEnumerableWrapper(items);

        private class IOrderedEnumerableWrapper : IOrderedEnumerable<TItem>
        {
            private readonly IEnumerable<TItem> _items;
            public IOrderedEnumerableWrapper(IEnumerable<TItem> items) => _items = items;
            public IOrderedEnumerable<TItem> CreateOrderedEnumerable<TKey>(Func<TItem, TKey> keySelector, IComparer<TKey> comparer, bool descending) => throw new InvalidOperationException("ThenBy or ThenByDescending cannot be executed client-side and should be moved to the LINQ query.");
            public IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class IOrderedEnumerableSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer itemSerializer)
        {
            var itemType = itemSerializer.ValueType;
            var serializerType = typeof(IOrderedEnumerableSerializer<>).MakeGenericType(itemType);
            return (IBsonSerializer)Activator.CreateInstance(serializerType, itemSerializer);
        }
    }
}
