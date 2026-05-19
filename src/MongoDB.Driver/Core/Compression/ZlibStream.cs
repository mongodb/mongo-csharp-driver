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
using System.IO;
using System.IO.Compression;

namespace MongoDB.Driver.Core.Compression
{
    // Implements RFC 1950 (zlib framing) around System.IO.Compression.DeflateStream (RFC 1951),
    // with manual Adler-32 checksum handling.
    //
    // Both compress and decompress initialise lazily — no I/O happens in the constructor.
    // Decompress path is fully streaming: the 2-byte header is read up front, then a
    // TrailerReservingStream wraps the remaining input and always holds back the last 4 bytes
    // (the Adler-32 trailer) while streaming everything before that to DeflateStream. Adler-32
    // is accumulated incrementally as the caller reads, then validated when DeflateStream
    // signals EOF. Peak allocation is independent of message size.
    internal sealed class ZlibStream : Stream
    {
        // RFC 1950 headers: CMF=0x78 (deflate, 32K window) + FLG satisfying (CMF*256+FLG)%31==0.
        // FLEVEL bits in FLG are informational only (RFC 1950 §2.2) and do not affect decompression.
        private static readonly byte[] __sDefaultHeader = { 0x78, 0x9C };       // FLEVEL=2
        private static readonly byte[] __sNoCompressionHeader = { 0x78, 0x01 }; // FLEVEL=0

        private readonly Stream _stream;
        private readonly CompressionMode _mode;
        private readonly CompressionLevel _level;
        private readonly bool _leaveOpen;

        // Compress-mode state (initialized lazily on first Write)
        private DeflateStream _compressDeflate;
        private uint _compressAdler32 = 1u;
        private bool _compressInitialized;

        // Decompress-mode state (initialized lazily on first Read)
        private DeflateStream _decompressDeflate;
        private TrailerReservingStream _decompressSource;
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
                // DeflateStream can detect BFINAL=1 inside bytes it has already pulled, signaling EOF
                // to us without ever asking our source for one more byte. Force one more pull on
                // TrailerReservingStream so it observes source EOF and finalizes the held-back trailer.
                // Any byte produced here means there was non-trailer data after the deflate payload.
                var sink = new byte[1];
                if (_decompressSource.Read(sink, 0, 1) != 0)
                {
                    throw new InvalidDataException("Unexpected trailing bytes after end of deflate stream.");
                }
                if (!_decompressSource.TryGetTrailer(out var trailer))
                {
                    throw new InvalidDataException("Truncated zlib stream — Adler-32 trailer missing.");
                }
                var expected = (uint)(trailer[0] << 24 | trailer[1] << 16 | trailer[2] << 8 | trailer[3]);
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
                    // Always emit a valid zlib stream, even if Write was never called.
                    // System.IO.Compression.DeflateStream emits zero bytes for empty input, which
                    // would yield a header+adler-only stream with no deflate payload — technically
                    // malformed per RFC 1951. Write the empty-final-block marker (03 00) ourselves
                    // in that case so the output is a well-formed RFC 1950 empty zlib stream.
                    EnsureHeaderWritten();
                    if (_compressDeflate != null)
                    {
                        _compressDeflate.Dispose();
                    }
                    else
                    {
                        _stream.WriteByte(0x03); // empty final deflate block (BFINAL=1, BTYPE=01, EOB code)
                        _stream.WriteByte(0x00);
                    }
                    _stream.WriteByte((byte)(_compressAdler32 >> 24));
                    _stream.WriteByte((byte)(_compressAdler32 >> 16));
                    _stream.WriteByte((byte)(_compressAdler32 >> 8));
                    _stream.WriteByte((byte)_compressAdler32);
                }
                else
                {
                    _decompressDeflate?.Dispose();
                    _decompressSource?.Dispose();
                }
                if (!_leaveOpen)
                    _stream.Dispose();
            }
            base.Dispose(disposing);
        }

        private void EnsureHeaderWritten()
        {
            if (_compressInitialized)
            {
                return;
            }
            _compressInitialized = true;

            var header = _level == CompressionLevel.NoCompression ? __sNoCompressionHeader : __sDefaultHeader;
            _stream.Write(header, 0, 2);
        }

        private void EnsureCompressInitialized()
        {
            EnsureHeaderWritten();
            _compressDeflate ??= new DeflateStream(_stream, _level, leaveOpen: true);
        }

        private void EnsureDecompressInitialized()
        {
            if (_decompressInitialized)
            {
                return;
            }
            _decompressInitialized = true;

            // Read and validate the 2-byte zlib header up front.
            var header = new byte[2];
            var headerRead = 0;
            while (headerRead < 2)
            {
                var got = _stream.Read(header, headerRead, 2 - headerRead);
                if (got == 0)
                {
                    throw new InvalidDataException("Compressed data is too short to be a valid zlib stream.");
                }
                headerRead += got;
            }

            ValidateZlibHeader(header[0], header[1]);

            // Wrap the remaining input (deflate payload + 4-byte Adler-32 trailer) in a
            // TrailerReservingStream. It feeds the deflate payload to DeflateStream and holds
            // back the final 4 bytes so we can validate the Adler-32 once DeflateStream signals EOF.
            _decompressSource = new TrailerReservingStream(_stream);
            _decompressDeflate = new DeflateStream(_decompressSource, CompressionMode.Decompress);
        }

        // Exception types track System.IO.Compression.ZLibStream's contract so the custom impl on
        // older TFMs is a near-drop-in match for the BCL impl that will eventually replace it.
        // - Bad header: InvalidDataException (matches BCL exactly).
        // - FDICT: InvalidDataException (BCL throws ZLibException, but that type is internal on
        //   net472 and not on the public netstandard2.1/net6.0 surface, so we can't construct it
        //   here. Both InvalidDataException and ZLibException derive from IOException, so callers
        //   that catch IOException — the realistic catch level on net6.0 where ZLibException is
        //   internal — see consistent behavior across TFMs and across the eventual cleanup.)
        private static void ValidateZlibHeader(byte cmf, byte flg)
        {
            // RFC 1950 §2.2: CMF low nibble must be 8 (deflate); (CMF*256+FLG) must be divisible by 31.
            if ((cmf & 0x0F) != 8 || (cmf * 256 + flg) % 31 != 0)
            {
                throw new InvalidDataException("Invalid zlib header.");
            }

            // RFC 1950 §2.2: FDICT (FLG bit 5). MongoDB never sends preset dictionaries.
            if ((flg & 0x20) != 0)
            {
                throw new InvalidDataException("Zlib preset dictionaries (FDICT) are not supported.");
            }
        }

        private static uint UpdateAdler32(uint adler, byte[] buf, int offset, int count)
        {
            // RFC 1950 §9. NMAX is the largest n such that 255*n*(n+1)/2 + (n+1)*(BASE-1) fits in 32 bits,
            // letting us defer the modulo until the end of each chunk instead of taking it per byte.
            const uint Base = 65521;
            const int Nmax = 5552;
            var s1 = adler & 0xFFFF;
            var s2 = (adler >> 16) & 0xFFFF;
            while (count > 0)
            {
                var k = count < Nmax ? count : Nmax;
                count -= k;
                for (var i = 0; i < k; i++)
                {
                    s1 += buf[offset + i];
                    s2 += s1;
                }
                offset += k;
                s1 %= Base;
                s2 %= Base;
            }
            return (s2 << 16) | s1;
        }

        // Reads from a source stream and always holds back the last 4 bytes — once the source
        // EOFs, those 4 bytes are the RFC 1950 Adler-32 trailer. Everything before that is
        // streamed through to the caller (DeflateStream). Lets decompression run with bounded
        // memory instead of buffering the whole compressed payload to slice off the trailer.
        private sealed class TrailerReservingStream : Stream
        {
#pragma warning disable CA2213 // Source stream is owned by the outer ZlibStream, not by us.
            private readonly Stream _source;
#pragma warning restore CA2213
            private readonly byte[] _trailer = new byte[4];
            private int _trailerLen; // 0..4 — bytes currently held in _trailer.
            private bool _sourceEof;
            private byte[] _workspace;

            public TrailerReservingStream(Stream source)
            {
                _source = source;
            }

            public bool TryGetTrailer(out byte[] trailer)
            {
                if (_sourceEof && _trailerLen == 4)
                {
                    trailer = _trailer;
                    return true;
                }
                trailer = null;
                return false;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count <= 0 || _sourceEof)
                {
                    return 0;
                }

                // Prime _trailer to be full (4 bytes) before we can emit anything.
                while (_trailerLen < 4)
                {
                    var got = _source.Read(_trailer, _trailerLen, 4 - _trailerLen);
                    if (got == 0)
                    {
                        _sourceEof = true;
                        throw new InvalidDataException("Compressed data is too short to be a valid zlib stream.");
                    }
                    _trailerLen += got;
                }

                if (_workspace == null || _workspace.Length < count)
                {
                    _workspace = new byte[count];
                }
                var n = _source.Read(_workspace, 0, count);
                if (n == 0)
                {
                    // Source EOF. _trailer holds exactly the 4 final bytes — the Adler-32 trailer.
                    _sourceEof = true;
                    return 0;
                }

                // Combined window (oldest first): _trailer[0..4] then _workspace[0..n].
                // Emit the first n bytes to caller; rotate the newest 4 back into _trailer.
                if (n <= 4)
                {
                    Buffer.BlockCopy(_trailer, 0, buffer, offset, n);
                    Buffer.BlockCopy(_trailer, n, _trailer, 0, 4 - n);
                    Buffer.BlockCopy(_workspace, 0, _trailer, 4 - n, n);
                }
                else
                {
                    Buffer.BlockCopy(_trailer, 0, buffer, offset, 4);
                    Buffer.BlockCopy(_workspace, 0, buffer, offset + 4, n - 4);
                    Buffer.BlockCopy(_workspace, n - 4, _trailer, 0, 4);
                }
                return n;
            }

            public override bool CanRead => !_sourceEof;
            public override bool CanWrite => false;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
