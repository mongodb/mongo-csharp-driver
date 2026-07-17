/* Copyright 2019-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Encryption;

/// <summary>
/// Opens the transport connection used to reach a KMS host. When supplied via
/// ClientEncryptionOptions or <see cref="AutoEncryptionOptions"/>, the driver
/// invokes this instead of opening a direct TCP connection to the KMS host, then wraps the
/// returned stream in TLS using the KMS provider's configured TLS options.
/// The primary use case is routing KMS traffic through an HTTP proxy via HTTPS CONNECT.
/// </summary>
public interface IKmsConnector
{
    /// <summary>
    /// Opens a connection to the specified KMS host.
    /// </summary>
    /// <param name="host">The KMS hostname (for example, <c>kms.us-east-1.amazonaws.com</c>).</param>
    /// <param name="port">The KMS port (typically 443).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream connected to the KMS host. The driver wraps this stream in TLS.</returns>
    Stream Connect(string host, int port, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a connection to the specified KMS host.
    /// </summary>
    /// <param name="host">The KMS hostname (for example, <c>kms.us-east-1.amazonaws.com</c>).</param>
    /// <param name="port">The KMS port (typically 443).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream connected to the KMS host. The driver wraps this stream in TLS.</returns>
    Task<Stream> ConnectAsync(string host, int port, CancellationToken cancellationToken);
}
