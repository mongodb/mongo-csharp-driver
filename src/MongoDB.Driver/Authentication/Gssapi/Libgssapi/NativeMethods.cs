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

namespace MongoDB.Driver.Authentication.Gssapi.Libgssapi
{
    internal static class NativeMethods
    {
        private const string GSSAPI_LIBRARY = @"gssapi_krb5";

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_import_name(out uint minorStatus, GssInputBuffer name, in Oid nameType, out IntPtr outputName);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_display_name(out uint minorStatus, IntPtr inputName, [MarshalAs(UnmanagedType.LPStruct)] GssOutputBuffer outputBuffer, out Oid outputNameType);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_canonicalize_name(out uint minorStatus, IntPtr inputName, in Oid mechType, out IntPtr outputName);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_acquire_cred_with_password(out uint minorStatus, IntPtr name, GssInputBuffer password, uint timeRequested, IntPtr desiredMechanisms, GssCredentialUsage credentialUsage, out GssapiSecurityCredential securityCredential, IntPtr actualMechanisms, out uint timeReceived);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_acquire_cred(out uint minorStatus, IntPtr name, uint timeRequested, IntPtr desiredMechanisms, GssCredentialUsage credentialUsage, out GssapiSecurityCredential securityCredential, IntPtr actualMechanisms, out uint timeReceived);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_init_sec_context(out uint minorStatus, GssapiSecurityCredential securityCredential, in IntPtr securityContextHandle, GssapiServicePrincipalName spnName, IntPtr inputMechType, GssFlags requestFlags, uint timeRequested, IntPtr inputChannelBindings, GssInputBuffer inputToken, out IntPtr actualMechType, [MarshalAs(UnmanagedType.LPStruct)] GssOutputBuffer outputToken, out GssFlags returnedFlags, out uint timeReceived);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_display_status(out uint minorStatus, uint status, GssCode statusType, in IntPtr mechType, out uint messageContext, [MarshalAs(UnmanagedType.LPStruct)] GssOutputBuffer statusString);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_release_name(out uint minorStatus, IntPtr name);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_release_buffer(out uint minorStatus, GssOutputBuffer buffer);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_release_cred(out uint minorStatus, IntPtr credentialHandle);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_delete_sec_context(out uint minorStatus, ref IntPtr securityContextHandle, IntPtr outputToken);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_wrap(out uint minorStatus, IntPtr securityContextHandle, int confidentialityRequested, int protectionType, GssInputBuffer inputBuffer, out int confidentialityState, [MarshalAs(UnmanagedType.LPStruct)] GssOutputBuffer outputBuffer);

        [DllImport(GSSAPI_LIBRARY)]
        public static extern uint gss_unwrap(out uint minorStatus, IntPtr securityContextHandle, GssInputBuffer inputBuffer, [MarshalAs(UnmanagedType.LPStruct)] GssOutputBuffer outputBuffer, out int confidentialityState, out int qualityOfProtectionState);
    }
}
