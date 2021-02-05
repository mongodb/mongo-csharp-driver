/* Copyright 2021-present MongoDB Inc.
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
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Core.Authentication.Libgssapi
{
    internal class GssapiSecurityContext : SafeHandle, ISecurityContext
    {
        private GssapiServicePrincipalName _servicePrincipalName;
        private GssapiSecurityCredential _credential;
        private bool _isDisposed;

        public bool IsInitialized { get; private set; }

        public override bool IsInvalid
        {
            get { return base.IsClosed || handle == IntPtr.Zero; }
        }

        public GssapiSecurityContext(GssapiServicePrincipalName servicePrincipalName, GssapiSecurityCredential credential) : base(IntPtr.Zero, true)
        {
            _servicePrincipalName = servicePrincipalName;
            _credential = credential;
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                var majorStatus = NativeMethods.DeleteSecurityContext(out var minorStatus, handle, IntPtr.Zero);
                return majorStatus == 0 && minorStatus == 0;
            }

            return true;
        }

        public byte[] Next(byte[] challenge)
        {
            GssOutputBuffer outputToken = new GssOutputBuffer();
            try
            {
                GssInputBuffer inputToken;
                using (inputToken = new GssInputBuffer(challenge))
                {
                    uint majorStatus, minorStatus;

                    const GssFlags authenticationFlags = GssFlags.Mutual | GssFlags.Sequence;
                    majorStatus = NativeMethods.InitializeSecurityContext(out minorStatus, _credential, ref handle, _servicePrincipalName, IntPtr.Zero, authenticationFlags, 0, IntPtr.Zero, ref inputToken, out var _, out outputToken, out var _, out var _);
                    Gss.ThrowIfError(majorStatus, minorStatus);

                    IsInitialized = true;
                    return outputToken.ToByteArray();
                }
            }
            finally
            {
                outputToken.Dispose();
            }
        }

        public byte[] DecryptMessage(int messageLength, byte[] encryptedBytes)
        {
            GssOutputBuffer outputBuffer = new GssOutputBuffer();
            try
            {
                GssInputBuffer inputBuffer;
                using (inputBuffer = new GssInputBuffer(encryptedBytes))
                {
                    var majorStatus = NativeMethods.UnwrapMessage(out uint minorStatus, handle, ref inputBuffer, out outputBuffer, out int _, out int _);
                    Gss.ThrowIfError(majorStatus, minorStatus);
                    return outputBuffer.ToByteArray();
                }
            }
            finally
            {
                outputBuffer.Dispose();
            }
        }

        public byte[] EncryptMessage(byte[] plainTextBytes)
        {
            GssOutputBuffer outputBuffer = new GssOutputBuffer();
            try
            {
                GssInputBuffer inputBuffer;
                using (inputBuffer = new GssInputBuffer(plainTextBytes))
                {
                    var majorStatus = NativeMethods.WrapMessage(out uint minorStatus, handle, 0, 0, ref inputBuffer, out int _, out outputBuffer);
                    Gss.ThrowIfError(majorStatus, minorStatus);
                    return outputBuffer.ToByteArray();
                }
            }
            finally
            {
                outputBuffer.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _servicePrincipalName?.Dispose();
                _servicePrincipalName = null;
                _credential?.Dispose();
                _credential = null;
                ReleaseHandle();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
