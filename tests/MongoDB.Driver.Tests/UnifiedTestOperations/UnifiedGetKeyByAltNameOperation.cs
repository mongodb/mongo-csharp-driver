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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedGetKeyByAltNameOperation : IUnifiedEntityTestOperation
    {
        private readonly ClientEncryption _clientEncryption;
        private readonly string _keyAlterName;

        public UnifiedGetKeyByAltNameOperation(ClientEncryption clientEncryption, string keyAlterName)
        {
            _clientEncryption = Ensure.IsNotNull(clientEncryption, nameof(clientEncryption));
            _keyAlterName = keyAlterName;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _clientEncryption.GetKeyByAlternateKeyName(_keyAlterName, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _clientEncryption.GetKeyByAlternateKeyNameAsync(_keyAlterName, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedGetKeyByAltNameOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedGetKeyByAltNameOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedGetKeyByAltNameOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var clientEncryption = _entityMap.ClientEncryptions[targetSessionId];

            string keyAlterName = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "keyAltName":
                        keyAlterName = argument.Value.AsString;
                        break;

                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedGetKeyByAltNameOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedGetKeyByAltNameOperation(clientEncryption, keyAlterName);
        }
    }
}
