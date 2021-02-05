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
    /// <summary>
    /// A wrapper around the Libgssapi structure specifically used as a credential handle.
    /// </summary>
    internal class GssapiSecurityCredential : GssapiSafeHandle
    {
        /// <summary>
        /// Acquires the TGT from the KDC with provided password or keytab (if password is null).
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static GssapiSecurityCredential Acquire(string username, SecureString password)
        {
            IntPtr gssName = IntPtr.Zero;
            try
            {
                GssInputBuffer nameBuffer;
                using (nameBuffer = new GssInputBuffer(username))
                {
                    uint minorStatus, majorStatus;
                    majorStatus = NativeMethods.ImportName(out minorStatus, ref nameBuffer, ref Oid.NtUserName, out gssName);
                    Gss.ThrowIfError(majorStatus, minorStatus);

                    GssapiSecurityCredential securityCredential;
                    if (password != null)
                    {
                        GssInputBuffer passwordBuffer;
                        using (passwordBuffer = new GssInputBuffer(SecureStringHelper.ToInsecureString(password)))
                        {
                            majorStatus = NativeMethods.AcquireCredentialWithPassword(out minorStatus, gssName, ref passwordBuffer, uint.MaxValue, IntPtr.Zero, GssCredentialUsage.Initiate, out securityCredential, out OidSet _, out uint _);
                        }
                    }
                    else
                    {
                        majorStatus = NativeMethods.AcquireCredential(out minorStatus, gssName, uint.MaxValue, IntPtr.Zero, GssCredentialUsage.Initiate, out securityCredential, out OidSet _, out uint _);
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

        /// <summary>
        /// Acquires the TGT from the KDC using keytab.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static GssapiSecurityCredential Acquire(string username)
        {
            return Acquire(username, null);
        }

        private GssapiSecurityCredential()
        {
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            uint majorStatus = NativeMethods.ReleaseCredential(out uint minorStatus, base.handle);
            return majorStatus != 0 && minorStatus != 0;
        }
    }
}
