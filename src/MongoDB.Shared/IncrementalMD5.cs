/* Copyright 2016-present MongoDB Inc.
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
using System.Security.Cryptography;

namespace MongoDB.Shared
{
    internal abstract class IncrementalMD5 : IDisposable
    {
        public static IncrementalMD5 Create()
        {
            return new IncrementalMD5Imp();
        }

        public abstract void AppendData(byte[] data, int offset, int count);
        public abstract void Dispose();
        public abstract byte[] GetHashAndReset();
    }

    internal class IncrementalMD5Imp : IncrementalMD5
    {
        private readonly IncrementalHash _incrementalHash;

        public IncrementalMD5Imp()
        {
            _incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        }

        public override void AppendData(byte[] data, int offset, int count)
        {
            _incrementalHash.AppendData(data, offset, count);
        }

        public override void Dispose()
        {
            _incrementalHash.Dispose();
        }

        public override byte[] GetHashAndReset()
        {
            return _incrementalHash.GetHashAndReset();
        }
    }
}
