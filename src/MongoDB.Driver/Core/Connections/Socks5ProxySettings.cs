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
/// Represents the settings for a SOCKS5 proxy connection.
/// </summary>
public sealed class Socks5ProxySettings
{
    private const int DefaultPort = 1080;

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

    internal Socks5ProxySettings(string host, int? port, Socks5AuthenticationSettings authentication)
    {
        Host = Ensure.IsNotNullOrEmpty(host, nameof(host));
        Port = port is null ? DefaultPort : Ensure.IsBetween(port.Value, 1, 65535, nameof(port));
        Authentication = authentication ?? Socks5AuthenticationSettings.None;
    }

    // Convenience method used internally.
    internal static Socks5ProxySettings Create(string host, int? port, string username, string password)
    {
        var authentication = !string.IsNullOrEmpty(username) ?
            Socks5AuthenticationSettings.UsernamePassword(username, password) : Socks5AuthenticationSettings.None;

        return new Socks5ProxySettings(host, port, authentication);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj is Socks5ProxySettings other)
        {
            return Host == other.Host &&
                   Port == other.Port &&
                   Equals(Authentication, other.Authentication);
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return new Hasher()
            .Hash(Host)
            .Hash(Port)
            .Hash(Authentication)
            .GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("{ Host : ");
        sb.Append(Host);
        sb.Append(", Port : ");
        sb.Append(Port);
        sb.Append(", Authentication : ");

        sb.Append(Authentication switch
        {
            Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings up =>
                $"UsernamePassword (Username: {up.Username}, Password: {up.Password})",
            _ => "None"
        });

        sb.Append(" }");
        return sb.ToString();
    }
}