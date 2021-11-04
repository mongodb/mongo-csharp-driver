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

namespace MongoDB.Driver.Core.Misc
{
    internal static class FisherYatesShuffle
    {
        public static void Shuffle<T>(IList<T> list)
        {
            Ensure.IsNotNull(list, nameof(list));

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = ThreadStaticRandom.Next(i + 1);
                if (i != j)
                {
                    (list[j], list[i]) = (list[i], list[j]);
                }
            }
        }
    }
}
