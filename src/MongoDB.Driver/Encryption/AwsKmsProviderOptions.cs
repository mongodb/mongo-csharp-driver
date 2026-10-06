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
using System.Collections.Generic;

namespace MongoDB.Driver.Encryption;

/// <summary>
/// Options for the AWS KMS provider.
/// </summary>
/// <remarks>
/// Set <see cref="AccessKeyId"/> and <see cref="SecretAccessKey"/> to authenticate with explicit credentials,
/// adding <see cref="SessionToken"/> for temporary credentials. Leave all three unset to obtain credentials
/// on demand from the environment (environment variables, ECS task role or EC2 instance profile), which
/// requires the AWS extension to be registered and is only supported for the unnamed <c>"aws"</c> provider.
/// The values are copied when the provider is registered, so later changes to this instance have no effect.
/// </remarks>
public sealed class AwsKmsProviderOptions
{
    /// <summary>
    /// Gets or sets the access key id. Must be set together with <see cref="SecretAccessKey"/>.
    /// </summary>
    public string AccessKeyId { get; set; }

    /// <summary>
    /// Gets or sets the secret access key. Must be set together with <see cref="AccessKeyId"/>.
    /// </summary>
    public string SecretAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the session token, required only for temporary credentials. Requires
    /// <see cref="AccessKeyId"/> and <see cref="SecretAccessKey"/> to be set.
    /// </summary>
    public string SessionToken { get; set; }

    /// <summary>
    /// Gets or sets the TLS settings used to reach the KMS host. The default value is <c>null</c>, which
    /// uses the default TLS settings.
    /// </summary>
    public SslSettings TlsSettings { get; set; }

    internal Dictionary<string, object> ToKmsProviderOptions(bool isNamed)
    {
        var hasAccessKeyId = !string.IsNullOrEmpty(AccessKeyId);
        var hasSecretAccessKey = !string.IsNullOrEmpty(SecretAccessKey);
        var hasSessionToken = !string.IsNullOrEmpty(SessionToken);

        if (hasAccessKeyId != hasSecretAccessKey)
        {
            throw new ArgumentException($"{nameof(AccessKeyId)} and {nameof(SecretAccessKey)} must be set together.", "options");
        }

        var options = new Dictionary<string, object>();
        if (!hasAccessKeyId)
        {
            if (hasSessionToken)
            {
                throw new ArgumentException($"{nameof(SessionToken)} requires {nameof(AccessKeyId)} and {nameof(SecretAccessKey)} to be set.", "options");
            }
            if (isNamed)
            {
                throw new ArgumentException("On-demand credentials are not supported for named KMS providers.", "options");
            }

            return options;
        }

        options.Add("accessKeyId", AccessKeyId);
        options.Add("secretAccessKey", SecretAccessKey);
        if (hasSessionToken)
        {
            options.Add("sessionToken", SessionToken);
        }

        return options;
    }
}
