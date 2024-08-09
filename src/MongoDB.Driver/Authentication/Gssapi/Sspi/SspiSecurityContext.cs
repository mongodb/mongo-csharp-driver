/* Copyright 2010-present MongoDB Inc.
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
#if NET472
using System.Runtime.CompilerServices;
#endif

namespace MongoDB.Driver.Authentication.Gssapi.Sspi
{
    /// <summary>
    /// A wrapper around the SspiHandle structure specifically used as a security context handle.
    /// </summary>
    internal class SspiSecurityContext : SafeHandle, ISecurityContext
    {
        // static fields
        private static readonly int __maxTokenSize;

        // fields
        private readonly string _servicePrincipalName;
        private SspiSecurityCredential _credential;
        private SspiHandle _sspiHandle;
        private bool _isInitialized;
        private bool _isDisposed;

        // constructors
        static SspiSecurityContext()
        {
            __maxTokenSize = GetMaxTokenSize();
        }

        public SspiSecurityContext(string servicePrincipalName, SspiSecurityCredential credential)
            : base(IntPtr.Zero, true)
        {
            _servicePrincipalName = servicePrincipalName;
            _credential = credential;
            _sspiHandle = new SspiHandle();
        }

        // properties
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        public override bool IsInvalid
        {
            get { return base.IsClosed || _sspiHandle.IsZero; }
        }

        // public methods
        public byte[] DecryptMessage(int messageLength, byte[] encryptedBytes)
        {
            byte[] decryptedBytes;

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
#if NET472
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
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
                    var result = NativeMethods.DecryptMessage(
                        ref _sspiHandle,
                        ref descriptor,
                        0,
                        out quality);

                    if (result != NativeMethods.SEC_E_OK)
                    {
                        throw NativeMethods.CreateException(result, "Unable to decrypt message.");
                    }

                    decryptedBytes = descriptor.ToByteArray();
                }
                finally
                {
                    descriptor.Free();
                }
            }

            return decryptedBytes;
        }

        public byte[] EncryptMessage(byte[] plainTextBytes)
        {
            byte[] outBytes;

            bool contextAddRefSuccess = false;
            SecurityPackageContextSizes sizes;
#if NET472
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
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
                uint result = NativeMethods.QueryContextAttributes(
                    ref _sspiHandle,
                    QueryContextAttributes.Sizes,
                    out sizes);

                DangerousRelease();

                if (result != NativeMethods.SEC_E_OK)
                {
                    throw NativeMethods.CreateException(result, "Unable to get the query context attribute sizes.");
                }
            }

            var buffers = new SecurityBuffer[]
            {
                new SecurityBuffer(new byte[sizes.SecurityTrailer], SecurityBufferType.Token),
                new SecurityBuffer(plainTextBytes, SecurityBufferType.Data),
                new SecurityBuffer(new byte[sizes.BlockSize], SecurityBufferType.Padding)
            };

            var descriptor = new SecurityBufferDescriptor(buffers);
#if NET472
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
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
                    uint result = NativeMethods.EncryptMessage(
                        ref _sspiHandle,
                        EncryptQualityOfProtection.WrapNoEncrypt,
                        ref descriptor,
                        0);

                    DangerousRelease();

                    if (result != NativeMethods.SEC_E_OK)
                    {
                        throw NativeMethods.CreateException(result, "Unable to encrypt message.");
                    }

                    outBytes = descriptor.ToByteArray();
                }
                finally
                {
                    descriptor.Free();
                }
            }

            return outBytes;
        }

        public byte[] Next(byte[] challenge)
        {
            byte[] outBytes;

            var outputBuffer = new SecurityBufferDescriptor(__maxTokenSize);

            bool credentialAddRefSuccess = false;
            bool contextAddRefSuccess = false;

#if NET472
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
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
                    if (challenge == null || challenge.Length == 0)
                    {
                        result = NativeMethods.InitializeSecurityContext(
                            ref credentialHandle,
                            IntPtr.Zero,
                            _servicePrincipalName,
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
                        var serverToken = new SecurityBufferDescriptor(challenge);
                        try
                        {
                            result = NativeMethods.InitializeSecurityContext(
                                ref credentialHandle,
                                ref _sspiHandle,
                                _servicePrincipalName,
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
                            serverToken.Free();
                        }
                    }

                    _credential.DangerousRelease();
                    DangerousRelease();

                    if (result != NativeMethods.SEC_E_OK && result != NativeMethods.SEC_I_CONTINUE_NEEDED)
                    {
                        throw NativeMethods.CreateException(result, "Unable to initialize security context.");
                    }

                    outBytes = outputBuffer.ToByteArray();
                    _isInitialized = result == NativeMethods.SEC_E_OK;
                }
                finally
                {
                    outputBuffer.Free();
                }
            }

            return outBytes;
        }

        // protected methods
        protected override bool ReleaseHandle()
        {
            return NativeMethods.DeleteSecurityContext(ref _sspiHandle) == 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _credential?.Dispose();
                _credential = null;
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        // static methods
        private static int GetMaxTokenSize()
        {
            uint count = 0;
            var array = IntPtr.Zero;

            try
            {
                var result = NativeMethods.EnumerateSecurityPackages(ref count, ref array);
                if (result != NativeMethods.SEC_E_OK)
                {
                    return NativeMethods.MAX_TOKEN_SIZE;
                }

                var current = new IntPtr(array.ToInt64());
                var size = Marshal.SizeOf<SecurityPackageInfo>();
                for (int i = 0; i < count; i++)
                {
                    var package = Marshal.PtrToStructure<SecurityPackageInfo>(current);
                    if (package.Name != null && package.Name.Equals(SspiPackage.Kerberos.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return (int)package.MaxTokenSize;
                    }
                    current = new IntPtr(current.ToInt64() + size);
                }

                return NativeMethods.MAX_TOKEN_SIZE;
            }
            catch
            {
                return NativeMethods.MAX_TOKEN_SIZE;
            }
            finally
            {
                try
                {
                    NativeMethods.FreeContextBuffer(array);
                }
                catch
                { }
            }
        }
    }
}
