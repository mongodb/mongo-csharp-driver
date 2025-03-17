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
    /// Represents a serializer for ImmutableQueues.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the collection.</typeparam>
    public class ImmutableQueueSerializer<T>: EnumerableInterfaceImplementerSerializerBase<ImmutableQueue<T>, T>
    {
        /// <inheritdoc/>
        protected override object CreateAccumulator()
        {
            return new List<T>();
        }

        /// <inheritdoc/>
        protected override ImmutableQueue<T> FinalizeResult(object accumulator)
        {
            return ImmutableQueue.CreateRange((List<T>)accumulator);
        }
    }
}
#endif