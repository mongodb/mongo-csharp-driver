/* Copyright 2010-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// Implements the SCRAM-SHA1 rfc: http://tools.ietf.org/html/rfc5802.
    /// </summary>
    internal class ScramSha1Mechanism : ISaslMechanism
    {
        // private static fields
        [ThreadStatic]
        private readonly static Random __random = new Random((int)DateTime.Now.Ticks);

        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "SCRAM-SHA-1"; }
        }

        // public methods
        /// <summary>
        /// Determines whether this instance can authenticate with the specified credential.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can authenticate with the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoConnection connection, MongoCredential credential)
        {
            return connection.ServerInstance.Supports(FeatureId.ScramSha1) &&
                (credential.Mechanism == null || credential.Mechanism.Equals(Name, StringComparison.InvariantCultureIgnoreCase)) &&
                credential.Evidence is PasswordEvidence;
        }

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>
        /// The initial step.
        /// </returns>
        public ISaslStep Initialize(MongoConnection connection, MongoCredential credential)
        {
            var gs2Header = "n,,";
            var username = "n=" + PrepUsername(credential.Username);
            var r =GenerateRandomString();
            var nonce = "r=" + r;

            var clientFirstMessageBare = username + "," + nonce;
            var clientFirstMessage = gs2Header + clientFirstMessageBare;

            return new ClientFirst(
                Encoding.UTF8.GetBytes(clientFirstMessage),
                clientFirstMessageBare, 
                credential, 
                r);
        }

        private string GenerateRandomString()
        {
            const int count = 24; // this is what the RFC uses, although it is unspecified
            const int comma = 44;
            const int low = 33;
            const int high = 126;
            const int range = high - low;

            var builder = new StringBuilder();
            int ch;
            for (int i = 0; i < count; i++)
            {
                ch = __random.Next(range) + low;
                while (ch == comma)
                {
                    ch = __random.Next(range) + low;
                }

                builder.Append((char)ch);
            }

            return builder.ToString();
        }

        private string PrepUsername(string username)
        {
            return username.Replace("=", "=3D").Replace(",", "=2C");
        }

        private class ClientFirst : ISaslStep
        {
            private string _clientFirstMessageBare;
            private MongoCredential _credential;
            private readonly string _rPrefix;
            private readonly byte[] _bytesToSendToServer;

            public ClientFirst(byte[] bytesToSendToServer, string clientFirstMessageBare, MongoCredential credential, string rPrefix)
            {
                _bytesToSendToServer = bytesToSendToServer;
                _clientFirstMessageBare = clientFirstMessageBare;
                _credential = credential;
                _rPrefix = rPrefix;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                var serverFirstMessage = Encoding.UTF8.GetString(bytesReceivedFromServer);
                var map = NVParser.Parse(serverFirstMessage);

                var r = map['r'];
                if (!r.StartsWith(_rPrefix))
                {
                    throw new MongoSecurityException("Server sent invalid nonce.");
                }
                var s = map['s'];
                var i = map['i'];

                var gs2Header = "n,,";
                var channelBinding = "c=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(gs2Header));
                var nonce = "r=" + r;
                var clientFinalMessageWithoutProof = channelBinding + "," + nonce;

                var saltedPassword = Hi(
                    MongoUtils.Hash(_credential.Username + ":mongo:" + MongoUtils.ToInsecureString(((PasswordEvidence)_credential.Evidence).SecurePassword)),
                    Convert.FromBase64String(s),
                    int.Parse(i));
                var clientKey = HMAC(saltedPassword, "Client Key");
                var storedKey = H(clientKey);
                var authMessage = _clientFirstMessageBare + "," + serverFirstMessage + "," + clientFinalMessageWithoutProof;
                var clientSignature = HMAC(storedKey, authMessage);
                var clientProof = XOR(clientKey, clientSignature);
                var serverKey = HMAC(saltedPassword, "Server Key");
                var serverSignature = HMAC(serverKey, authMessage);

                var proof = "p=" + Convert.ToBase64String(clientProof);
                var clientFinalMessage = clientFinalMessageWithoutProof + "," + proof;

                return new ClientLast(Encoding.UTF8.GetBytes(clientFinalMessage), Convert.ToBase64String(serverSignature));
            }

            private static byte[] XOR(byte[] a, byte[] b)
            {
                var result = new byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    result[i] = (byte)(a[i] ^ b[i]);
                }

                return result;
            }

            private static byte[] H(byte[] data)
            {
                using (var sha1 = SHA1.Create())
                {
                    return sha1.ComputeHash(data);
                }
            }

            private static byte[] Hi(string password, byte[] salt, int iterations)
            {
                return new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations).GetBytes(20); // this is length of output of a sha-1 hmac
            }

            private static byte[] HMAC(byte[] data, string key)
            {
                using (var hmac = new HMACSHA1(data, true))
                {
                    return hmac.ComputeHash(Encoding.UTF8.GetBytes(key));
                }
            }
        }

        private class ClientLast : ISaslStep
        {
            private readonly byte[] _bytesToSendToServer;
            private readonly string _serverSignature64;

            public ClientLast(byte[] bytesToSendToServer, string serverSignature64)
            {
                _bytesToSendToServer = bytesToSendToServer;
                _serverSignature64 = serverSignature64;
            }

            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return false; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                var map = NVParser.Parse(Encoding.UTF8.GetString(bytesReceivedFromServer));

                var serverSignature = map['v'];

                if (_serverSignature64 != serverSignature)
                {
                    throw new MongoSecurityException("Server signature was invalid.");
                }

                return new SaslCompletionStep(bytesReceivedFromServer);
            }
        }

        private class NVParser
        {
            private const int EOF = -1;

            public static IDictionary<char, string> Parse(string text)
            {
                IDictionary<char, string> dict = new Dictionary<char, string>();

                using (var reader = new StringReader(text))
                {
                    while (reader.Peek() != EOF)
                    {
                        dict.Add(ReadKeyValue(reader));
                        if (reader.Peek() == ',')
                        {
                            Read(reader, ',');
                        }
                    }
                }

                return dict;
            }

            private static KeyValuePair<char, string> ReadKeyValue(TextReader reader)
            {
                var key = ReadKey(reader);
                Read(reader, '=');
                var value = ReadValue(reader);
                return new KeyValuePair<char, string>(key, value);
            }

            private static char ReadKey(TextReader reader)
            {
                // keys are of length 1.
                return (char)reader.Read();
            }

            private static void Read(TextReader reader, char expected)
            {
                var ch = (char)reader.Read();
                if (ch != expected)
                {
                    throw new IOException(string.Format("Expected {0} but found {1}.", expected, ch));
                }
            }

            private static string ReadValue(TextReader reader)
            {
                var sb = new StringBuilder();
                var ch = reader.Peek();
                while (ch != ',' && ch != EOF)
                {
                    sb.Append((char)reader.Read());
                    ch = reader.Peek();
                }

                return sb.ToString();
            }
        }
    }
}