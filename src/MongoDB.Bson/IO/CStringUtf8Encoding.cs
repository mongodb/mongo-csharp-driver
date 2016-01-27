/* Copyright 2016 MongoDB Inc.
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
using System.Text;

namespace MongoDB.Bson.IO
{
    internal static class CStringUtf8Encoding
    {
        public static int GetBytes(string value, byte[] bytes, int byteIndex, UTF8Encoding fallbackEncoding)
        {
            var charLength = value.Length;
            var initialByteIndex = byteIndex;

            for (var charIndex = 0; charIndex < charLength; charIndex++)
            {
                var c = (int)value[charIndex];
                if (c == 0)
                {
                    throw new ArgumentException("A CString cannot contain null bytes.", "value");
                }
                else if (c <= 0x7f)
                {
                    bytes[byteIndex++] = (byte)c;
                }
                else if (c <= 0x7ff)
                {
                    var byte1 = 0xc0 | (c >> 6);
                    var byte2 = 0x80 | (c & 0x3f);
                    bytes[byteIndex++] = (byte)byte1;
                    bytes[byteIndex++] = (byte)byte2;
                }
                else if (c <= 0xd7ff || c >= 0xe000)
                {
                    var byte1 = 0xe0 | (c >> 12);
                    var byte2 = 0x80 | ((c >> 6) & 0x3f);
                    var byte3 = 0x80 | (c & 0x3f);
                    bytes[byteIndex++] = (byte)byte1;
                    bytes[byteIndex++] = (byte)byte2;
                    bytes[byteIndex++] = (byte)byte3;
                }
                else
                {
                    // let fallback encoding handle surrogate pairs
                    var bytesWritten = fallbackEncoding.GetBytes(value, 0, value.Length, bytes, byteIndex);
                    if (Array.IndexOf<byte>(bytes, 0, initialByteIndex, bytesWritten) != -1)
                    {
                        throw new ArgumentException("A CString cannot contain null bytes.", "value");
                    }
                    return bytesWritten;
                }
            }

            return byteIndex - initialByteIndex;
        }

        public static int GetMaxByteCount(int charCount)
        {
            return charCount * 3;
        }

        public static bool TryGetCString(IByteBuffer buffer, int position, out string value, out int bytesRead)
        {
            value = null;
            bytesRead = 0;
            var sb = new StringBuilder();
            var originalPosition = position;
            var byteSource = new ByteBufferByteSource(buffer, position);

            while (true)
            {
                var b = byteSource.GetByte();
                char c;
                if (b == 0)
                {
                    value = sb.ToString();
                    bytesRead = byteSource.GetBytesRead();
                    return true;
                }
                else if ((b & 0x80) == 0)
                {
                    c = (char)b;
                }
                else if ((b & 0xe0) == 0xc0)
                {
                    var byte1 = b;
                    var byte2 = byteSource.GetByte();
                    if ((byte2 & 0xc0) != 0x80)
                    {
                        return false; // invalid continuation byte
                    }
                    var nibble1 = byte1 & 0x1f;
                    var nibble2 = byte2 & 0x3f;
                    var code = (nibble1 << 6) | nibble2;
                    if (code < 0x80)
                    {
                        return false; // invalid overlong encoding
                    }
                    c = (char)code;
                }
                else if ((b & 0xf0) == 0xe0)
                {
                    var byte1 = b;
                    var byte2 = byteSource.GetByte();
                    var byte3 = byteSource.GetByte();
                    if ((byte2 & 0xc0) != 0x80 || (byte3 & 0xc0) != 0x80)
                    {
                        return false; // invalid continuation byte
                    }
                    var nibble1 = byte1 & 0x0f;
                    var nibble2 = byte2 & 0x3f;
                    var nibble3 = byte3 & 0x3f;
                    var code = (nibble1 << 12) | (nibble2 << 6) | nibble3;
                    if (code < 0x800)
                    {
                        return false; // invalid overlong encoding
                    }
                    if (code >= 0xd800 && code <= 0xdfff)
                    {
                        return false; // invalid surrogate
                    }
                    c = (char)code;
                }
                else
                {
                    return false; // anything else we don't recognize
                }
                sb.Append(c);
            }
        }

        // nested types
        private class ByteBufferByteSource
        {
            private byte[] _array;
            private readonly IByteBuffer _buffer;
            private int _count;
            private int _offset;
            private int _originalPosition;
            private int _position;


            public ByteBufferByteSource(IByteBuffer buffer, int position)
            {
                _buffer = buffer;
                _position = position;
                _originalPosition = position;
                MoveToNextSegment();
            }

            public byte GetByte()
            {
                if (_count == 0)
                {
                    MoveToNextSegment();
                }

                _count -= 1;
                return _array[_offset++];
            }

            public int GetBytesRead()
            {
                return (_position - _originalPosition) - _count;
            }

            private void MoveToNextSegment()
            {
                var segment = _buffer.AccessBackingBytes(_position);
                _array = segment.Array;
                _offset = segment.Offset;
                _count = segment.Count;
                if (_count == 0)
                {
                    throw new EndOfStreamException();
                }
                _position += _count;
            }
        }
    }
}
