/* Copyright 2013-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc;

internal sealed class DefaultRandom : IRandom
{
    public static DefaultRandom Instance { get; } = new DefaultRandom();

    public string GenerateString(int length, string legalCharacters)
    {
        Ensure.IsGreaterThanOrEqualToZero(length, nameof(length));
        Ensure.IsNotNullOrEmpty(legalCharacters, nameof(legalCharacters));

        if (length == 0)
        {
            return string.Empty;
        }

#if NET472
        var randomData = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomData);
        }

        var sb = new System.Text.StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(GetResultChar(legalCharacters, randomData[i]));
        }

        return sb.ToString();
#else
        return string.Create(length, legalCharacters, (buffer, charset) =>
        {
            var randomData = buffer.Length < 1024 ? stackalloc byte[buffer.Length] : new byte[buffer.Length];
            RandomNumberGenerator.Fill(randomData);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = GetResultChar(charset, randomData[i]);
            }
        });
#endif

        static char GetResultChar(string charset, byte randomValue)
        {
            var pos = randomValue % charset.Length;
            return charset[pos];
        }
    }

    public double NextDouble()
    {
#if NET6_0_OR_GREATER
        return System.Random.Shared.NextDouble();
#else
        return ThreadStaticRandom.NextDouble();
#endif
    }
}
