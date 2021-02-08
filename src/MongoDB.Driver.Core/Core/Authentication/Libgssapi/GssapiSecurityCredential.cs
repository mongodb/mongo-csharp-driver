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
using System.Security;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Authentication.Libgssapi
{
    internal sealed class GssapiSecurityCredential : GssapiSafeHandle
    {
        public static GssapiSecurityCredential Acquire(string username, SecureString password)
        {
            IntPtr gssName = IntPtr.Zero;
            try
            {
                using (var nameBuffer = new GssInputBuffer(username))
                {
                    uint minorStatus, majorStatus;
                    majorStatus = NativeMethods.ImportName(out minorStatus, nameBuffer, in Oid.NtUserName, out gssName);
                    Gss.ThrowIfError(majorStatus, minorStatus);

                    GssapiSecurityCredential securityCredential;
                    if (password != null)
                    {
                        using (var passwordBuffer = new GssInputBuffer(SecureStringHelper.ToInsecureString(password)))
                        {
                            majorStatus = NativeMethods.AcquireCredentialWithPassword(out minorStatus, gssName, passwordBuffer, uint.MaxValue, IntPtr.Zero, GssCredentialUsage.Initiate, out securityCredential, IntPtr.Zero, out uint _);
                        }
                    }
                    else
                    {
                        majorStatus = NativeMethods.AcquireCredential(out minorStatus, gssName, uint.MaxValue, IntPtr.Zero, GssCredentialUsage.Initiate, out securityCredential, IntPtr.Zero, out uint _);
                    }
                    Gss.ThrowIfError(majorStatus, minorStatus);
                    return securityCredential;
                }
            }
            finally
            {
                if (gssName != IntPtr.Zero)
                {
                    NativeMethods.ReleaseName(out _, gssName);
                }
            }
        }

        public static GssapiSecurityCredential Acquire(string username)
        {
            return Acquire(username, null);
        }

        private GssapiSecurityCredential()
        {
        }

        protected override bool ReleaseHandle()
        {
            var majorStatus = NativeMethods.ReleaseCredential(out var minorStatus, base.handle);
            return majorStatus != 0 && minorStatus != 0;
        }
    }
}
