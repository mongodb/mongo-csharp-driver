/* Copyright 2010-2014 MongoDB Inc.
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Driver.Communication.Security.Mechanisms.Sspi
{
    /// <summary>
    /// A wrapper around the SspiHandle structure specificly used as a credential handle.
    /// </summary>
    internal class SecurityCredential : SafeHandle
    {
        // internal fields
        internal SspiHandle _sspiHandle;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityCredential" /> class.
        /// </summary>
        public SecurityCredential()
            : base(IntPtr.Zero, true)
        {
            _sspiHandle = new SspiHandle();
        }

        // public properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the handle value is invalid.
        /// </summary>
        /// <returns>true if the handle value is invalid; otherwise, false.</returns>
        ///   <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode" />
        ///   </PermissionSet>
        public override bool IsInvalid
        {
            get { return base.IsClosed || _sspiHandle.IsZero; }
        }

        // public methods
        /// <summary>
        /// Acquires the credential handle.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="username">The username.</param>
        /// <param name="evidence">The evidence.</param>
        /// <returns>A security credential.</returns>
        public static SecurityCredential Acquire(SspiPackage package, string username, MongoIdentityEvidence evidence)
        {
            long timestamp;

            var credential = new SecurityCredential();
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                uint result;
                var passwordEvidence = evidence as PasswordEvidence;
                if (passwordEvidence == null)
                {
                    result = Win32.AcquireCredentialsHandle(
                        null,
                        package.ToString(),
                        SecurityCredentialUse.Outbound,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        0,
                        IntPtr.Zero,
                        ref credential._sspiHandle,
                        out timestamp);

                }
                else
                {
                    using(var authIdentity = new AuthIdentity(username, passwordEvidence.SecurePassword))
                    {
                        // TODO: make this secure by using SecurePassword
                        result = Win32.AcquireCredentialsHandle(
                            null,
                            package.ToString(),
                            SecurityCredentialUse.Outbound,
                            IntPtr.Zero,
                            authIdentity,
                            0,
                            IntPtr.Zero,
                            ref credential._sspiHandle,
                            out timestamp);
                    }
                }
                if (result != Win32.SEC_E_OK)
                {
                    credential.SetHandleAsInvalid();
                    throw Win32.CreateException(result, "Unable to acquire credential.");
                }
            }
            return credential;
        }

        // protected methods
        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        /// <returns>
        /// true if the handle is released successfully; otherwise, in the event of a catastrophic failure, false. In this case, it generates a releaseHandleFailed MDA Managed Debugging Assistant.
        /// </returns>
        protected override bool ReleaseHandle()
        {
            return Win32.FreeCredentialsHandle(ref _sspiHandle) == 0;
        }
    }
}