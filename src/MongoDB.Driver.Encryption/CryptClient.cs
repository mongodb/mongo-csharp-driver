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
    /// CryptClient represents a session with libmongocrypt.
    ///
    /// It can be used to encrypt and decrypt documents.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class CryptClient : IDisposable, IStatus
    {
        private readonly MongoCryptSafeHandle _handle;
        private readonly Status _status;

        internal CryptClient(MongoCryptSafeHandle handle, Status status)
        {
            _handle = handle ?? throw new ArgumentNullException(paramName: nameof(handle));
            _status = status ?? throw new ArgumentNullException(paramName: nameof(status));
        }

        /// <summary>
        /// Gets the crypt shared library version.
        /// </summary>
        /// <returns>A crypt shared library version.</returns>
        public string CryptSharedLibraryVersion
        {
            get
            {
                var versionPtr = Library.mongocrypt_crypt_shared_lib_version_string(_handle, out _);
                var result = Marshal.PtrToStringAnsi(versionPtr);

                return result;
            }
        }

        /// <summary>
        /// Starts the create data key context.
        /// </summary>
        /// <param name="keyId">The key identifier.</param>
        /// <returns>A crypt context for creating a data key</returns>
        public CryptContext StartCreateDataKeyContext(KmsKeyId keyId)
        {
            ContextSafeHandle handle = Library.mongocrypt_ctx_new(_handle);

            keyId.SetCredentials(handle, _status);

            handle.Check(_status, Library.mongocrypt_ctx_datakey_init(handle));

            return new CryptContext(handle);
        }

        /// <summary>
        /// Starts the encryption context.
        /// </summary>
        /// <param name="db">The database of the collection.</param>
        /// <param name="command">The command.</param>
        /// <returns>A encryption context.</returns>
        public CryptContext StartEncryptionContext(string db, byte[] command)
        {
            ContextSafeHandle handle = Library.mongocrypt_ctx_new(_handle);

            var stringPointer = Marshal.StringToHGlobalAnsi(db);

            try
            {
                unsafe
                {
                    fixed (byte* c = command)
                    {
                        var commandPtr = (IntPtr)c;
                        using (var pinnedCommand = new PinnedBinary(commandPtr, (uint)command.Length))
                        {
                            // Let mongocrypt run strlen
                            handle.Check(_status, Library.mongocrypt_ctx_encrypt_init(handle, stringPointer, -1, pinnedCommand.Handle));
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(stringPointer);
            }

            return new CryptContext(handle);
        }

        /// <summary>
        /// Starts an explicit encryption context.
        /// </summary>
        public CryptContext StartExplicitEncryptionContext(byte[] keyId, byte[] keyAltName, string queryType, long? contentionFactor, string encryptionAlgorithm, byte[] message, byte[] rangeOptions, bool isExpressionMode = false)
        {
            var handle = Library.mongocrypt_ctx_new(_handle);

            if (keyId != null)
            {
                PinnedBinary.RunAsPinnedBinary(handle, keyId, _status, (h, pb) => Library.mongocrypt_ctx_setopt_key_id(h, pb));
            }
            else if (keyAltName != null)
            {
                PinnedBinary.RunAsPinnedBinary(handle, keyAltName, _status, (h, pb) => Library.mongocrypt_ctx_setopt_key_alt_name(h, pb));
            }

            if (rangeOptions != null)
            {
                PinnedBinary.RunAsPinnedBinary(handle, rangeOptions, _status, (h, pb) => Library.mongocrypt_ctx_setopt_algorithm_range(h, pb));
            }

            handle.Check(_status, Library.mongocrypt_ctx_setopt_algorithm(handle, encryptionAlgorithm, -1));

            if (queryType != null)
            {
                handle.Check(_status, Library.mongocrypt_ctx_setopt_query_type(handle, queryType, -1));
            }

            if (contentionFactor.HasValue)
            {
                var contentionFactorInt = contentionFactor.Value;
                handle.Check(_status, Library.mongocrypt_ctx_setopt_contention_factor(handle, contentionFactorInt));
            }

            PinnedBinary.RunAsPinnedBinary(
                handle,
                message,
                _status,
                (h, pb) =>
                {
                    if (isExpressionMode)
                    {
                        // mongocrypt_ctx_explicit_encrypt_expression_init shares the same code as mongocrypt_ctx_explicit_encrypt_init.
                        // The only difference is the validation of the queryType argument:
                        // * mongocrypt_ctx_explicit_encrypt_expression_init requires queryType of "rangePreview".
                        // * mongocrypt_ctx_explicit_encrypt_init rejects queryType of "rangePreview".
                        return Library.mongocrypt_ctx_explicit_encrypt_expression_init(h, pb);
                    }
                    else
                    {
                        return Library.mongocrypt_ctx_explicit_encrypt_init(h, pb);
                    }
                });

            return new CryptContext(handle);
        }

        /// <summary>
        /// Starts the decryption context.
        /// </summary>
        /// <param name="buffer">The bson document to decrypt.</param>
        /// <returns>A decryption context</returns>
        public CryptContext StartDecryptionContext(byte[] buffer)
        {
            ContextSafeHandle handle = Library.mongocrypt_ctx_new(_handle);

            unsafe
            {
                fixed (byte* p = buffer)
                {
                    IntPtr ptr = (IntPtr)p;
                    using (PinnedBinary pinned = new PinnedBinary(ptr, (uint)buffer.Length))
                    {
                        handle.Check(_status, Library.mongocrypt_ctx_decrypt_init(handle, pinned.Handle));
                    }
                }
            }

            return new CryptContext(handle);
        }

        /// <summary>
        /// Starts an explicit decryption context.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>A encryption context</returns>
        public CryptContext StartExplicitDecryptionContext(byte[] buffer)
        {
            ContextSafeHandle handle = Library.mongocrypt_ctx_new(_handle);

            unsafe
            {
                fixed (byte* p = buffer)
                {
                    IntPtr ptr = (IntPtr)p;
                    using (PinnedBinary pinned = new PinnedBinary(ptr, (uint)buffer.Length))
                    {
                        // Let mongocrypt run strlen
                        handle.Check(_status, Library.mongocrypt_ctx_explicit_decrypt_init(handle, pinned.Handle));
                    }
                }
            }

            return new CryptContext(handle);
        }

        public CryptContext StartRewrapMultipleDataKeysContext(KmsKeyId kmsKey, byte[] filter)
        {
            var handle = Library.mongocrypt_ctx_new(_handle);

            kmsKey.SetCredentials(handle, _status);

            PinnedBinary.RunAsPinnedBinary(handle, filter, _status, (h, pb) => Library.mongocrypt_ctx_rewrap_many_datakey_init(h, pb));

            return new CryptContext(handle);
        }

        void IStatus.Check(Status status)
        {
            Library.mongocrypt_status(_handle, status.Handle);
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
            }

            // Free the status
            _status.Dispose();
        }
        #endregion

        // private methods
        private void Check(bool success)
        {
            if (!success)
            {
                _status.Check(this);
            }
        }
    }
}
