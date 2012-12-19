using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// A wrapper around the SspiHandle structure specificly used as a credentials handle.
    /// </summary>
    internal class SecurityCredentials : SafeHandle
    {
        // internal fields
        internal SspiHandle _sspiHandle;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityCredentials" /> class.
        /// </summary>
        public SecurityCredentials()
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
        /// Acquires the credentials handle.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="identity">The identity.</param>
        /// <returns>A security credential.</returns>
        public static SecurityCredentials Acquire(SspiPackage package, MongoClientIdentity identity)
        {
            long timestamp;

            var credentials = new SecurityCredentials();
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                uint result;
                if (identity is SystemMongoClientIdentity)
                {
                    result = Win32.AcquireCredentialsHandle(
                        null,
                        package.ToString(),
                        SecurityCredentialUse.Outbound,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        0,
                        IntPtr.Zero,
                        ref credentials._sspiHandle,
                        out timestamp);

                }
                else
                {
                    using(var authIdentity = new AuthIdentity(identity))
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
                            ref credentials._sspiHandle,
                            out timestamp);
                    }
                }
                if (result != Win32.SEC_E_OK)
                {
                    credentials.SetHandleAsInvalid();
                    throw Win32.CreateException(result, "Unable to acquire credentials.");
                }
            }
            return credentials;
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