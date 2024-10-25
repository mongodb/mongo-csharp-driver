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

#if NET6_0_OR_GREATER
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for ImmutableSortedDictionaries.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class ImmutableSortedDictionarySerializer<TKey, TValue>: DictionaryInterfaceImplementerSerializer<ImmutableSortedDictionary<TKey, TValue>, TKey, TValue>
    {
        /// <inheritdoc/>
        protected override ICollection<KeyValuePair<TKey, TValue>> CreateAccumulator()
        {
            return ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();
        }

        /// <inheritdoc/>
        protected override ImmutableSortedDictionary<TKey, TValue> FinalizeAccumulator(ICollection<KeyValuePair<TKey, TValue>> accumulator)
        {
            return ((ImmutableSortedDictionary<TKey, TValue>.Builder)accumulator).ToImmutable();
        }
    }
}
#endif