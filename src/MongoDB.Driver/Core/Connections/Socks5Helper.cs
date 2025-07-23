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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver.Core.Connections
{
    internal static class Socks5Helper
    {
        // Schemas for requests/responses are taken from the following RFCs:
        // SOCKS Protocol Version 5 - https://datatracker.ietf.org/doc/html/rfc1928
        // Username/Password Authentication for SOCKS V5 - https://datatracker.ietf.org/doc/html/rfc1929

        // Greeting request
        // +----+----------+----------+
        // |VER | NMETHODS | METHODS  |
        // +----+----------+----------+
        // | 1  |    1     | 1 to 255 |
        // +----+----------+----------+

        // Greeting response
        // +----+--------+
        // |VER | METHOD |
        // +----+--------+
        // | 1  |   1    |
        // +----+--------+

        // Authentication request -- if using username/password authentication
        // +----+------+----------+------+----------+
        // |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
        // +----+------+----------+------+----------+
        // | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
        // +----+------+----------+------+----------+

        // Authentication response
        // +----+--------+
        // |VER | STATUS |
        // +----+--------+
        // | 1  |   1    |
        // +----+--------+

        // Connect request
        // +----+-----+-------+------+----------+----------+
        // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
        // +----+-----+-------+------+----------+----------+
        // | 1  |  1  | X'00' |  1   | Variable |    2     |
        // +----+-----+-------+------+----------+----------+

        // Connect response
        // +----+-----+-------+------+----------+----------+
        // |VER | REP |  RSV  | ATYP | DST.ADDR | DST.PORT |
        // +----+-----+-------+------+----------+----------+
        // | 1  |  1  | X'00' |  1   | Variable |    2     |
        // +----+-----+-------+------+----------+----------+

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

        public static void PerformSocks5Handshake(Stream stream, EndPoint endPoint, Socks5AuthenticationSettings authenticationSettings, CancellationToken cancellationToken)
        {
            var (targetHost, targetPort) = endPoint.GetHostAndPort();
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                var useAuth = authenticationSettings is Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings;

                CreateGreetingRequest(buffer, useAuth);

                stream.Write(buffer, 0, useAuth ? 4 : 3);
                stream.Flush();

                ReadGreetingResponse(stream, buffer, useAuth, authenticationSettings, cancellationToken);

                var addressLength = CreateConnectRequest(buffer, targetHost, targetPort);
                stream.Write(buffer, 0, addressLength + 6);
                stream.Flush();

                stream.ReadBytes(buffer, 0, 5, cancellationToken);
                var skip = ReadConnectResponse(buffer, cancellationToken);
                stream.ReadBytes(buffer, 0, skip, cancellationToken);

            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static async Task PerformSocks5HandshakeAsync(Stream stream, EndPoint endPoint, Socks5AuthenticationSettings authenticationSettings, CancellationToken cancellationToken)
        {
            var (targetHost, targetPort) = endPoint.GetHostAndPort();
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                var useAuth = authenticationSettings is Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings;

                CreateGreetingRequest(buffer, useAuth);
                await stream.WriteAsync(buffer, 0, useAuth ? 4 : 3, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                await ReadGreetingResponseAsync(stream, buffer, useAuth, authenticationSettings, cancellationToken).ConfigureAwait(false);

                var addressLength = CreateConnectRequest(buffer, targetHost, targetPort);
                await stream.WriteAsync(buffer, 0, addressLength + 6, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                await stream.ReadBytesAsync(buffer, 0, 5, cancellationToken).ConfigureAwait(false);
                var skip = ReadConnectResponse(buffer, cancellationToken);
                await stream.ReadBytesAsync(buffer, 0, skip, cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void ReadGreetingResponse(Stream stream, byte[] buffer, bool useAuth, Socks5AuthenticationSettings authenticationSettings, CancellationToken cancellationToken)
        {
            stream.ReadBytes(buffer, 0, 2, cancellationToken);
            VerifyProtocolVersion(buffer[0]);
            var method = buffer[1];
            if (method == MethodUsernamePassword)
            {
                if (!useAuth)
                {
                    throw new IOException("SOCKS5 proxy requires authentication, but no credentials were provided.");
                }
                PerformUsernamePasswordAuth(stream, buffer, authenticationSettings, cancellationToken);
            }
            else if (method != MethodNoAuth)
            {
                throw new IOException("SOCKS5 proxy requires unsupported authentication method.");
            }
        }

        private static async Task ReadGreetingResponseAsync(Stream stream, byte[] buffer, bool useAuth, Socks5AuthenticationSettings authenticationSettings, CancellationToken cancellationToken)
        {
            await stream.ReadBytesAsync(buffer, 0, 2, cancellationToken).ConfigureAwait(false);
            VerifyProtocolVersion(buffer[0]);
            var method = buffer[1];
            if (method == MethodUsernamePassword)
            {
                if (!useAuth)
                {
                    throw new IOException("SOCKS5 proxy requires authentication, but no credentials were provided.");
                }
                await PerformUsernamePasswordAuthAsync(stream, buffer, authenticationSettings, cancellationToken).ConfigureAwait(false);
            }
            else if (method != MethodNoAuth)
            {
                throw new IOException("SOCKS5 proxy requires unsupported authentication method.");
            }
        }

        private static void PerformUsernamePasswordAuth(Stream stream, byte[] buffer, Socks5AuthenticationSettings authenticationSettings, CancellationToken cancellationToken)
        {
            var usernamePasswordAuthenticationSettings = (Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings)authenticationSettings;
            var proxyUsername = usernamePasswordAuthenticationSettings.Username;
            var proxyPassword = usernamePasswordAuthenticationSettings.Password;

            buffer[0] = SubnegotiationVersion;
            var usernameLength = EncodeString(proxyUsername, buffer, 2, nameof(proxyUsername));
            buffer[1] = usernameLength;
            var passwordLength = EncodeString(proxyPassword, buffer, 3 + usernameLength, nameof(proxyPassword));
            buffer[2 + usernameLength] = (byte)passwordLength;

            var authLength = 3 + usernameLength + passwordLength;
            stream.Write(buffer, 0, authLength);
            stream.Flush();

            stream.ReadBytes(buffer, 0, 2, cancellationToken);
            if (buffer[0] != SubnegotiationVersion || buffer[1] != Socks5Success)
            {
                throw new IOException("SOCKS5 authentication failed.");
            }
        }

        private static async Task PerformUsernamePasswordAuthAsync(Stream stream, byte[] buffer, Socks5AuthenticationSettings authenticationSettings, CancellationToken cancellationToken)
        {
            var usernamePasswordAuthenticationSettings = (Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings)authenticationSettings;
            var proxyUsername = usernamePasswordAuthenticationSettings.Username;
            var proxyPassword = usernamePasswordAuthenticationSettings.Password;

            buffer[0] = SubnegotiationVersion;
            var usernameLength = EncodeString(proxyUsername, buffer, 2, nameof(proxyUsername));
            buffer[1] = usernameLength;
            var passwordLength = EncodeString(proxyPassword, buffer, 3 + usernameLength, nameof(proxyPassword));
            buffer[2 + usernameLength] = passwordLength;

            var authLength = 3 + usernameLength + passwordLength;
            await stream.WriteAsync(buffer, 0, authLength, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            await stream.ReadBytesAsync(buffer, 0, 2, cancellationToken).ConfigureAwait(false);
            if (buffer[0] != SubnegotiationVersion || buffer[1] != Socks5Success)
            {
                throw new IOException("SOCKS5 authentication failed.");
            }
        }

        private static int CreateConnectRequest(byte[] buffer, string targetHost, int targetPort)
        {
            buffer[0] = ProtocolVersion5;
            buffer[1] = CmdConnect;
            buffer[2] = 0x00;
            var addressLength = 0;

            if (IPAddress.TryParse(targetHost, out var ip))
            {
                switch (ip.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        buffer[3] = AddressTypeIPv4;
                        Array.Copy(ip.GetAddressBytes(), 0, buffer, 4, 4);
                        addressLength = 4;
                        break;
                    case AddressFamily.InterNetworkV6:
                        buffer[3] = AddressTypeIPv6;
                        Array.Copy(ip.GetAddressBytes(), 0, buffer, 4, 16);
                        addressLength = 16;
                        break;
                    default:
                        throw new IOException("Invalid target host address family.");
                }
            }
            else
            {
                buffer[3] = AddressTypeDomain;
                var hostLength = EncodeString(targetHost, buffer, 5, nameof(targetHost));
                buffer[4] = hostLength;
                addressLength = hostLength + 1;
            }

            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(addressLength + 4), (ushort)targetPort);

            return addressLength;
        }

        private static int ReadConnectResponse(byte[] buffer, CancellationToken cancellationToken)
        {
            VerifyProtocolVersion(buffer[0]);
            VerifySockSuccess(buffer[1]);

            return buffer[3] switch
            {
                AddressTypeIPv4 => 5,
                AddressTypeIPv6 => 17,
                AddressTypeDomain => buffer[4] + 2,
                _ => throw new IOException("Unknown address type in SOCKS5 reply.")
            };
        }

        private static void CreateGreetingRequest(byte[] buffer, bool useAuth)
        {
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
        }

        private static void VerifyProtocolVersion(byte version)
        {
            if (version != ProtocolVersion5)
            {
                throw new IOException("Invalid SOCKS version in response.");
            }
        }

        private static void VerifySockSuccess(byte value) //TODO Need to check this
        {
            if (value != Socks5Success)
            {
                throw new IOException($"SOCKS5 connect failed");
            }
        }

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
    }
}