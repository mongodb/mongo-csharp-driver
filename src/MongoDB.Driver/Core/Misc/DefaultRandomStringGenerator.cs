﻿/* Copyright 2013-present MongoDB Inc.
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

using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Driver.Core.Misc
{
    internal class DefaultRandomStringGenerator : IRandomStringGenerator
    {
        public static DefaultRandomStringGenerator Instance { get; } = new DefaultRandomStringGenerator();

        public string Generate(int length, string legalCharacters)
        {
            var randomData = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomData);
            }

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int pos = randomData[i] % legalCharacters.Length;
                sb.Append(legalCharacters[pos]);
            }

            return sb.ToString();
        }
    }
}
