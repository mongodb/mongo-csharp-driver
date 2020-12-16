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
    internal static class NativeMethods
    {
        private const string GSSAPI_LIBRARY = @"gssapi_krb5";

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_import_name")]
        public static extern uint ImportName(out uint minorStatus, ref GssInputBuffer name, ref Oid nameType, out IntPtr outputName);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_display_name")]
        public static extern uint DisplayName(out uint minorStatus, IntPtr inputName, out GssOutputBuffer outputBuffer, out Oid outputNameType);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_canonicalize_name")]
        public static extern uint CanonicalizeName(out uint minorStatus, IntPtr inputName, ref Oid mechType, out IntPtr outputName);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_acquire_cred_with_password")]
        public static extern uint AcquireCredentialWithPassword(out uint minorStatus, IntPtr name, ref GssInputBuffer password, uint timeRequested, IntPtr desiredMechanisms, GssCredentialUsage credentialUsage, out IntPtr credentialHandle, out OidSet actualMechanisms, out uint timeReceived);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_acquire_cred")]
        public static extern uint AcquireCredential(out uint minorStatus, IntPtr name, uint timeRequested, IntPtr desiredMechanisms, GssCredentialUsage credentialUsage, out IntPtr credentialHandle, out OidSet actualMechanisms, out uint timeReceived);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_init_sec_context")]
        public static extern uint InitializeSecurityContext(out uint minorStatus, IntPtr credentialHandle, ref IntPtr securityContextHandle, IntPtr spnName, IntPtr inputMechType, GssFlags requestFlags, uint timeRequested, IntPtr inputChannelBindings, ref GssInputBuffer inputToken, out IntPtr actualMechType, out GssOutputBuffer outputToken, out GssFlags returnedFlags, out uint timeReceived);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_display_status")]
        public static extern uint DisplayStatus(out uint minorStatus, uint status, GssCode statusType, ref IntPtr mechType, out uint messageContext, out GssOutputBuffer statusString);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_release_name")]
        public static extern uint ReleaseName(out uint minorStatus, IntPtr name);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_release_buffer")]
        public static extern uint ReleaseBuffer(out uint minorStatus, ref GssOutputBuffer buffer);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_release_cred")]
        public static extern uint ReleaseCredential(out uint minorStatus, IntPtr credentialHandle);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_delete_sec_context")]
        public static extern uint DeleteSecurityContext(out uint minorStatus, IntPtr securityContextHandle, IntPtr outputToken);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_wrap")]
        public static extern uint WrapMessage(out uint minorStatus, IntPtr securityContextHandle, int confidentialityRequested, int protectionType, ref GssInputBuffer inputBuffer, out int confidentialityState, out GssOutputBuffer outputBuffer);

        [DllImport(GSSAPI_LIBRARY, EntryPoint = "gss_unwrap")]
        public static extern uint UnwrapMessage(out uint minorStatus, IntPtr securityContextHandle, ref GssInputBuffer inputBuffer, out GssOutputBuffer outputBuffer, out int confidentialityState, out int qualityOfProtectionState);
    }
}
