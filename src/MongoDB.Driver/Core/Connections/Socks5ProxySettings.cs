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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents the settings for a SOCKS5 proxy connection.
    /// </summary>
    public class Socks5ProxySettings
    {
        /// <summary>
        /// Gets the host of the SOCKS5 proxy.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the port of the SOCKS5 proxy.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the authentication settings of the SOCKS5 proxy.
        /// </summary>
        public Socks5AuthenticationSettings Authentication { get; }

        private Socks5ProxySettings(string host, int port, Socks5AuthenticationSettings authentication)
        {
            Host = Ensure.IsNotNullOrEmpty(host, nameof(host));
            Port = Ensure.IsBetween(port, 0, 65535, nameof(port));
            Authentication = Ensure.IsNotNull(authentication, nameof(authentication));
        }

        internal static Socks5ProxySettings Create(string host, int? port, string username, string password)
        {
            Socks5AuthenticationSettings authentication;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                authentication = Socks5AuthenticationSettings.UsernamePassword(username, password);
            }
            else
            {
                authentication = Socks5AuthenticationSettings.None;
            }

            return new Socks5ProxySettings(host, port ?? 1080, authentication);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Socks5ProxySettings"/>.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port</param>
        /// <param name="authentication">The authentication settings.</param>
        /// <returns></returns>
        public static Socks5ProxySettings Create(string host, int port = 1080, Socks5AuthenticationSettings authentication = null)
        {
            return new Socks5ProxySettings(host, port, authentication ?? Socks5AuthenticationSettings.None);
        }
    }

    internal enum Socks5AuthenticationType
    {
        None,
        UsernamePassword
    }

    /// <summary>
    /// Represents the settings for SOCKS5 authentication.
    /// </summary>
    public abstract class Socks5AuthenticationSettings
    {
        internal abstract Socks5AuthenticationType Type { get; }

        /// <summary>
        /// Creates authentication settings that do not require any authentication.
        /// </summary>
        public static Socks5AuthenticationSettings None => new NoAuthenticationSettings();

        /// <summary>
        /// Creates authentication settings for username and password.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Socks5AuthenticationSettings UsernamePassword(string username, string password)
            => new UsernamePasswordAuthenticationSettings(username, password);

        private sealed class NoAuthenticationSettings : Socks5AuthenticationSettings
        {
            internal override Socks5AuthenticationType Type => Socks5AuthenticationType.None;
        }

        private sealed class UsernamePasswordAuthenticationSettings : Socks5AuthenticationSettings
        {
            internal override Socks5AuthenticationType Type => Socks5AuthenticationType.UsernamePassword;
            public string Username { get; }
            public string Password { get; }

            internal UsernamePasswordAuthenticationSettings(string username, string password)
            {
                Username = Ensure.IsNotNullOrEmpty(username, nameof(username));
                Password = Ensure.IsNotNullOrEmpty(password, nameof(password));
            }
        }
    }
}