/* Copyright 2010-2013 10gen Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// A managed implementation of the DIGEST-MD5 sasl spec (http://www.ietf.org/rfc/rfc2831.txt).
    /// </summary>
    internal class ManagedDigestMD5Implementation : SaslImplementationBase, ISaslStep
    {
        // private fields
        private readonly byte[] _cnonce;
        private readonly string _digestUri;
        private readonly string _nonceCount;
        private readonly string _qop;
        private readonly string _password;
        private readonly string _username;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedDigestMD5Implementation" /> class.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public ManagedDigestMD5Implementation(string serverName, string username, string password)
        {
            _cnonce = CreateClientNonce();
            _digestUri = "mongodb/" + serverName;
            _nonceCount = "00000001";
            _qop = "auth";
            _password = password;
            _username = username;
        }

        // public properties
        /// <summary>
        /// The bytes that should be sent to ther server before calling Transition.
        /// </summary>
        public byte[] BytesToSendToServer
        {
            get { return new byte[0]; }
        }

        // public methods
        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from the server.</param>
        /// <returns>An ISaslStep.</returns>
        public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
        {
            var directives = DirectiveParser.Parse(bytesReceivedFromServer);
            var encoding = Encoding.UTF8;

            var sb = new StringBuilder();
            sb.AppendFormat("username=\"{0}\"", _username);
            sb.AppendFormat(",nonce=\"{0}\"", encoding.GetString(directives["nonce"]).Replace("\"", "\\\""));
            sb.AppendFormat(",cnonce=\"{0}\"", encoding.GetString(_cnonce).Replace("\"", "\\\""));
            sb.AppendFormat(",nc={0}", _nonceCount);
            sb.AppendFormat(",qop={0}", _qop);
            sb.AppendFormat(",digest-uri=\"{0}\"", _digestUri);
            sb.AppendFormat(",response={0}", ComputeResponse(encoding, directives["nonce"]));
            sb.Append(",charset=\"utf-8\"");

            return new ManagedDigestMD5FinalStep(encoding.GetBytes(sb.ToString()));
        }

        // private methods
        private string ComputeResponse(Encoding encoding, byte[] nonce)
        {
            using (var md5 = MD5.Create())
            {
                var a1 = ComputeA1(encoding, md5, nonce);
                var a2 = ComputeA2(encoding);

                var a1Hash = md5.ComputeHash(a1);
                var a2Hash = md5.ComputeHash(a2);

                var a1Hex = ToHexString(a1Hash);
                var a2Hex = ToHexString(a2Hash);

                var kd = new List<byte>();
                kd.AddRange(encoding.GetBytes(a1Hex));
                kd.Add((byte)':');
                kd.AddRange(nonce);
                kd.Add((byte)':');
                kd.AddRange(encoding.GetBytes(_nonceCount));
                kd.Add((byte)':');
                kd.AddRange(_cnonce);
                kd.Add((byte)':');
                kd.AddRange(encoding.GetBytes(_qop));
                kd.Add((byte)':');
                kd.AddRange(encoding.GetBytes(a2Hex));

                var kdHash = md5.ComputeHash(kd.ToArray());
                return ToHexString(kdHash);
            }
        }

        private byte[] ComputeA1(Encoding encoding, MD5 md5, byte[] nonce)
        {
            // User Token
            var userToken = new List<byte>();
            userToken.AddRange(encoding.GetBytes(_username));
            userToken.Add((byte)':');
            userToken.Add((byte)':');
            var passwordBytes = GetMongoPassword(md5, encoding, _username, _password);
            var passwordHex = ToHexString(passwordBytes);
            userToken.AddRange(encoding.GetBytes(passwordHex));
            var userTokenBytes = md5.ComputeHash(userToken.ToArray());

            var nonceBytes = new List<byte>();
            nonceBytes.Add((byte)':');
            nonceBytes.AddRange(nonce);
            nonceBytes.Add((byte)':');
            nonceBytes.AddRange(_cnonce);

            var result = new byte[userTokenBytes.Length + nonceBytes.Count];
            userTokenBytes.CopyTo(result, 0);
            nonceBytes.CopyTo(result, userTokenBytes.Length);

            return result;
        }

        private byte[] ComputeA2(Encoding encoding)
        {
            return encoding.GetBytes("AUTHENTICATE:" + _digestUri);
        }

        private byte[] CreateClientNonce()
        {
            return Encoding.UTF8.GetBytes(new Random().Next(1234000, 99999999).ToString());
        }

        // nested classes
        private class ManagedDigestMD5FinalStep : ISaslStep
        {
            private readonly byte[] _bytesToSendToServer;

            public ManagedDigestMD5FinalStep(byte[] bytesToSendToServer)
            {
                _bytesToSendToServer = bytesToSendToServer;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                return new SaslCompletionStep(new byte[0]);
            }
        }

        private static class DirectiveParser
        {
            public static Dictionary<string, byte[]> Parse(byte[] bytes)
            {
                var s = Encoding.UTF8.GetString(bytes);

                var parsed = new Dictionary<string, byte[]>();
                var index = 0;
                while (index < bytes.Length)
                {
                    var key = ParseKey(bytes, ref index);
                    SkipWhitespace(bytes, ref index);
                    if (bytes[index] != '=')
                    {
                        throw new MongoSecurityException(string.Format("Expected a '=' after a key \"{0}\"."));
                    }
                    else if (key.Length == 0)
                    {
                        throw new MongoSecurityException("Empty directive key.");
                    }
                    index++; // skip =

                    var value = ParseValue(key, bytes, ref index);
                    parsed.Add(key, value);
                    if (index >= bytes.Length)
                    {
                        break;
                    }
                    else if (bytes[index] != ',')
                    {
                        throw new MongoSecurityException(string.Format("Expected a ',' after directive \"{0}\".", key));
                    }
                    else
                    {
                        index++;
                        SkipWhitespace(bytes, ref index);
                    }
                }

                return parsed;
            }

            private static string ParseKey(byte[] bytes, ref int index)
            {
                var key = new StringBuilder();
                while (index < bytes.Length)
                {
                    var b = bytes[index];
                    if (b == ',')
                    {
                        if (key.Length == 0)
                        {
                            // there were some extra commas, so we skip over them 
                            // and try to find the next key
                            index++;
                            SkipWhitespace(bytes, ref index);
                        }
                        else
                        {
                            throw new MongoSecurityException(string.Format("Directive key \"{0}\" contains a ','.", key.ToString()));
                        }
                    }
                    else if (b == '=')
                    {
                        break;
                    }
                    else if (IsWhiteSpace(b))
                    {
                        index++;
                        break;
                    }
                    else
                    {
                        index++;
                        key.Append((char)b);
                    }
                }

                return key.ToString();
            }

            private static byte[] ParseValue(string key, byte[] bytes, ref int index)
            {
                List<byte> value = new List<byte>();
                bool isQuoted = false;
                while (index < bytes.Length)
                {
                    var b = bytes[index];
                    if (b == '\\')
                    {
                        index++; // skip escape
                        if (index < bytes.Length)
                        {
                            value.Add(b);
                            index++;
                        }
                        else
                        {
                            throw new MongoSecurityException(string.Format("Unmatched quote found in value of directive key \"{0}\".", key));
                        }
                    }
                    else if (b == '"')
                    {
                        index++;
                        if (isQuoted)
                        {
                            // we have closed the quote...
                            break;
                        }
                        else if (value.Count == 0)
                        {
                            isQuoted = true;
                        }
                        else
                        {
                            // quote in the middle of an unquoted string
                            value.Add(b);
                        }
                    }
                    else
                    {
                        if (b == ',' && !isQuoted)
                        {
                            break;
                        }
                        value.Add(b);
                        index++;
                    }
                }

                return value.ToArray();
            }

            private static bool IsWhiteSpace(byte b)
            {
                switch (b)
                {
                    case 13:   // US-ASCII CR, carriage return
                    case 10:   // US-ASCII LF, linefeed
                    case 32:   // US-ASCII SP, space
                    case 9:    // US-ASCII HT, horizontal-tab
                        return true;
                }
                return false;
            }

            private static void SkipWhitespace(byte[] bytes, ref int index)
            {
                for (; index < bytes.Length; index++)
                {
                    if (!IsWhiteSpace(bytes[index]))
                    {
                        return;
                    }
                }
            }
        }
    }
}