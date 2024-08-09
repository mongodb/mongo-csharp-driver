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

namespace MongoDB.Driver.Authentication.Gssapi.Libgssapi
{
    internal sealed class GssapiSecurityContext : GssapiSafeHandle, ISecurityContext
    {
        private readonly GssapiSecurityCredential _credential;
        private bool _isInitialized;
        private readonly GssapiServicePrincipalName _servicePrincipalName;

        public GssapiSecurityContext(GssapiServicePrincipalName servicePrincipalName, GssapiSecurityCredential credential)
        {
            _servicePrincipalName = servicePrincipalName;
            _credential = credential;
        }

        public bool IsInitialized => _isInitialized;

        public byte[] Next(byte[] challenge)
        {
            using var inputToken = new GssInputBuffer(challenge);
            using var outputToken = new GssOutputBuffer();
            const GssFlags authenticationFlags = GssFlags.GSS_C_MUTUAL_FLAG | GssFlags.GSS_C_SEQUENCE_FLAG;
            var majorStatus = NativeMethods.gss_init_sec_context(out var minorStatus, _credential, in handle, _servicePrincipalName, IntPtr.Zero, authenticationFlags, 0, IntPtr.Zero, inputToken, out var _, outputToken, out var _, out var _);
            Gss.ThrowIfError(majorStatus, minorStatus);

            _isInitialized = true;
            return outputToken.ToByteArray();
        }

        public byte[] DecryptMessage(int messageLength, byte[] encryptedBytes)
        {
            using var inputBuffer = new GssInputBuffer(encryptedBytes);
            using var outputBuffer = new GssOutputBuffer();
            var majorStatus = NativeMethods.gss_unwrap(out uint minorStatus, handle, inputBuffer, outputBuffer, out int _, out int _);
            Gss.ThrowIfError(majorStatus, minorStatus);
            return outputBuffer.ToByteArray();
        }

        public byte[] EncryptMessage(byte[] plainTextBytes)
        {
            using var inputBuffer = new GssInputBuffer(plainTextBytes);
            using var outputBuffer = new GssOutputBuffer();
            var majorStatus = NativeMethods.gss_wrap(out uint minorStatus, handle, 0, 0, inputBuffer, out int _, outputBuffer);
            Gss.ThrowIfError(majorStatus, minorStatus);
            return outputBuffer.ToByteArray();
        }

        protected override bool ReleaseHandle()
        {
            var majorStatus = NativeMethods.gss_delete_sec_context(out var minorStatus, ref handle, IntPtr.Zero);
            return majorStatus == 0 && minorStatus == 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _servicePrincipalName?.Dispose();
                _credential?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
