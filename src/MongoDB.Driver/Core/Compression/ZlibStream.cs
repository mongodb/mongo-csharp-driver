/* Copyright 2019-present MongoDB Inc.
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
using System.IO;
using System.IO.Compression;

namespace MongoDB.Driver.Core.Compression
{
    // Replacement for SharpCompress.Compressors.Deflate.ZlibStream.
    // Implements RFC 1950 (zlib framing) around System.IO.Compression.DeflateStream (RFC 1951),
    // with manual Adler-32 checksum handling.
    //
    // Both compress and decompress initialise lazily — no I/O happens in the constructor.
    // Decompress path: the compressed input is buffered up-front (small — wire messages are
    // length-bounded), but the decompressed output is produced lazily as the caller reads,
    // with Adler-32 accumulated incrementally and validated when DeflateStream signals EOF.
    internal sealed class ZlibStream : Stream
    {
        // RFC 1950 headers: CMF=0x78 (deflate, 32K window) + FLG satisfying (CMF*256+FLG)%31==0.
        // FLEVEL bits in FLG are informational only (RFC 1950 §2.2) and do not affect decompression.
        private static readonly byte[] s_defaultHeader = { 0x78, 0x9C };       // FLEVEL=2
        private static readonly byte[] s_noCompressionHeader = { 0x78, 0x01 }; // FLEVEL=0

        private readonly Stream _stream;
        private readonly CompressionMode _mode;
        private readonly CompressionLevel _level;
        private readonly bool _leaveOpen;

        // Compress-mode state (initialised lazily on first Write)
        private DeflateStream _compressDeflate;
        private uint _compressAdler32 = 1u;
        private bool _compressInitialized;

        // Decompress-mode state (initialised lazily on first Read)
        private DeflateStream _decompressDeflate;
        private byte[] _trailer;
        private uint _decompressAdler32 = 1u;
        private bool _decompressInitialized;
        private bool _trailerValidated;

        private bool _disposed;

        public ZlibStream(Stream stream, CompressionMode mode, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = false)
        {
            _stream = stream;
            _mode = mode;
            _level = level;
            _leaveOpen = leaveOpen;
        }

        public override bool CanRead => _mode == CompressionMode.Decompress && !_disposed;
        public override bool CanWrite => _mode == CompressionMode.Compress && !_disposed;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _compressDeflate?.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureDecompressInitialized();
            var n = _decompressDeflate.Read(buffer, offset, count);
            if (n > 0)
            {
                _decompressAdler32 = UpdateAdler32(_decompressAdler32, buffer, offset, n);
            }
            else if (!_trailerValidated)
            {
                _trailerValidated = true;
                var expected = (uint)(_trailer[0] << 24 | _trailer[1] << 16 | _trailer[2] << 8 | _trailer[3]);
                if (_decompressAdler32 != expected)
                {
                    throw new InvalidDataException("Zlib Adler-32 checksum mismatch.");
                }
            }
            return n;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCompressInitialized();
            _compressAdler32 = UpdateAdler32(_compressAdler32, buffer, offset, count);
            _compressDeflate.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_mode == CompressionMode.Compress)
                {
                    // Emit a valid empty zlib stream if Write was never called (matches SharpCompress).
                    EnsureCompressInitialized();
                    _compressDeflate.Dispose();
                    _stream.WriteByte((byte)(_compressAdler32 >> 24));
                    _stream.WriteByte((byte)(_compressAdler32 >> 16));
                    _stream.WriteByte((byte)(_compressAdler32 >> 8));
                    _stream.WriteByte((byte)_compressAdler32);
                }
                else
                {
                    _decompressDeflate?.Dispose();
                }
                if (!_leaveOpen)
                    _stream.Dispose();
            }
            base.Dispose(disposing);
        }

        private void EnsureCompressInitialized()
        {
            if (_compressInitialized)
            {
                return;
            }
            _compressInitialized = true;

            var header = _level == CompressionLevel.NoCompression ? s_noCompressionHeader : s_defaultHeader;
            _stream.Write(header, 0, 2);
            _compressDeflate = new DeflateStream(_stream, _level, leaveOpen: true);
        }

        private void EnsureDecompressInitialized()
        {
            if (_decompressInitialized)
            {
                return;
            }
            _decompressInitialized = true;

            byte[] compressed;
            using (var ms = new MemoryStream())
            {
                _stream.CopyTo(ms);
                compressed = ms.ToArray();
            }

            // 2-byte header + at least 1 byte deflate payload (empty deflate is "03 00") + 4-byte Adler-32 trailer.
            if (compressed.Length < 7)
            {
                throw new FormatException("Compressed data is too short to be a valid zlib stream.");
            }

            // RFC 1950 §2.2: CMF low nibble must be 8 (deflate); (CMF*256+FLG) must be divisible by 31.
            var cmf = compressed[0];
            var flg = compressed[1];
            if ((cmf & 0x0F) != 8 || (cmf * 256 + flg) % 31 != 0)
            {
                throw new FormatException("Invalid zlib header.");
            }

            // RFC 1950 §2.2: FDICT (FLG bit 5). MongoDB never sends preset dictionaries; reject with a
            // clear error rather than letting the DICTID bytes be misinterpreted as deflate data.
            if ((flg & 0x20) != 0)
            {
                throw new NotSupportedException("Zlib preset dictionaries (FDICT) are not supported.");
            }

            _trailer = new byte[4];
            Buffer.BlockCopy(compressed, compressed.Length - 4, _trailer, 0, 4);

            var deflateLength = compressed.Length - 2 - 4;
            var deflateSource = new MemoryStream(compressed, 2, deflateLength, writable: false);
            _decompressDeflate = new DeflateStream(deflateSource, CompressionMode.Decompress);
        }

        private static uint UpdateAdler32(uint adler, byte[] buf, int offset, int count)
        {
            const uint ModAdler = 65521;
            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;
            for (int i = 0; i < count; i++)
            {
                s1 = (s1 + buf[offset + i]) % ModAdler;
                s2 = (s2 + s1) % ModAdler;
            }
            return (s2 << 16) | s1;
        }
    }
}
