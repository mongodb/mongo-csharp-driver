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

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections;

internal sealed class Socks5ProxyStreamFactory : IStreamFactory
{
    private readonly Socks5ProxyStreamSettings _settings;
    private readonly IStreamFactory _wrapped;

    public Socks5ProxyStreamFactory(Socks5ProxyStreamSettings settings, IStreamFactory wrapped)
    {
        _settings = Ensure.IsNotNull(settings, nameof(settings));
        _wrapped = Ensure.IsNotNull(wrapped, nameof(wrapped));
    }

    public Stream CreateStream(EndPoint endPoint, CancellationToken cancellationToken)
    {
        Stream stream = null;

        try
        {
            var proxyEndpoint = new DnsEndPoint(_settings.Socks5ProxySettings.Host, _settings.Socks5ProxySettings.Port);
            stream = _wrapped.CreateStream(proxyEndpoint, cancellationToken);
            Socks5Helper.PerformSocks5Handshake(stream, endPoint, _settings.Socks5ProxySettings.Authentication, cancellationToken);
            return stream;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        Stream stream = null;

        try
        {
            var proxyEndpoint = new DnsEndPoint(_settings.Socks5ProxySettings.Host, _settings.Socks5ProxySettings.Port);
            stream = await _wrapped.CreateStreamAsync(proxyEndpoint, cancellationToken).ConfigureAwait(false);
            await Socks5Helper.PerformSocks5HandshakeAsync(stream, endPoint, _settings.Socks5ProxySettings.Authentication, cancellationToken).ConfigureAwait(false);
            return stream;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }
}