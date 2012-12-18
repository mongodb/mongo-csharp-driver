using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MongoDB.Driver.Security.Gsasl
{
    /// <summary>
    /// A handle to a gsasl context.
    /// </summary>
    internal class GsaslContext : SafeHandle
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslContext" /> class.
        /// </summary>
        public GsaslContext()
            : base(IntPtr.Zero, true)
        {
        }

        // public static methods
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unable to initialize context.</exception>
        public static GsaslContext Initialize()
        {
            GsaslContext context;
            var rc = Gsasl.gsasl_init(out context);
            if (rc != Gsasl.GSASL_OK)
            {
                var message = Gsasl.GetError(rc);
                throw new GsaslException(rc, message);
            }

            return context;
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
            get { return base.IsClosed || handle == IntPtr.Zero; }
        }

        // public methods
        /// <summary>
        /// Begins the session.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        /// <returns>A GsaslSession.</returns>
        /// <exception cref="System.Exception">Unable to being session.</exception>
        public GsaslSession BeginSession(string mechanism)
        {
            GsaslSession session;
            var rc = Gsasl.gsasl_client_start(
                this,
                mechanism,
                out session);

            if (rc != Gsasl.GSASL_OK)
            {
                var message = Gsasl.GetError(rc);
                throw new GsaslException(rc, message);
            }

            return session;
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
            Gsasl.gsasl_done(handle);
            return true;
        }
    }
}