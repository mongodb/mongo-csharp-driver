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
    /// A handle to a gsasl session.
    /// </summary>
    internal class GsaslSession : SafeHandle
    {
        // private fields
        private bool _isComplete;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslSession" /> class.
        /// </summary>
        public GsaslSession()
            : base(IntPtr.Zero, true)
        {
        }

        // public properties
        /// <summary>
        /// Gets a value indicating whether this instance is complete.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
        /// </value>
        public bool IsComplete
        {
            get { return _isComplete; }
        }

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
        /// Sets the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="MongoSecurityException"></exception>
        public void SetProperty(string name, string value)
        {
            if(!name.StartsWith("GSASL_", StringComparison.InvariantCultureIgnoreCase))
            {
                name = "GSASL_" + name;
            }

            GsaslProperty prop;
            try
            {
                prop = (GsaslProperty)Enum.Parse(typeof(GsaslProperty), name, true);
            }
            catch (ArgumentException)
            {
                throw new MongoSecurityException(string.Format("The name {0} is not a valid name.", name));
            }

            Gsasl.gsasl_property_set(this, prop, value);
        }

        /// <summary>
        /// Steps the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The output bytes to be sent to the server.</returns>
        /// <exception cref="System.Exception"></exception>
        public byte[] Step(byte[] input)
        {
            IntPtr inputPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            int outputLength;
            try
            {
                inputPtr = Marshal.AllocHGlobal(input.Length);
                Marshal.Copy(input, 0, inputPtr, input.Length);

                var rc = Gsasl.gsasl_step(this, inputPtr, input.Length, out outputPtr, out outputLength);
                if (rc != Gsasl.GSASL_OK && rc != Gsasl.GSASL_NEEDS_MORE)
                {
                    var message = Gsasl.GetError(rc);
                    throw new GsaslException(rc, message);
                }

                _isComplete = rc == Gsasl.GSASL_OK;

                var output = new byte[outputLength];
                Marshal.Copy(outputPtr, output, 0, output.Length);
                return output;
            }
            finally
            {
                if (inputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inputPtr);
                }
                if (outputPtr != IntPtr.Zero)
                {
                    Gsasl.gsasl_free(outputPtr);
                }
            }
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
            Gsasl.gsasl_finish(handle);
            return true;
        }
    }
}