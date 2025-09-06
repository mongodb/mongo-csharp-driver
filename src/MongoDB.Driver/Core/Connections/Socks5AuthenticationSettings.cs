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

using System.Text;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections;

/// <summary>
/// Represents the settings for SOCKS5 authentication.
/// </summary>
public abstract class Socks5AuthenticationSettings
{
    /// <summary>
    /// Creates authentication settings that does not require any authentication.
    /// </summary>
    public static Socks5AuthenticationSettings None { get; } = new NoAuthenticationSettings();

    /// <summary>
    /// Creates authentication settings for username and password.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <returns></returns>
    public static Socks5AuthenticationSettings UsernamePassword(string username, string password)
        => new UsernamePasswordAuthenticationSettings(username, password);

    /// <summary>
    /// Represents settings for no authentication in SOCKS5.
    /// </summary>
    internal sealed class NoAuthenticationSettings : Socks5AuthenticationSettings
    {
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is NoAuthenticationSettings;

        /// <inheritdoc />
        public override int GetHashCode() => 0;
    }

    /// <summary>
    /// Represents settings for username and password authentication in SOCKS5.
    /// </summary>
    internal sealed class UsernamePasswordAuthenticationSettings : Socks5AuthenticationSettings
    {
        /// <summary>
        /// Gets the username for authentication.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Gets the password for authentication.
        /// </summary>
        public string Password { get; }

        internal UsernamePasswordAuthenticationSettings(string username, string password)
        {
            Username = Ensure.IsNotNullOrEmpty(username, nameof(username));
            Ensure.That(Encoding.UTF8.GetByteCount(username) <= byte.MaxValue, $"{nameof(username)} must be at most 255 bytes long when encoded as UTF-8", nameof(username));
            Password = Ensure.IsNotNullOrEmpty(password, nameof(password));
            Ensure.That(Encoding.UTF8.GetByteCount(password) <= byte.MaxValue, $"{nameof(password)} must be at most 255 bytes long when encoded as UTF-8", nameof(password));
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is UsernamePasswordAuthenticationSettings other)
            {
                return Username == other.Username && Password == other.Password;
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode() =>
            new Hasher()
                .Hash(Username)
                .Hash(Password)
                .GetHashCode();
    }
}