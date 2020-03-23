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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Compression.Zstandard
{
    internal class ZstandardNativeWrapper : IDisposable
    {
        #region static
        public static int MaxCompressionLevel => (int)Zstandard64NativeMethods.ZSTD_maxCLevel();
        #endregion

        private readonly int _compressionLevel;
        private readonly CompressionMode _compressionMode;
        private bool _operationInitialized;
        private readonly ulong _recommendedZstreamInputSize;
        private readonly ulong _recommendedZstreamOutputSize;
        private IntPtr _zstreamPointer;

        public ZstandardNativeWrapper(CompressionMode compressionMode, int compressionLevel)
        {
            _compressionLevel = compressionLevel;
            _compressionMode = compressionMode;

            switch (_compressionMode)
            {
                case CompressionMode.Compress:
                    // calculate recommended size for input buffer
                    _recommendedZstreamInputSize = Zstandard64NativeMethods.ZSTD_CStreamInSize();
                    // calculate recommended size for output buffer. Guarantee to successfully flush at least one complete compressed block
                    _recommendedZstreamOutputSize = Zstandard64NativeMethods.ZSTD_CStreamOutSize();
                    _zstreamPointer = Zstandard64NativeMethods.ZSTD_createCStream(); // create resource
                    break;
                case CompressionMode.Decompress:
                    // calculate recommended size for input buffer
                    _recommendedZstreamInputSize = Zstandard64NativeMethods.ZSTD_DStreamInSize();
                    // calculate recommended size for output buffer. Guarantee to successfully flush at least one complete block in all circumstances
                    _recommendedZstreamOutputSize = Zstandard64NativeMethods.ZSTD_DStreamOutSize();
                    _zstreamPointer = Zstandard64NativeMethods.ZSTD_createDStream(); // create resource
                    break;
            }
        }

        public int RecommendedInputSize => (int)_recommendedZstreamInputSize;
        public int RecommendedOutputSize => (int)_recommendedZstreamOutputSize;

        // public methods
        public void Compress(
            OperationContext operationContext,
            int inputCompressedSize,
            int inputUncompressedSize,
            out int compressedBytesProcessed,
            out int uncompressedBytesProcessed)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.That(_compressionMode == CompressionMode.Compress, nameof(_compressionMode));
            Ensure.IsGreaterThanOrEqualToZero(inputCompressedSize, nameof(inputCompressedSize));
            Ensure.IsGreaterThanOrEqualToZero(inputUncompressedSize, nameof(inputUncompressedSize));

            InitializeIfNotAlreadyInitialized();

            // compressed data
            var outputNativeBuffer = CreateNativeBuffer(
                operationContext.CompressedPinnedBufferWalker, // operation result
                (ulong)inputCompressedSize);

            // uncompressed data
            var inputNativeBuffer = CreateNativeBuffer(
                operationContext.UncompressedPinnedBufferWalker,
                (ulong)inputUncompressedSize);

            // compress inputNativeBuffer to outputNativeBuffer
            _ = Zstandard64NativeMethods.ZSTD_compressStream(
                _zstreamPointer,
                outputNativeBuffer,
                inputNativeBuffer);

            compressedBytesProcessed = (int)outputNativeBuffer.Position; // because start Position is always 0
            uncompressedBytesProcessed = (int)inputNativeBuffer.Position; // because start Position is always 0

            operationContext.UncompressedPinnedBufferWalker.Offset += uncompressedBytesProcessed;
            // CompressedPinnedBufferWalker.Offset is always 0
        }

        public void Decompress(
            OperationContext operationContext,
            int compressedOffset,
            int inputCompressedSize,
            int inputUncompressedSize,
            out int compressedBytesProcessed,
            out int uncompressedBytesProcessed)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.That(_compressionMode == CompressionMode.Decompress, nameof(_compressionMode));
            Ensure.IsGreaterThanOrEqualToZero(inputCompressedSize, nameof(inputCompressedSize));
            Ensure.IsGreaterThanOrEqualToZero(inputUncompressedSize, nameof(inputUncompressedSize));

            InitializeIfNotAlreadyInitialized();

            // apply reading progress on CompressedPinnedBufferWalker
            operationContext.CompressedPinnedBufferWalker.Offset = compressedOffset;
            // compressed data
            var inputNativeBuffer = CreateNativeBuffer(
                inputCompressedSize <= 0 ? null : operationContext.CompressedPinnedBufferWalker,
                inputCompressedSize <= 0 ? 0 : (ulong)inputCompressedSize);

            // uncompressed data
            var outputNativeBuffer = CreateNativeBuffer(
                operationContext.UncompressedPinnedBufferWalker, // operation result
                (ulong)inputUncompressedSize);

            // decompress inputNativeBuffer to outputNativeBuffer
            _ = Zstandard64NativeMethods.ZSTD_decompressStream(
                _zstreamPointer,
                outputNativeBuffer,
                inputNativeBuffer);

            uncompressedBytesProcessed = (int)outputNativeBuffer.Position; // because start Position is always 0
            compressedBytesProcessed = (int)inputNativeBuffer.Position; // because start Position is always 0

            operationContext.UncompressedPinnedBufferWalker.Offset += uncompressedBytesProcessed;
            // CompressedPinnedBufferWalker.Offset will be calculated on stream side
        }

        public void Dispose()
        {
            switch (_compressionMode)
            {
                case CompressionMode.Compress:
                    Zstandard64NativeMethods.ZSTD_freeCStream(_zstreamPointer);
                    break;
                case CompressionMode.Decompress:
                    Zstandard64NativeMethods.ZSTD_freeDStream(_zstreamPointer);
                    break;
            }
        }

        public OperationContext InitializeOperationContext(
            BufferInfo compressedBufferInfo,
            BufferInfo uncompressedBufferInfo = null)
        {
            var compressedPinnedBufferWalker = new PinnedBufferWalker(compressedBufferInfo.Bytes, compressedBufferInfo.Offset);
            PinnedBufferWalker uncompressedPinnedBufferWalker = null;
            if (uncompressedBufferInfo != null)
            {
                uncompressedPinnedBufferWalker = new PinnedBufferWalker(uncompressedBufferInfo.Bytes, uncompressedBufferInfo.Offset);
            }

            return new OperationContext(uncompressedPinnedBufferWalker, compressedPinnedBufferWalker);
        }

        public IEnumerable<int> StepwiseFlush(BufferInfo compressedBufferInfo)
        {
            if (_compressionMode != CompressionMode.Compress)
            {
                throw new InvalidDataException($"{nameof(StepwiseFlush)} must be called only from Compress mode.");
            }

            using (var operationContext = InitializeOperationContext(compressedBufferInfo))
            {
                yield return ProcessCompressedOutput(operationContext, (zcs, buffer) => Zstandard64NativeMethods.ZSTD_flushStream(zcs, buffer));
            }

            using (var operationContext = InitializeOperationContext(compressedBufferInfo))
            {
                yield return ProcessCompressedOutput(operationContext, (zcs, buffer) => Zstandard64NativeMethods.ZSTD_endStream(zcs, buffer));
            }

            int ProcessCompressedOutput(OperationContext context, Action<IntPtr, NativeBufferInfo> outputAction)
            {
                var outputNativeBuffer = CreateNativeBuffer(
                    context.CompressedPinnedBufferWalker,
                    _recommendedZstreamOutputSize);

                outputAction(_zstreamPointer, outputNativeBuffer);

                return (int)outputNativeBuffer.Position;
            }
        }

        // private methods
        private NativeBufferInfo CreateNativeBuffer(PinnedBufferWalker pinnedBufferWalker, ulong size)
        {
            var nativeBuffer = new NativeBufferInfo();
            nativeBuffer.DataPointer = pinnedBufferWalker?.IntPtr ?? IntPtr.Zero;
            nativeBuffer.Size = size;
            nativeBuffer.Position = 0;
            return nativeBuffer;
        }

        private void InitializeIfNotAlreadyInitialized()
        {
            if (!_operationInitialized)
            {
                _operationInitialized = true;

                switch (_compressionMode)
                {
                    case CompressionMode.Compress:
                        Zstandard64NativeMethods.ZSTD_initCStream(_zstreamPointer, _compressionLevel); // start a new compression operation
                        break;
                    case CompressionMode.Decompress:
                        Zstandard64NativeMethods.ZSTD_initDStream(_zstreamPointer); // start a new decompression operation
                        break;
                }
            }
        }

        // nested types
        internal class OperationContext : IDisposable
        {
            private readonly PinnedBufferWalker _compressedPinnedBufferWalker;
            private readonly PinnedBufferWalker _uncompressedPinnedBufferWalker;

            public OperationContext(PinnedBufferWalker uncompressedPinnedBufferWalker, PinnedBufferWalker compressedPinnedBufferWalker)
            {
                _compressedPinnedBufferWalker = Ensure.IsNotNull(compressedPinnedBufferWalker, nameof(compressedPinnedBufferWalker));
                _uncompressedPinnedBufferWalker = uncompressedPinnedBufferWalker; // can be null
            }

            public PinnedBufferWalker CompressedPinnedBufferWalker => _compressedPinnedBufferWalker; // internal data
            public PinnedBufferWalker UncompressedPinnedBufferWalker => _uncompressedPinnedBufferWalker; // external data

            // public methods
            public void Dispose()
            {
                _compressedPinnedBufferWalker.Dispose();  // PinnedBufferWalker.Dispose suppresses all errors
                _uncompressedPinnedBufferWalker?.Dispose();
            }
        }
    }

    internal class BufferInfo
    {
        public BufferInfo(byte[] bytes, int offset)
        {
            Bytes = bytes;
            Offset = offset;
        }

        public byte[] Bytes { get; }
        public int Offset { get; }
    }
}
