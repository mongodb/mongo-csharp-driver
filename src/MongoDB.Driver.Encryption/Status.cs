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
    /// A LibMongoCrypt Status
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class Status : IDisposable
    {
        private readonly StatusSafeHandle _handle;

        public Status()
        {
            _handle = Library.mongocrypt_status_new();
        }

        public Status(StatusSafeHandle handle)
        {
            _handle = handle;
        }

        public void Check(IStatus status)
        {
            status.Check(this);
            ThrowExceptionIfNeeded();
        }

        public void SetStatus(uint code, string msg)
        {
            var stringPointer = Marshal.StringToHGlobalAnsi(msg);
            try
            {
                Library.mongocrypt_status_set(_handle, (int)Library.StatusType.MONGOCRYPT_STATUS_ERROR_CLIENT, code, stringPointer, -1);
            }
            finally
            {
                Marshal.FreeHGlobal(stringPointer);
            }
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
        }
        #endregion

        internal StatusSafeHandle Handle => _handle;

        internal void ThrowExceptionIfNeeded()
        {
            if (!Library.mongocrypt_status_ok(_handle))
            {
                var statusType = Library.mongocrypt_status_type(_handle);
                var statusCode = Library.mongocrypt_status_code(_handle);

                uint length;
                IntPtr msgPtr = Library.mongocrypt_status_message(_handle, out length);
                var message = Marshal.PtrToStringAnsi(msgPtr);

                throw new CryptException(statusType, statusCode, message);
            }
        }
    }
}
