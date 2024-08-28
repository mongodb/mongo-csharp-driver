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
    internal sealed class GssapiServicePrincipalName : GssapiSafeHandle
    {
        #region static
        public static GssapiServicePrincipalName Create(string service, string host, string realm)
        {
            var servicePrincipalName = $"{service}@{host}";
            if (!string.IsNullOrEmpty(realm))
            {
                servicePrincipalName += $"@{realm}";
            }

            using (var spnBuffer = new GssInputBuffer(servicePrincipalName))
            {
                var majorStatus = NativeMethods.gss_import_name(out var minorStatus, spnBuffer, in Oid.GSS_C_NT_HOSTBASED_SERVICE, out var spnName);
                Gss.ThrowIfError(majorStatus, minorStatus);
                return new GssapiServicePrincipalName(spnName);
            }
        }
        #endregion

        private GssapiServicePrincipalName(IntPtr spnName)
        {
            SetHandle(spnName);
        }

        protected override bool ReleaseHandle()
        {
            var majorStatus = NativeMethods.gss_release_name(out var minorStatus, handle);
            return majorStatus == 0 && minorStatus == 0;
        }
    }
}
