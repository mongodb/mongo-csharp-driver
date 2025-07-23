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

namespace MongoDB.Driver.Core.Connections;

/// <summary>
/// Builder for creating <see cref="Socks5ProxySettings"/>.
/// </summary>
public class Socks5ProxySettingsBuilder
{
    private readonly string _host;
    private int? _port;
    private Socks5AuthenticationSettings _authentication;

    /// <summary>
    /// Initializes a new instance of the <see cref="Socks5ProxySettingsBuilder"/> class with the specified host.
    /// </summary>
    /// <param name="host">The host of the SOCKS5 proxy.</param>
    public Socks5ProxySettingsBuilder(string host)
    {
        _host = host;
    }

    /// <summary>
    /// Sets the port for the SOCKS5 proxy.
    /// </summary>
    /// <param name="port">The port of the SOCKS5 proxy.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public Socks5ProxySettingsBuilder Port(int port)
    {
        _port = port;
        return this;
    }

    /// <summary>
    /// Sets the authentication for the SOCKS5 proxy using username and password.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public Socks5ProxySettingsBuilder UsernameAndPasswordAuth(string username, string password)
    {
        _authentication = Socks5AuthenticationSettings.UsernamePassword(username, password);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="Socks5ProxySettings"/> instance with the specified settings.
    /// </summary>
    public Socks5ProxySettings Build()
    {
        return new Socks5ProxySettings(_host, _port, _authentication);
    }
}