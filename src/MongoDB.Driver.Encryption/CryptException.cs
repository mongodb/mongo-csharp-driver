/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// An exception from libmongocrypt.
    /// </summary>
    /// <seealso cref="System.Exception" />
#pragma warning disable CA1032
    public class CryptException : Exception
#pragma warning restore CA1032
    {
        private readonly uint _code;
        private readonly Library.StatusType _statusType;

        internal CryptException(Library.StatusType statusType, uint code, string message)
            : base(message)
        {
            _code = code;
            _statusType = statusType;
        }
    }
}
