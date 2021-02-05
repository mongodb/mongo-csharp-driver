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

namespace MongoDB.Driver.Core.Authentication.Libgssapi
{
    internal sealed class GssapiSecurityContext : GssapiSafeHandle, ISecurityContext
    {
        private GssapiServicePrincipalName _servicePrincipalName;
        private GssapiSecurityCredential _credential;

        public bool IsInitialized { get; private set; }

        public GssapiSecurityContext(GssapiServicePrincipalName servicePrincipalName, GssapiSecurityCredential credential)
        {
            _servicePrincipalName = servicePrincipalName;
            _credential = credential;
        }

        public byte[] Next(byte[] challenge)
        {
            GssOutputBuffer outputToken = new GssOutputBuffer();
            try
            {
                using (var inputToken = new GssInputBuffer(challenge))
                {
                    uint majorStatus, minorStatus;

                    const GssFlags authenticationFlags = GssFlags.Mutual | GssFlags.Sequence;
                    majorStatus = NativeMethods.InitializeSecurityContext(out minorStatus, _credential, in handle, _servicePrincipalName, IntPtr.Zero, authenticationFlags, 0, IntPtr.Zero, inputToken, out var _, out outputToken, out var _, out var _);
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
                using (var inputBuffer = new GssInputBuffer(encryptedBytes))
                {
                    var majorStatus = NativeMethods.UnwrapMessage(out uint minorStatus, handle, inputBuffer, out outputBuffer, out int _, out int _);
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
                using (var inputBuffer = new GssInputBuffer(plainTextBytes))
                {
                    var majorStatus = NativeMethods.WrapMessage(out uint minorStatus, handle, 0, 0, inputBuffer, out int _, out outputBuffer);
                    Gss.ThrowIfError(majorStatus, minorStatus);
                    return outputBuffer.ToByteArray();
                }
            }
            finally
            {
                outputBuffer.Dispose();
            }
        }

        protected override bool ReleaseHandle()
        {
            uint majorStatus = NativeMethods.DeleteSecurityContext(out uint minorStatus, handle, IntPtr.Zero);
            return majorStatus == 0 && minorStatus == 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _servicePrincipalName?.Dispose();
                _servicePrincipalName = null;
                _credential?.Dispose();
                _credential = null;
            }
            base.Dispose(disposing);
        }
    }
}
