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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption;

/// <summary>
/// Describes the KMS host that an <see cref="IKmsConnector"/> is being asked to connect to.
/// </summary>
public sealed class KmsConnectionContext
{

    /// <summary>
    /// Initializes a new instance of the <see cref="KmsConnectionContext"/> class.
    /// </summary>
    /// <param name="host">The KMS hostname.</param>
    /// <param name="port">The KMS port.</param>
    public KmsConnectionContext(string host, int port)
    {
        Host = Ensure.IsNotNullOrEmpty(host, nameof(host));
        Port = port;
    }

    /// <summary>
    /// Gets the KMS hostname (for example, <c>kms.us-east-1.amazonaws.com</c>).
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Gets the KMS port.
    /// </summary>
    public int Port { get; }
}
