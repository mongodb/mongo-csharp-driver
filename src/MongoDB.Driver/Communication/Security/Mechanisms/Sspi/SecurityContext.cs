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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Driver.Communication.Security.Mechanisms.Sspi
{
    /// <summary>
    /// A wrapper around the SspiHandle structure specificly used as a security context handle.
    /// </summary>
    internal class SecurityContext : SafeHandle
    {
        // private static fields
        private static int __maxTokenSize;

        // private fields
        private SecurityCredential _credential;
        private SspiHandle _sspiHandle;
        private bool _isInitialized;

        // constructors
        static SecurityContext()
        {
            __maxTokenSize = GetMaxTokenSize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityContext" /> class.
        /// </summary>
        public SecurityContext()
            : base(IntPtr.Zero, true)
        {
            _sspiHandle = new SspiHandle();
        }

        // public static methods
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <param name="servicePrincipalName">Name of the service principal.</param>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        /// <returns></returns>
        public static SecurityContext Initialize(SecurityCredential credential, string servicePrincipalName, byte[] input, out byte[] output)
        {
            var context = new SecurityContext();
            context._credential = credential;

            context.Initialize(servicePrincipalName, input, out output);
            return context;
        }

        // public properties
        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitialized
        {
            get { return _isInitialized; }
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
            get { return base.IsClosed || _sspiHandle.IsZero; }
        }

        // public methods
        /// <summary>
        /// Decrypts the message.
        /// </summary>
        /// <param name="messageLength">Length of the message.</param>
        /// <param name="encryptedBytes">The encrypted bytes.</param>
        /// <param name="decryptedBytes">The decrypted bytes.</param>
        /// <returns>A result code.</returns>
        public void DecryptMessage(int messageLength, byte[] encryptedBytes, out byte[] decryptedBytes)
        {
            decryptedBytes = null;

            byte[] encryptedMessage = new byte[messageLength];
            Array.Copy(encryptedBytes, 0, encryptedMessage, 0, messageLength);

            int securityTrailerLength = encryptedBytes.Length - messageLength;
            byte[] securityTrailer = new byte[securityTrailerLength];
            Array.Copy(encryptedBytes, messageLength, securityTrailer, 0, securityTrailerLength);

            var buffers = new SecurityBuffer[]
            {
                new SecurityBuffer(encryptedBytes, SecurityBufferType.Data),
                new SecurityBuffer(securityTrailer, SecurityBufferType.Stream)
            };

            var descriptor = new SecurityBufferDescriptor(buffers);
            bool contextAddRefSuccess = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref contextAddRefSuccess);
            }
            catch (Exception ex)
            {
                if (contextAddRefSuccess)
                {
                    DangerousRelease();
                    contextAddRefSuccess = false;
                }

                if (!(ex is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                try
                {
                    uint quality;
                    var result = Win32.DecryptMessage(
                        ref _sspiHandle,
                        ref descriptor,
                        0,
                        out quality);

                    if (result != Win32.SEC_E_OK)
                    {
                        throw Win32.CreateException(result, "Unable to decrypt message.");
                    }

                    decryptedBytes = descriptor.ToByteArray();
                }
                finally
                {
                    descriptor.Dispose();
                }
            }
        }

        /// <summary>
        /// Encrypts the message.
        /// </summary>
        /// <param name="inBytes">The in bytes.</param>
        /// <param name="outBytes">The out bytes.</param>
        /// <returns>A result code.</returns>
        public void EncryptMessage(byte[] inBytes, out byte[] outBytes)
        {
            outBytes = null;

            bool contextAddRefSuccess = false;
            SecurityPackageContextSizes sizes;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref contextAddRefSuccess);
            }
            catch (Exception ex)
            {
                if (contextAddRefSuccess)
                {
                    DangerousRelease();
                    contextAddRefSuccess = false;
                }

                if (!(ex is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                uint result = Win32.QueryContextAttributes(
                    ref _sspiHandle,
                    QueryContextAttributes.Sizes,
                    out sizes);

                DangerousRelease();

                if (result != Win32.SEC_E_OK)
                {
                    throw Win32.CreateException(result, "Unable to get the query context attribute sizes.");
                }

            }

            var buffers = new SecurityBuffer[]
            {
                new SecurityBuffer(new byte[sizes.SecurityTrailer], SecurityBufferType.Token),
                new SecurityBuffer(inBytes, SecurityBufferType.Data),
                new SecurityBuffer(new byte[sizes.BlockSize], SecurityBufferType.Padding)
            };

            var descriptor = new SecurityBufferDescriptor(buffers);
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref contextAddRefSuccess);
            }
            catch (Exception ex)
            {
                if (contextAddRefSuccess)
                {
                    DangerousRelease();
                    contextAddRefSuccess = false;
                }

                if (!(ex is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                try
                {
                    uint result = Win32.EncryptMessage(
                        ref _sspiHandle,
                        EncryptQualityOfProtection.WrapNoEncrypt,
                        ref descriptor,
                        0);

                    DangerousRelease();

                    if (result != Win32.SEC_E_OK)
                    {
                        throw Win32.CreateException(result, "Unable to encrypt message.");
                    }

                    outBytes = descriptor.ToByteArray();
                }
                finally
                {
                    descriptor.Dispose();
                }
            }
        }

        /// <summary>
        /// Initializes the specified service principal name.
        /// </summary>
        /// <param name="servicePrincipalName">Name of the service principal.</param>
        /// <param name="inBytes">The in bytes.</param>
        /// <param name="outBytes">The out bytes.</param>
        public void Initialize(string servicePrincipalName, byte[] inBytes, out byte[] outBytes)
        {
            outBytes = null;

            var outputBuffer = new SecurityBufferDescriptor(__maxTokenSize);
            
            bool credentialAddRefSuccess = false;
            bool contextAddRefSuccess = false;
            
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                _credential.DangerousAddRef(ref credentialAddRefSuccess);
                DangerousAddRef(ref contextAddRefSuccess);
            }
            catch (Exception ex)
            {
                if (credentialAddRefSuccess)
                {
                    _credential.DangerousRelease();
                    credentialAddRefSuccess = false;
                }
                if (contextAddRefSuccess)
                {
                    DangerousRelease();
                    contextAddRefSuccess = false;
                }

                if (!(ex is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                try
                {
                    var flags = SspiContextFlags.MutualAuth;

                    uint result;
                    long timestamp;
                    var credentialHandle = _credential._sspiHandle;
                    if (inBytes == null || inBytes.Length == 0)
                    {
                        result = Win32.InitializeSecurityContext(
                            ref credentialHandle,
                            IntPtr.Zero,
                            servicePrincipalName,
                            flags,
                            0,
                            DataRepresentation.Network,
                            IntPtr.Zero,
                            0,
                            ref _sspiHandle,
                            ref outputBuffer,
                            out flags,
                            out timestamp);
                    }
                    else
                    {
                        var serverToken = new SecurityBufferDescriptor(inBytes);
                        try
                        {
                            result = Win32.InitializeSecurityContext(
                                ref credentialHandle,
                                ref _sspiHandle,
                                servicePrincipalName,
                                flags,
                                0,
                                DataRepresentation.Network,
                                ref serverToken,
                                0,
                                ref _sspiHandle,
                                ref outputBuffer,
                                out flags,
                                out timestamp);
                        }
                        finally
                        {
                            serverToken.Dispose();
                        }
                    }

                    _credential.DangerousRelease();
                    DangerousRelease();

                    if (result != Win32.SEC_E_OK && result != Win32.SEC_I_CONTINUE_NEEDED)
                    {
                        throw Win32.CreateException(result, "Unable to initialize security context.");
                    }

                    outBytes = outputBuffer.ToByteArray();
                    _isInitialized = result == Win32.SEC_E_OK;
                }
                finally
                {
                    outputBuffer.Dispose();
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
            return Win32.DeleteSecurityContext(ref _sspiHandle) == 0;
        }

        // private methods
        private static int GetMaxTokenSize()
        {
            uint count = 0;
            var array = IntPtr.Zero;

            try
            {
                var result = Win32.EnumerateSecurityPackages(ref count, ref array);
                if (result != Win32.SEC_E_OK)
                {
                    return Win32.MAX_TOKEN_SIZE;
                }

                var current = new IntPtr(array.ToInt64());
                var size = Marshal.SizeOf(typeof(SecurityPackageInfo));
                for (int i = 0; i < count; i++)
                {
                    var package = (SecurityPackageInfo)Marshal.PtrToStructure(current, typeof(SecurityPackageInfo));
                    if (package.Name != null && package.Name.Equals(SspiPackage.Kerberos.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        return (int)package.MaxTokenSize;
                    }
                    current = new IntPtr(current.ToInt64() + size);
                }

                return Win32.MAX_TOKEN_SIZE;
            }
            catch
            {
                return Win32.MAX_TOKEN_SIZE;
            }
            finally
            {
                try
                {
                    Win32.FreeContextBuffer(array);
                }
                catch
                { }
            }
        }
    }
}