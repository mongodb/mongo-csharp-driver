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
using System.Collections.Concurrent;
using MongoDB.Driver.Authentication.Gssapi;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Authentication.Plain;
using MongoDB.Driver.Authentication.ScramSha;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication
{
    internal sealed class SaslMechanismRegistry : ISaslMechanismRegistry
    {
        internal static SaslMechanismRegistry CreateDefaultInstance()
        {
            var registry = new SaslMechanismRegistry();
            registry.Register(PlainSaslMechanism.MechanismName, PlainSaslMechanism.Create);
            registry.Register(OidcSaslMechanism.MechanismName, OidcSaslMechanism.Create);
            registry.Register(ScramShaSaslMechanism.ScramSha1MechanismName, ScramShaSaslMechanism.CreateScramSha1Mechanism);
            registry.Register(ScramShaSaslMechanism.ScramSha256MechanismName, ScramShaSaslMechanism.CreateScramSha256Mechanism);
            registry.Register(GssapiSaslMechanism.MechanismName, GssapiSaslMechanism.Create);

            return registry;
        }

        private readonly ConcurrentDictionary<string, Func<SaslContext, ISaslMechanism>> _registry = new();

        public void Register(string mechanismName, Func<SaslContext, ISaslMechanism> factory)
        {
            Ensure.IsNotNullOrEmpty(mechanismName, nameof(mechanismName));
            Ensure.IsNotNull(factory, nameof(factory));

            if (!_registry.TryAdd(mechanismName, factory))
            {
                throw new ArgumentException($"Mechanism named '{mechanismName}' already registered.", nameof(mechanismName));
            }
        }

        public bool TryCreate(SaslContext context, out ISaslMechanism mechanism)
        {
            Ensure.IsNotNull(context, nameof(context));

            if (_registry.TryGetValue(context.Mechanism, out var factory))
            {
                mechanism = factory(context);
                return true;
            }

            mechanism = null;
            return false;
        }
    }
}
