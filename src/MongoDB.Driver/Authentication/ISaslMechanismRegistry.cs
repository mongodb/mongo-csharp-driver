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

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// SASL Mechanism Registry.
    /// </summary>
    public interface ISaslMechanismRegistry
    {
        /// <summary>
        /// Registers new SASL mechanism factory.
        /// </summary>
        /// <param name="mechanismName">Mechanism name.</param>
        /// <param name="factory">Factory method.</param>
        void Register(string mechanismName, Func<SaslContext, ISaslMechanism> factory);

        /// <summary>
        /// Creates SASL mechanism if possible.
        /// </summary>
        /// <param name="context">Sasl context.</param>
        /// <param name="mechanism">When this method succeeds contains the created mechanism, otherwise <value>null</value>.</param>
        /// <returns><value>true</value> if the requested provider was created, otherwise <value>false</value>.</returns>
        bool TryCreate(SaslContext context, out ISaslMechanism mechanism);
    }
}

