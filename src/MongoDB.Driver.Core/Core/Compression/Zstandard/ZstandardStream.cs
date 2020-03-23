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
using System.Buffers;
using System.IO;
using System.IO.Compression;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Compression.Zstandard
{
    internal class ZstandardStream : Stream
    {
        #region static
        public static int MaxCompressionLevel => ZstandardNativeWrapper.MaxCompressionLevel; // maximum compression level available
        #endregion

        private const int _defaultCompressionLevel = 6;

        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private readonly Stream _compressedStream; // input for decompress and output for compress

        private readonly StreamReadHelper _streamReadHelper;
        private readonly StreamWriteHelper _streamWriteHelper;
        private readonly CompressionMode _compressionMode;

        private bool _disposed;
        private bool _flushed;
        private readonly ZstandardNativeWrapper _nativeWrapper;

        public ZstandardStream(
            Stream compressedStream,
            CompressionMode compressionMode,
            Optional<int> compressionLevel = default)
        {
            _compressedStream = Ensure.IsNotNull(compressedStream, nameof(compressedStream));
            _compressionMode = EnsureCompressionModeIsValid(compressionMode);

            _nativeWrapper = new ZstandardNativeWrapper(_compressionMode, EnsureCompressionLevelIsValid(compressionLevel));
            switch (_compressionMode)
            {
                case CompressionMode.Compress:
                    _streamWriteHelper = new StreamWriteHelper(
                        compressedStream: compressedStream,
                        compressedBuffer: _arrayPool.Rent(_nativeWrapper.RecommendedOutputSize));
                    break;
                case CompressionMode.Decompress:
                    _streamReadHelper = new StreamReadHelper(
                        compressedStream: compressedStream,
                        compressedBuffer: _arrayPool.Rent(_nativeWrapper.RecommendedInputSize));
                    break;
            }
        }

        public override bool CanRead => _compressedStream.CanRead && _compressionMode == CompressionMode.Decompress;

        public override bool CanWrite => _compressedStream.CanWrite && _compressionMode == CompressionMode.Compress;

        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                switch (_compressionMode)
                {
                    case CompressionMode.Compress:
                        if (!_flushed)
                        {
                            try { Flush(); } catch { }
                        }
                        _nativeWrapper.Dispose();
                        _arrayPool.Return(_streamWriteHelper.CompressedBufferInfo.Bytes);
                        break;
                    case CompressionMode.Decompress:
                        _nativeWrapper.Dispose();
                        _arrayPool.Return(_streamReadHelper.CompressedBufferInfo.Bytes);
                        break;
                }

                _disposed = true;
            }
        }

        public override void Flush()
        {
            foreach (var outputBufferPosition in _nativeWrapper.StepwiseFlush(_streamWriteHelper.CompressedBufferInfo))
            {
                _streamWriteHelper.WriteBufferToCompressedStream(count: outputBufferPosition);
            }
            _compressedStream.Flush();
            _flushed = true;
        }

        public override int Read(byte[] outputBytes, int outputOffset, int count) // Decompress
        {
            if (!CanRead) throw new InvalidDataException("Read is not accessible.");

            var uncompressedOutputBuffer = new BufferInfo(outputBytes, outputOffset);

            using (var operationContext = _nativeWrapper.InitializeOperationContext(
                _streamReadHelper.CompressedBufferInfo,
                uncompressedOutputBuffer)) // operation result
            {
                var totalUncompressedBytesProcessed = 0; // the result size
                var remainingCount = count;

                while (remainingCount > 0)
                {
                    var currentDataPosition = _streamReadHelper.ReadCompressedStreamToBufferIfAvailable(
                        recommendedCompressedSize: _nativeWrapper.RecommendedInputSize,
                        out int remainingCompressedBufferSize);

                    // decompress input to output
                    _nativeWrapper.Decompress(
                        operationContext,
                        compressedOffset: currentDataPosition,
                        inputCompressedSize: remainingCompressedBufferSize,
                        inputUncompressedSize: remainingCount,
                        out int compressedBytesProcessed,
                        out int uncompressedBytesProcessed);

                    if (!_streamReadHelper.TryPrepareDataForNextAttempt(
                        compressedBytesProcessed,
                        uncompressedBytesProcessed))
                    {
                        break;
                    }

                    totalUncompressedBytesProcessed += uncompressedBytesProcessed;
                    remainingCount -= uncompressedBytesProcessed;
                }

                return totalUncompressedBytesProcessed;
            }
        }

        public override void Write(byte[] inputBytes, int inputOffset, int count) // Compress
        {
            if (!CanWrite) throw new InvalidDataException("Write is not accessible.");

            var uncompressedInputInfo = new BufferInfo(inputBytes, inputOffset);

            using (var operationContext = _nativeWrapper.InitializeOperationContext(
                _streamWriteHelper.CompressedBufferInfo, // operation result
                uncompressedInputInfo))
            {
                var remainingCount = count;

                while (remainingCount > 0)
                {
                    var currentAttemptSize = Math.Min(remainingCount, _nativeWrapper.RecommendedInputSize);

                    // compress input to output
                    _nativeWrapper.Compress(
                        operationContext,
                        inputCompressedSize: _nativeWrapper.RecommendedOutputSize,
                        inputUncompressedSize: currentAttemptSize,
                        out var compressedBytesProcessed,
                        out var uncompressedBytesProcessed);

                    _streamWriteHelper.WriteBufferToCompressedStream(count: compressedBytesProcessed);

                    // calculate progress in input buffer
                    remainingCount -= uncompressedBytesProcessed;
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        // private methods
        private int EnsureCompressionLevelIsValid(Optional<int> compressionLevel)
        {
            if (compressionLevel.HasValue && _compressionMode != CompressionMode.Compress)
            {
                throw new ArgumentException("Compression level can be specified only in Compress mode", nameof(compressionLevel));
            }

            if (compressionLevel.HasValue && (compressionLevel.Value <= 0 || compressionLevel.Value > ZstandardNativeWrapper.MaxCompressionLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(compressionLevel));
            }

            return compressionLevel.WithDefault(_defaultCompressionLevel);
        }

        private CompressionMode EnsureCompressionModeIsValid(CompressionMode compressionMode)
        {
            if (compressionMode != CompressionMode.Compress && compressionMode != CompressionMode.Decompress)
            {
                throw new ArgumentException($"Invalid compression mode {compressionMode}.", nameof(compressionMode));
            }
            return compressionMode;
        }

        // nested types
        private class StreamReadHelper
        {
            private readonly byte[] _compressedDataBuffer;
            private readonly Stream _compressedStream;
            private readonly ReadingState _readingState;

            public StreamReadHelper(
                Stream compressedStream,
                byte[] compressedBuffer)
            {
                _compressedDataBuffer = Ensure.IsNotNull(compressedBuffer, nameof(compressedBuffer));
                _compressedStream = Ensure.IsNotNull(compressedStream, nameof(compressedStream));
                _readingState = new ReadingState();
            }

            public BufferInfo CompressedBufferInfo => new BufferInfo(_compressedDataBuffer, _readingState.DataPosition);

            // public methods
            public int ReadCompressedStreamToBufferIfAvailable(
                int recommendedCompressedSize,
                out int remainingSize)
            {
                remainingSize = _readingState.LastReadDataSize - _readingState.DataPosition;

                if (remainingSize <= 0 && !_readingState.IsDataDepleted && !_readingState.SkipDataReading)
                {
                    var readSize = _compressedStream.Read(_compressedDataBuffer, 0, recommendedCompressedSize);
                    UpdateStateAfterReading(readSize, out remainingSize);
                }

                return _readingState.DataPosition;
            }

            public bool TryPrepareDataForNextAttempt(int compressedBytesProcessed, int uncompressedBytesProcessed)
            {
                if (uncompressedBytesProcessed == 0) // 0 - when a frame is completely decoded and fully flushed
                {
                    // the internal buffer is depleted, we're either done
                    if (_readingState.IsDataDepleted)
                    {
                        return false;
                    }

                    // or we need more bytes
                    _readingState.SkipDataReading = false;
                }

                // 1. calculate progress in compressed(input) buffer
                // 2. save the data position for next Read calls
                _readingState.DataPosition += compressedBytesProcessed;
                return true;
            }

            // private methods
            private void UpdateStateAfterReading(int readSize, out int remainingSize)
            {
                _readingState.LastReadDataSize = readSize;
                _readingState.IsDataDepleted = readSize <= 0;
                _readingState.DataPosition = 0;

                // skip _compressedStream.Read until the internal buffer is depleted
                // avoids a Read timeout for applications that know the exact number of bytes in the _compressedStream
                _readingState.SkipDataReading = true;
                remainingSize = _readingState.IsDataDepleted ? 0 : _readingState.LastReadDataSize;
            }

            // nested types
            private class ReadingState
            {
                /// <summary>
                /// Saves a position between different Read calls.
                /// </summary>
                public int DataPosition { get; set; }
                /// <summary>
                /// Shows whether the last attempt to read data from the stream contained records or no. <c>false</c> if no.
                /// </summary>
                public bool IsDataDepleted { get; set; }
                /// <summary>
                /// The size of the last fetched data from the stream.
                /// </summary>
                public int LastReadDataSize { get; set; }
                /// <summary>
                /// Determines whether stream.Read should be skipped until the internal buffer is depleted.
                /// </summary>
                public bool SkipDataReading { get; set; }
            }
        }

        private class StreamWriteHelper
        {
            private readonly BufferInfo _compressedBufferInfo;
            private readonly Stream _compressedStream;

            public StreamWriteHelper(Stream compressedStream, byte[] compressedBuffer)
            {
                _compressedStream = Ensure.IsNotNull(compressedStream, nameof(compressedStream));
                _compressedBufferInfo = new BufferInfo(
                    Ensure.IsNotNull(compressedBuffer, nameof(compressedBuffer)),
                    offset: 0);
            }

            public BufferInfo CompressedBufferInfo => _compressedBufferInfo; // will be implicitly updated via calls of native methods

            public void WriteBufferToCompressedStream(int count)
            {
                _compressedStream.Write(
                    _compressedBufferInfo.Bytes,
                    _compressedBufferInfo.Offset, // 0
                    count);
            }
        }
    }
}
