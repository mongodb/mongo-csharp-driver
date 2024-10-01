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

namespace MongoDB.Driver.Authentication.AWS
{
    /// <summary>
    /// Auth Registry Extensions.
    /// </summary>
    public static class ExtensionManagerExtensions
    {
        /// <summary>
        /// Registers both AWS SASL mechanism and AWS Kms Provider.
        /// </summary>
        /// <param name="extensionManager"></param>
        public static IExtensionManager AddAWSAuthentication(this IExtensionManager extensionManager)
        {
            extensionManager.SaslMechanisms.Register(AWSSaslMechanism.MechanismName, AWSSaslMechanism.Create);
            extensionManager.KmsProviders.Register(AWSKmsProvider.ProviderName, () => AWSKmsProvider.Instance);
            return extensionManager;
        }
    }
}
