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

using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Authentication.AWS
{
    /// <summary>
    /// Auth Registry Extensions.
    /// </summary>
    public static class AuthRegistryExtensions
    {
        /// <summary>
        /// Registers Aws SASL mechanism.
        /// </summary>
        /// <param name="registry">SASL Mechanism Registry.</param>
        public static SaslMechanismRegistry RegisterAWSMechanism(this SaslMechanismRegistry registry)
        {
            registry.Register(AWSSaslMechanism.MechanismName, AWSSaslMechanism.Create);
            return registry;
        }

        /// <summary>
        /// Registers Aws Kms Provider.
        /// </summary>
        /// <param name="registry">Kms Provider Registry.</param>
        public static KmsProviderRegistry RegisterAWSKmsProvider(this KmsProviderRegistry registry)
        {
            registry.Register(AWSKmsProvider.ProviderName, () => AWSKmsProvider.Instance);
            return registry;
        }
    }
}
