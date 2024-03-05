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

using System.Collections.Generic;

namespace MongoDB.Shared
{
    internal static class DictionaryComparer
    {
        public static bool Equals<TKey, TValue>(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null) || x.Count != y.Count)
            {
                return false;
            }

            foreach (var kv in x)
            {
                if (!y.TryGetValue(kv.Key, out var yValue) || !object.Equals(kv.Value, yValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
