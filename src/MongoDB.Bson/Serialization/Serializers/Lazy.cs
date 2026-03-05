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
using System.Threading;

namespace MongoDB.Bson.Serialization.Serializers;

internal static class Lazy
{
    /// <summary>
    /// Creates a <see cref="Lazy{T}"/> with <see cref="LazyThreadSafetyMode.PublicationOnly"/>.
    /// </summary>
    /// <typeparam name="T">The type of the lazily initialized value.</typeparam>
    /// <param name="valueFactory">The factory delegate that produces the value.</param>
    /// <returns>A <see cref="Lazy{T}"/> configured with <see cref="LazyThreadSafetyMode.PublicationOnly"/>.</returns>
    public static Lazy<T> CreatePublicationOnly<T>(Func<T> valueFactory)
    {
        return new Lazy<T>(valueFactory, LazyThreadSafetyMode.PublicationOnly);
    }
}
