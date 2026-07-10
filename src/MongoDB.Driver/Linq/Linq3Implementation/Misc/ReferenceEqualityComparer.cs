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
using System.Collections.Generic;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        #region static
        public static ReferenceEqualityComparer<T> Instance { get; } = new ReferenceEqualityComparer<T>();
        #endregion

        public bool Equals(T x, T y) => object.ReferenceEquals(x, y);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "This comparer supports reference equality only and is never used for hashing.")]
        public int GetHashCode(T obj) => throw new InvalidOperationException();
    }
}
