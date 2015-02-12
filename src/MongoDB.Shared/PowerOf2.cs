/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Shared
{
    internal static class PowerOf2
    {
        public static bool IsPowerOf2(int x)
        {
            return x == RoundUpToPowerOf2(x);
        }

        public static int RoundUpToPowerOf2(int x)
        {
            if (x < 0)
            {
                throw new ArgumentException("x is negative.", "x");
            }
            if (x > 0x40000000)
            {
                throw new ArgumentException("x is greater than 0x40000000.", "x");
            }

            // see: Hacker's Delight, by Henry S. Warren
            x = x - 1;
            x = x | (x >> 1);
            x = x | (x >> 2);
            x = x | (x >> 4);
            x = x | (x >> 8);
            x = x | (x >> 16);
            return x + 1;
        }
    }
}
