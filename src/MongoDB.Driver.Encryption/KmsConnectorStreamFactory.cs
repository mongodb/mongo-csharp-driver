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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption;

internal sealed class KmsConnectorStreamFactory : IStreamFactory
{
    private readonly IKmsConnector _kmsConnector;

    public KmsConnectorStreamFactory(IKmsConnector kmsConnector)
    {
        _kmsConnector = Ensure.IsNotNull(kmsConnector, nameof(kmsConnector));
    }

    public Stream CreateStream(EndPoint endPoint, CancellationToken cancellationToken)
    {
        var stream = _kmsConnector.Connect(CreateConnectionContext(endPoint), cancellationToken);
        return EnsureConnectResult(stream, nameof(IKmsConnector.Connect));
    }

    public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        var stream = await _kmsConnector.ConnectAsync(CreateConnectionContext(endPoint), cancellationToken).ConfigureAwait(false);
        return EnsureConnectResult(stream, nameof(IKmsConnector.ConnectAsync));
    }

    private static KmsConnectionContext CreateConnectionContext(EndPoint endPoint)
    {
        var dnsEndPoint = (DnsEndPoint)endPoint;
        return new KmsConnectionContext(dnsEndPoint.Host, dnsEndPoint.Port);
    }

    private static Stream EnsureConnectResult(Stream stream, string methodName)
    {
        return stream ?? throw new InvalidOperationException($"{nameof(IKmsConnector)}.{methodName} returned null.");
    }
}
