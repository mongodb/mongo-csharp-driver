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
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Contains a KMS request to make to a remote server.
    /// </summary>
    /// <seealso cref="IStatus" />
    internal class KmsRequest : IStatus, IDisposable
    {
        private readonly Status _status;
        private readonly IntPtr _id;
        private bool disposed = false;

        internal KmsRequest(IntPtr id)
        {
            _id = id;
            _status = new Status();
        }

        /// <summary>
        /// Gets the bytes needed from the remote side. No more data is need when this returns 0.
        /// </summary>
        /// <value>
        /// The number of bytes needed.
        /// </value>
        public uint BytesNeeded
        {
            get { return Library.mongocrypt_kms_ctx_bytes_needed(_id); }
        }

        /// <summary>
        /// Gets the endpoint.
        /// </summary>
        /// <value>
        /// The endpoint.
        /// </value>
        public string Endpoint
        {
            get
            {
                IntPtr stringPointer = IntPtr.Zero;
                Check(Library.mongocrypt_kms_ctx_endpoint(_id, ref stringPointer));
                return Marshal.PtrToStringAnsi(stringPointer);
            }
        }

        /// <summary>
        /// Gets the kms provider name.
        /// </summary>
        /// <value>
        /// The kms provider name.
        /// </value>
        public string KmsProvider
        {
            get
            {
                var kmsProviderNamePointer = Library.mongocrypt_kms_ctx_get_kms_provider(_id, length: out _);
                return Marshal.PtrToStringAnsi(kmsProviderNamePointer);
            }
        }

        /// <summary>
        /// Gets the message to send to KMS.
        /// </summary>
        /// <returns>The message</returns>
        public Binary GetMessage()
        {
            Binary binary = new Binary();
            Check(Library.mongocrypt_kms_ctx_message(_id, binary.Handle));
            return binary;
        }

        /// <summary>
        /// Feeds the response back to the libmongocrypt
        /// </summary>
        /// <param name="buffer">The response.</param>
        public void Feed(byte[] buffer)
        {
                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        IntPtr ptr = (IntPtr)p;
                        using (PinnedBinary pinned = new PinnedBinary(ptr, (uint)buffer.Length))
                        {
                            Check(Library.mongocrypt_kms_ctx_feed(_id, pinned.Handle));
                        }
                    }
                }
        }

        void IStatus.Check(Status status)
        {
            Library.mongocrypt_kms_ctx_status(_id, status.Handle);
        }

        private void Check(bool success)
        {
            if (!success)
            {
                _status.Check(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _status?.Dispose();
                }
                disposed = true;
            }
        }
    }
}
