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
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver.Core.Connections
{
    internal static class Socks5Helper
    {
        // Schemas for requests/responses are taken from the following RFCs:
        // SOCKS Protocol Version 5 - https://datatracker.ietf.org/doc/html/rfc1928
        // Username/Password Authentication for SOCKS V5 - https://datatracker.ietf.org/doc/html/rfc1929

        private const byte ProtocolVersion5 = 0x05;
        private const byte SubnegotiationVersion = 0x01;
        private const byte CmdConnect = 0x01;
        private const byte MethodNoAuth = 0x00;
        private const byte MethodUsernamePassword = 0x02;
        private const byte AddressTypeIPv4 = 0x01;
        private const byte AddressTypeDomain = 0x03;
        private const byte AddressTypeIPv6 = 0x04;
        private const byte Socks5Success = 0x00;

        private const int BufferSize = 512;

        //TODO Make an async version of this method
        public static void PerformSocks5Handshake(Stream stream, EndPoint endPoint, string proxyUsername, string proxyPassword, CancellationToken cancellationToken)
        {
            var (targetHost, targetPort) = endPoint.GetHostAndPort();

            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                var useAuth = !string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword);

                // Greeting request
                // +----+----------+----------+
                // |VER | NMETHODS | METHODS  |
                // +----+----------+----------+
                // | 1  |    1     | 1 to 255 |
                // +----+----------+----------+
                buffer[0] = ProtocolVersion5;

                if (!useAuth)
                {
                    buffer[1] = 1;
                    buffer[2] = MethodNoAuth;
                }
                else
                {
                    buffer[1] = 2;
                    buffer[2] = MethodNoAuth;
                    buffer[3] = MethodUsernamePassword;
                }

                stream.Write(buffer, 0, useAuth ? 4 : 3);
                stream.Flush();

                // Greeting response
                // +----+--------+
                // |VER | METHOD |
                // +----+--------+
                // | 1  |   1    |
                // +----+--------+

                stream.ReadBytes(buffer, 0,2, cancellationToken);

                VerifyProtocolVersion(buffer[0]);

                var method = buffer[1];
                if (method == MethodUsernamePassword)
                {
                    if (!useAuth)
                    {
                        //We should not reach here
                        throw new IOException("SOCKS5 proxy requires authentication, but no credentials were provided.");
                    }

                    // Authentication request
                    // +----+------+----------+------+----------+
                    // |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
                    // +----+------+----------+------+----------+
                    // | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                    // +----+------+----------+------+----------+
                    buffer[0] = SubnegotiationVersion;
#if NET472
                    var usernameLength = EncodeString(proxyUsername, buffer, 2, nameof(proxyUsername));
#else
                    var usernameLength = EncodeString(proxyUsername, buffer.AsSpan(2), nameof(proxyUsername));
#endif
                    buffer[1] = usernameLength;
#if NET472
                    var passwordLength = EncodeString(proxyPassword, buffer, 3 + usernameLength, nameof(proxyPassword));
#else
                    var passwordLength = EncodeString(proxyPassword, buffer.AsSpan(3 + usernameLength), nameof(proxyPassword));
#endif
                    buffer[2 + usernameLength] = passwordLength;

                    var authLength = 3 + usernameLength + passwordLength;
                    stream.Write(buffer, 0, authLength);
                    stream.Flush();

                    // Authentication response
                    // +----+--------+
                    // |VER | STATUS |
                    // +----+--------+
                    // | 1  |   1    |
                    // +----+--------+
                    stream.ReadBytes(buffer, 0,2, cancellationToken);
                    if (buffer[0] != SubnegotiationVersion || buffer[1] != Socks5Success)
                    {
                        throw new IOException("SOCKS5 authentication failed.");
                    }
                }
                else if (method != MethodNoAuth)
                {
                    throw new IOException("SOCKS5 proxy requires unsupported authentication method.");
                }

                // Connect request
                // +----+-----+-------+------+----------+----------+
                // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                buffer[0] = ProtocolVersion5;
                buffer[1] = CmdConnect;
                buffer[2] = 0x00;
                var addressLength = 0;

                //TODO Can we avoid doing this...?
                if (IPAddress.TryParse(targetHost, out var ip))
                {
                    switch (ip.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            buffer[3] = AddressTypeIPv4;
#if !NET472
                            ip.TryWriteBytes(buffer.AsSpan(4), out _);
#endif
                            addressLength = 4;
                            break;
                        case AddressFamily.InterNetworkV6:
                            buffer[3] = AddressTypeIPv6;
#if !NET472
                            ip.TryWriteBytes(buffer.AsSpan(4), out _);
#endif
                            addressLength = 16;
                            break;
                        default:
                            throw new IOException("Invalid target host address family. Only IPv4 and IPv6 are supported.");
                    }
                }
                else
                {
                    buffer[3] = AddressTypeDomain;
#if NET472
                    var hostLength = EncodeString(targetHost, buffer, 5, nameof(targetHost));
#else
                    var hostLength = EncodeString(targetHost, buffer.AsSpan(5), nameof(targetHost));
#endif
                    buffer[4] = hostLength;
                    addressLength = hostLength + 1;
                }

                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(addressLength + 4), (ushort)targetPort);

                stream.Write(buffer, 0, addressLength + 6);
                stream.Flush();

                // Connect response
                // +----+-----+-------+------+----------+----------+
                // |VER | REP |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                stream.ReadBytes(buffer, 0,5, cancellationToken);
                VerifyProtocolVersion(buffer[0]);
                if (buffer[1] != Socks5Success)
                {
                    throw new IOException($"SOCKS5 connect failed with code 0x{buffer[1]:X2}");
                }

                var skip = buffer[3] switch
                {
                    AddressTypeIPv4 => 5,
                    AddressTypeIPv6 => 17,
                    AddressTypeDomain => buffer[4] + 2,
                    _ => throw new IOException("Unknown address type in SOCKS5 reply.")
                };

                stream.ReadBytes(buffer, 0, skip, cancellationToken);
                // Address and port in response are ignored
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void VerifyProtocolVersion(byte version)
        {
            if (version != ProtocolVersion5)
            {
                throw new IOException("Invalid SOCKS version in method selection response.");
            }
        }

#if NET472
        private static byte EncodeString(string input, byte[] buffer, int offset, string parameterName)
        {
            try
            {
                var written = Encoding.UTF8.GetBytes(input, 0, input.Length, buffer, offset);
                return checked((byte)written);
            }
            catch
            {
                throw new IOException($"The {parameterName} could not be encoded as UTF-8.");
            }
        }
#else
        private static byte EncodeString(ReadOnlySpan<char> chars, Span<byte> buffer, string parameterName)
        {
            try
            { //TODO Maybe we should remove checked?
                return checked((byte)Encoding.UTF8.GetBytes(chars, buffer));
            }
            catch
            {
                throw new IOException($"The {parameterName} could not be encoded as UTF-8.");
            }
        }
#endif
    }
}