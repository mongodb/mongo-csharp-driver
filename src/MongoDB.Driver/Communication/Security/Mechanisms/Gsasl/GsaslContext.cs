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
using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Driver.Communication.Security.Mechanisms.Gsasl
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