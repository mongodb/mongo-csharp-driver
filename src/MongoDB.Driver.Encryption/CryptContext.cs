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
using System.Collections.Generic;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// A encryption or decryption session. It may not be reused.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="IStatus" />
    internal class CryptContext : IDisposable, IStatus
    {
        /// <summary>
        /// States of the CryptContext state machine
        /// </summary>
        public enum StateCode
        {
            /// <summary>
            /// LibMongoCrypt hit an error
            /// </summary>
            MONGOCRYPT_CTX_ERROR = 0,

            /// <summary>
            /// LibMongoCrypt wants the collection information by running a OP_MSG command against the users' mongod.
            /// </summary>
            MONGOCRYPT_CTX_NEED_MONGO_COLLINFO = 1,

            /// <summary>
            /// LibMongoCrypt wants a command to be run against mongocryptd.
            /// </summary>
            MONGOCRYPT_CTX_NEED_MONGO_MARKINGS = 2,

            /// <summary>
            /// LibMongoCrypt wants a command to be run against mongod key vault.
            /// </summary>
            MONGOCRYPT_CTX_NEED_MONGO_KEYS = 3,

            /// <summary>
            /// LibMongoCrypt wants a request sent to KMS.
            /// </summary>
            MONGOCRYPT_CTX_NEED_KMS = 4,

            /// <summary>
            /// LibMongoCrypt is ready to do encryption, call Finish().
            /// </summary>
            MONGOCRYPT_CTX_READY = 5,

            /// <summary>
            /// LibMongoCrypt is complete.
            /// </summary>
            MONGOCRYPT_CTX_DONE = 6,

            /// <summary>
            /// LibMongoCrypt requires new credentials.
            /// </summary>
            MONGOCRYPT_CTX_NEED_KMS_CREDENTIALS = 7
        }

        private readonly ContextSafeHandle _handle;
        private readonly Status _status;

        internal CryptContext(ContextSafeHandle handle)
        {
            _handle = handle;
            _status = new Status();
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public StateCode State
        {
            get
            {
                return Library.mongocrypt_ctx_state(_handle);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is done.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is done; otherwise, <c>false</c>.
        /// </value>
        public bool IsDone
        {
            get { return State == StateCode.MONGOCRYPT_CTX_DONE; }
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <returns>Binary payload to send to either KMS, mongod, or mongocryptd</returns>
        public Binary GetOperation()
        {
            Binary binary = new Binary();
            Check(Library.mongocrypt_ctx_mongo_op(_handle, binary.Handle));
            return binary;
        }

        /// <summary>
        /// Feeds the result from running a remote operation back to the libmongocrypt.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public void Feed(byte[] buffer)
        {
            unsafe
            {
                fixed (byte* p = buffer)
                {
                    IntPtr ptr = (IntPtr)p;
                    using (PinnedBinary pinned = new PinnedBinary(ptr, (uint)buffer.Length))
                    {
                        Check(Library.mongocrypt_ctx_mongo_feed(_handle, pinned.Handle));
                    }
                }
            }
        }

        /// <summary>
        /// Signal the feeding is done.
        /// </summary>
        public void MarkDone()
        {
            Check(Library.mongocrypt_ctx_mongo_done(_handle));
        }

        /// <summary>
        /// Finalizes for encryption.
        /// </summary>
        /// <returns>The encrypted or decrypted result.</returns>
        public Binary FinalizeForEncryption()
        {
            Binary binary = new Binary();
            Check(Library.mongocrypt_ctx_finalize(_handle, binary.Handle));
            return binary;
        }

        /// <summary>
        /// Gets a collection of KMS message requests to make
        /// </summary>
        /// <returns>Collection of KMS Messages</returns>
        public KmsRequestCollection GetKmsMessageRequests()
        {
            var requests = new List<KmsRequest>();
            for (IntPtr request = Library.mongocrypt_ctx_next_kms_ctx(_handle); request != IntPtr.Zero; request = Library.mongocrypt_ctx_next_kms_ctx(_handle))
            {
                requests.Add(new KmsRequest(request));
            }

            return new KmsRequestCollection(requests, this);
        }

        /// <summary>
        /// Sets the KMS credentials
        /// </summary>
        public void SetCredentials(byte[] credentials)
        {
            PinnedBinary.RunAsPinnedBinary(_handle, credentials, _status, (h, b) => Library.mongocrypt_ctx_provide_kms_providers(h, b));
        }

        void IStatus.Check(Status status)
        {
            Library.mongocrypt_ctx_status(_handle, status.Handle);
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Adapted from: https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.safehandle?view=netcore-3.0
            if (_handle != null && !_handle.IsInvalid)
            {
                // Free the handle
                _handle.Dispose();
                _status.Dispose();
            }
        }
        #endregion

        internal void MarkKmsDone()
        {
            Check(Library.mongocrypt_ctx_kms_done(_handle));
        }

        private void Check(bool success)
        {
            if (!success)
            {
                _status.Check(this);
            }
        }
    }
}
