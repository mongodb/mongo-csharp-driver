/* Original work:
*   Copyright(c) 2016-present, Facebook, Inc. All rights reserved.
*
*   Redistribution and use in source and binary forms, with or without modification,
*   are permitted provided that the following conditions are met:
*
*   * Redistributions of source code must retain the above copyright notice, this
*     list of conditions and the following disclaimer.
*
*   * Redistributions in binary form must reproduce the above copyright notice,
*     this list of conditions and the following disclaimer in the documentation
*     and/or other materials provided with the distribution.
*
*   * Neither the name Facebook nor the names of its contributors may be used to
*     endorse or promote products derived from this software without specific
*     prior written permission.
*
*   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
*   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
*   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
*   DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
*   ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
*   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
*   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
*   ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
*   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
*   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
* Modified work:
*   Copyright 2020–present MongoDB Inc.
*
*   Licensed under the Apache License, Version 2.0 (the "License");
*   you may not use this file except in compliance with the License.
*   You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
*   Unless required by applicable law or agreed to in writing, software
*   distributed under the License is distributed on an "AS IS" BASIS,
*   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*   See the License for the specific language governing permissions and
*   limitations under the License.
*/

using System;
using System.IO;
using System.Runtime.InteropServices;
using MongoDB.Driver.Core.NativeLibraryLoader;

namespace MongoDB.Driver.Core.Compression.Zstandard
{
    internal class Zstandard64NativeMethods
    {
        // private static fields
        private static readonly Lazy<LibraryLoader> __libraryLoader;
        private static readonly Lazy<Delegates64.ZSTD_CStreamInSize> __ZSTD_CStreamInSize;
        private static readonly Lazy<Delegates64.ZSTD_CStreamOutSize> __ZSTD_CStreamOutSize;
        private static readonly Lazy<Delegates64.ZSTD_createCStream> __ZSTD_createCStream;

        private static readonly Lazy<Delegates64.ZSTD_DStreamInSize> __ZSTD_DStreamInSize;
        private static readonly Lazy<Delegates64.ZSTD_DStreamOutSize> __ZSTD_DStreamOutSize;
        private static readonly Lazy<Delegates64.ZSTD_createDStream> __ZSTD_createDStream;

        private static readonly Lazy<Delegates64.ZSTD_maxCLevel> __ZSTD_maxCLevel;

        private static readonly Lazy<Delegates64.ZSTD_flushStream> __ZSTD_flushStream;
        private static readonly Lazy<Delegates64.ZSTD_endStream> __ZSTD_endStream;
        private static readonly Lazy<Delegates64.ZSTD_freeCStream> __ZSTD_freeCStream;
        private static readonly Lazy<Delegates64.ZSTD_freeDStream> __ZSTD_freeDStream;

        private static readonly Lazy<Delegates64.ZSTD_initDStream> __ZSTD_initDStream;
        private static readonly Lazy<Delegates64.ZSTD_decompressStream> __ZSTD_decompressStream;

        private static readonly Lazy<Delegates64.ZSTD_initCStream> __ZSTD_initCStream;
        private static readonly Lazy<Delegates64.ZSTD_compressStream> __ZSTD_compressStream;

        private static readonly Lazy<Delegates64.ZSTD_isError> __ZSTD_isError;
        private static readonly Lazy<Delegates64.ZSTD_getErrorName> __ZSTD_getErrorName;


        // static constructor
        static Zstandard64NativeMethods()
        {
            var zstandardLocator = new ZstandardLocator();
            __libraryLoader = new Lazy<LibraryLoader>(() => new LibraryLoader(zstandardLocator), isThreadSafe: true);

            __ZSTD_CStreamInSize = CreateLazyForDelegate<Delegates64.ZSTD_CStreamInSize>(nameof(ZSTD_CStreamInSize));
            __ZSTD_CStreamOutSize = CreateLazyForDelegate<Delegates64.ZSTD_CStreamOutSize>(nameof(ZSTD_CStreamOutSize));
            __ZSTD_createCStream = CreateLazyForDelegate<Delegates64.ZSTD_createCStream>(nameof(ZSTD_createCStream));

            __ZSTD_DStreamInSize = CreateLazyForDelegate<Delegates64.ZSTD_DStreamInSize>(nameof(ZSTD_DStreamInSize));
            __ZSTD_DStreamOutSize = CreateLazyForDelegate<Delegates64.ZSTD_DStreamOutSize>(nameof(ZSTD_DStreamOutSize));
            __ZSTD_createDStream = CreateLazyForDelegate<Delegates64.ZSTD_createDStream>(nameof(ZSTD_createDStream));

            __ZSTD_maxCLevel = CreateLazyForDelegate<Delegates64.ZSTD_maxCLevel>(nameof(ZSTD_maxCLevel));

            __ZSTD_flushStream = CreateLazyForDelegate<Delegates64.ZSTD_flushStream>(nameof(ZSTD_flushStream));
            __ZSTD_endStream = CreateLazyForDelegate<Delegates64.ZSTD_endStream>(nameof(ZSTD_endStream));
            __ZSTD_freeCStream = CreateLazyForDelegate<Delegates64.ZSTD_freeCStream>(nameof(ZSTD_freeCStream));
            __ZSTD_freeDStream = CreateLazyForDelegate<Delegates64.ZSTD_freeDStream>(nameof(ZSTD_freeDStream));

            __ZSTD_initDStream = CreateLazyForDelegate<Delegates64.ZSTD_initDStream>(nameof(ZSTD_initDStream));
            __ZSTD_decompressStream = CreateLazyForDelegate<Delegates64.ZSTD_decompressStream>(nameof(ZSTD_decompressStream));

            __ZSTD_initCStream = CreateLazyForDelegate<Delegates64.ZSTD_initCStream>(nameof(ZSTD_initCStream));
            __ZSTD_compressStream = CreateLazyForDelegate<Delegates64.ZSTD_compressStream>(nameof(ZSTD_compressStream));

            __ZSTD_isError = CreateLazyForDelegate<Delegates64.ZSTD_isError>(nameof(ZSTD_isError));
            __ZSTD_getErrorName = CreateLazyForDelegate<Delegates64.ZSTD_getErrorName>(nameof(ZSTD_getErrorName));
        }

        // public static methods
        public static ulong ZSTD_CStreamInSize()
        {
            return __ZSTD_CStreamInSize.Value();
        }

        public static ulong ZSTD_CStreamOutSize()
        {
            return __ZSTD_CStreamOutSize.Value();
        }

        public static IntPtr ZSTD_createCStream()
        {
            return __ZSTD_createCStream.Value();
        }

        public static ulong ZSTD_DStreamInSize()
        {
            return __ZSTD_DStreamInSize.Value();
        }

        public static ulong ZSTD_DStreamOutSize()
        {
            return __ZSTD_DStreamOutSize.Value();
        }

        public static IntPtr ZSTD_createDStream()
        {
            return __ZSTD_createDStream.Value();
        }

        public static long ZSTD_maxCLevel()
        {
            return __ZSTD_maxCLevel.Value();
        }

        public static ulong ZSTD_flushStream(IntPtr zcs, NativeBufferInfo outputBuffer)
        {
            var result = __ZSTD_flushStream.Value(zcs, outputBuffer);
            ThrowIfError(result);
            return result;
        }

        public static ulong ZSTD_endStream(IntPtr zcs, NativeBufferInfo outputBuffer)
        {
            var result = __ZSTD_endStream.Value(zcs, outputBuffer);
            ThrowIfError(result);
            return result;
        }

        public static ulong ZSTD_freeCStream(IntPtr zcs)
        {
            return __ZSTD_freeCStream.Value(zcs);
        }

        public static ulong ZSTD_freeDStream(IntPtr zds)
        {
            return __ZSTD_freeDStream.Value(zds);
        }

        public static ulong ZSTD_initDStream(IntPtr zds)
        {
            return __ZSTD_initDStream.Value(zds);
        }

        public static ulong ZSTD_decompressStream(IntPtr zds, NativeBufferInfo outputBuffer, NativeBufferInfo inputBuffer)
        {
            var result = __ZSTD_decompressStream.Value(zds, outputBuffer, inputBuffer);
            ThrowIfError(result);
            return result;
        }

        public static ulong ZSTD_initCStream(IntPtr zcs, int compressionLevel)
        {
            var result = __ZSTD_initCStream.Value(zcs, compressionLevel);
            ThrowIfError(result);
            return result;
        }

        public static ulong ZSTD_compressStream(IntPtr zcs, NativeBufferInfo outputBuffer, NativeBufferInfo inputBuffer)
        {
            var result = __ZSTD_compressStream.Value(zcs, outputBuffer, inputBuffer);
            ThrowIfError(result);
            return result;
        }

        // private static methods
        private static Lazy<TDelegate> CreateLazyForDelegate<TDelegate>(string name)
        {
            return new Lazy<TDelegate>(() => __libraryLoader.Value.GetDelegate<TDelegate>(name), isThreadSafe: true);
        }

        private static void ThrowIfError(ulong code)
        {
            if (Zstandard64NativeMethods.ZSTD_isError(code))
            {
                var errorPtr = Zstandard64NativeMethods.ZSTD_getErrorName(code);
                var errorMsg = Marshal.PtrToStringAnsi(errorPtr);
                throw new IOException(errorMsg);
            }
        }

        private static bool ZSTD_isError(ulong code)
        {
            return __ZSTD_isError.Value(code);
        }

        private static IntPtr ZSTD_getErrorName(ulong code)
        {
            return __ZSTD_getErrorName.Value(code);
        }

        // nested types
        private class Delegates64
        {
            public delegate ulong ZSTD_CStreamInSize();
            public delegate ulong ZSTD_CStreamOutSize();
            public delegate IntPtr ZSTD_createCStream();

            public delegate ulong ZSTD_DStreamInSize();
            public delegate ulong ZSTD_DStreamOutSize();
            public delegate IntPtr ZSTD_createDStream();

            public delegate long ZSTD_maxCLevel();

            public delegate ulong ZSTD_flushStream(IntPtr zcs, [MarshalAs(UnmanagedType.LPStruct)] NativeBufferInfo outputBuffer);
            public delegate ulong ZSTD_endStream(IntPtr zcs, [MarshalAs(UnmanagedType.LPStruct)] NativeBufferInfo outputBuffer);
            public delegate ulong ZSTD_freeCStream(IntPtr zcs);
            public delegate ulong ZSTD_freeDStream(IntPtr zds);

            public delegate ulong ZSTD_initDStream(IntPtr zds);

            public delegate ulong ZSTD_decompressStream(IntPtr zds, [MarshalAs(UnmanagedType.LPStruct)] NativeBufferInfo outputBuffer, [MarshalAs(UnmanagedType.LPStruct)] NativeBufferInfo inputBuffer);

            public delegate ulong ZSTD_initCStream(IntPtr zcs, int compressionLevel);

            public delegate ulong ZSTD_compressStream(IntPtr zcs, [MarshalAs(UnmanagedType.LPStruct)] NativeBufferInfo outputBuffer, [MarshalAs(UnmanagedType.LPStruct)] NativeBufferInfo inputBuffer);

            public delegate bool ZSTD_isError(ulong code);
            public delegate IntPtr ZSTD_getErrorName(ulong code);
        }

        private class ZstandardLocator : RelativeLibraryLocatorBase
        {
            public override string GetLibraryRelativePath(SupportedPlatform currentPlatform)
            {
                switch (currentPlatform)
                {
                    case SupportedPlatform.Windows:
                        return @"runtimes\win\native\libzstd.dll";
                    case SupportedPlatform.Linux: // TODO: add support for Linux and MacOS later
                    case SupportedPlatform.MacOS:
                    default:
                        throw new InvalidOperationException($"Zstandard is not supported on the current platform: {currentPlatform}.");
                }
            }
        }
    }
}
