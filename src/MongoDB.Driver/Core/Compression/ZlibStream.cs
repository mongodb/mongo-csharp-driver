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
    // Implements RFC 1950 (zlib framing) using System.IO.Compression.DeflateStream for the
    // inner RFC 1951 (deflate) payload, with manual Adler-32 checksum handling.
    internal sealed class ZlibStream : Stream
    {
        // RFC 1950 headers: CMF=0x78 (deflate, 32K window) + FLG satisfying (CMF*256+FLG)%31==0.
        // FLEVEL bits in FLG are informational only (RFC 1950 §2.2) and do not affect decompression.
        private static readonly byte[] s_defaultHeader = { 0x78, 0x9C };       // FLEVEL=2
        private static readonly byte[] s_noCompressionHeader = { 0x78, 0x01 }; // FLEVEL=0

        private readonly Stream _stream;
        private readonly CompressionMode _mode;
        private readonly bool _leaveOpen;

        // Compress-mode state
        private DeflateStream _deflateStream;
        private uint _adler32 = 1u;

        // Decompress-mode state
        private MemoryStream _decompressed;

        private bool _disposed;

        public ZlibStream(Stream stream, CompressionMode mode, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = false)
        {
            _stream = stream;
            _mode = mode;
            _leaveOpen = leaveOpen;

            if (mode == CompressionMode.Compress)
            {
                var header = level == CompressionLevel.NoCompression ? s_noCompressionHeader : s_defaultHeader;
                stream.Write(header, 0, 2);
                _deflateStream = new DeflateStream(stream, level, leaveOpen: true);
            }
            else
            {
                _decompressed = ReadAndDecompress(stream);
            }
        }

        public override bool CanRead => _mode == CompressionMode.Decompress && !_disposed;
        public override bool CanWrite => _mode == CompressionMode.Compress && !_disposed;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _deflateStream?.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => _decompressed.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _adler32 = UpdateAdler32(_adler32, buffer, offset, count);
            _deflateStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_mode == CompressionMode.Compress)
                {
                    _deflateStream.Dispose();
                    _stream.WriteByte((byte)(_adler32 >> 24));
                    _stream.WriteByte((byte)(_adler32 >> 16));
                    _stream.WriteByte((byte)(_adler32 >> 8));
                    _stream.WriteByte((byte)_adler32);
                }
                if (!_leaveOpen)
                    _stream.Dispose();
            }
            base.Dispose(disposing);
        }

        private static MemoryStream ReadAndDecompress(Stream input)
        {
            byte[] compressed;
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                compressed = ms.ToArray();
            }

            if (compressed.Length < 6)
                throw new FormatException("Compressed data is too short to be a valid zlib stream.");

            if ((compressed[0] & 0x0F) != 8 || (compressed[0] * 256 + compressed[1]) % 31 != 0)
                throw new FormatException("Invalid zlib header.");

            var decompressed = new MemoryStream();
            using (var deflateStream = new DeflateStream(
                new MemoryStream(compressed, 2, compressed.Length - 6, writable: false),
                CompressionMode.Decompress))
            {
                deflateStream.CopyTo(decompressed);
            }

            var decompressedBytes = decompressed.ToArray();
            var expectedAdler = (uint)(compressed[compressed.Length - 4] << 24 |
                                       compressed[compressed.Length - 3] << 16 |
                                       compressed[compressed.Length - 2] << 8 |
                                       compressed[compressed.Length - 1]);
            if (UpdateAdler32(1u, decompressedBytes, 0, decompressedBytes.Length) != expectedAdler)
                throw new InvalidDataException("Zlib Adler-32 checksum mismatch.");

            decompressed.Position = 0;
            return decompressed;
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
