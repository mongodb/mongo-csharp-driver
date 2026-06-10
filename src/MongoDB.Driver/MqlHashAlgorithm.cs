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

namespace MongoDB.Driver;

/// <summary>
/// Specifies the hash algorithm for use with <see cref="Mql.Hash"/> and <see cref="Mql.HexHash"/> methods.
/// </summary>
public enum MqlHashAlgorithm
{
    /// <summary>
    /// Represents an undefined or default state for the <see cref="MqlHashAlgorithm"/> enumeration.
    /// This value indicates that no specific hash algorithm has been selected
    /// and serves as the default value of the enum.
    /// </summary>
    Undefined = 0,
    /// <summary>
    /// Represents the MD5 hashing algorithm.
    /// This algorithm is considered deprecated and is only included to support legacy use cases.
    /// </summary>
    [Obsolete("MD5 is deprecated and only supported for legacy purposes.")]
    MD5 = 1,
    /// <summary>
    /// Specifies the SHA-256 hash algorithm.
    /// </summary>
    SHA256 = 2,
    /// <summary>
    /// Represents the XXH64 hash algorithm.
    /// </summary>
    XXH64 = 3
}

internal static class HashAlgorithmExtensions
{
    public static string ToAlgorithmName(this MqlHashAlgorithm algorithm)
    {
        return algorithm switch
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MqlHashAlgorithm.MD5 => "md5",
#pragma warning restore CS0618 // Type or member is obsolete
            MqlHashAlgorithm.SHA256 => "sha256",
            MqlHashAlgorithm.XXH64 => "xxh64",
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Invalid hash algorithm.")
        };
    }
}
