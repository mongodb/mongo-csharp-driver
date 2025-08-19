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

using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Configuration;

/// <summary>
/// Represents settings for a SOCKS5 proxy stream.
/// </summary>
public class Socks5ProxyStreamSettings
{
    private Socks5ProxySettings _socks5ProxySettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="Socks5ProxyStreamSettings"/> class.
    /// </summary>
    /// <param name="socks5ProxySettings"> The settings for the SOCKS5 proxy.</param>
    public Socks5ProxyStreamSettings(Optional<Socks5ProxySettings> socks5ProxySettings = default)
    {
        _socks5ProxySettings = socks5ProxySettings.WithDefault(null);
    }

    /// <summary>
    /// Gets the settings for the SOCKS5 proxy.
    /// </summary>
    public Socks5ProxySettings Socks5ProxySettings => _socks5ProxySettings;

    /// <summary>
    /// Creates a new instance of <see cref="Socks5ProxyStreamSettings"/> with the specified SOCKS5 proxy settings.
    /// </summary>
    /// <param name="socks5ProxySettings"></param>
    /// <returns></returns>
    public Socks5ProxyStreamSettings With(Socks5ProxySettings socks5ProxySettings)
    {
        return new Socks5ProxyStreamSettings(socks5ProxySettings);
    }
}