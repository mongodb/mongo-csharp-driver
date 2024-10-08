/*
 * Copyright 2020–present MongoDB, Inc.
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
    /// KMS Credentials.
    /// </summary>
    internal class KmsCredentials
    {
        private readonly byte[] _credentialsBytes;

        /// <summary>
        /// Creates a <see cref="KmsCredentials"/> class.
        /// </summary>
        /// <param name="credentialsBytes">The byte representation of credentials bson document.</param>
        public KmsCredentials(byte[] credentialsBytes)
        {
            _credentialsBytes = credentialsBytes ?? throw new ArgumentNullException(nameof(credentialsBytes));
        }

        // internal methods
        internal void SetCredentials(MongoCryptSafeHandle handle, Status status)
        {
            unsafe
            {
                fixed (byte* p = _credentialsBytes)
                {
                    IntPtr ptr = (IntPtr)p;
                    using (PinnedBinary pinned = new PinnedBinary(ptr, (uint)_credentialsBytes.Length))
                    {
                        handle.Check(status, Library.mongocrypt_setopt_kms_providers(handle, pinned.Handle));
                    }
                }
            }
        }
    }
}
