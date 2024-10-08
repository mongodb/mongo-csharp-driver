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
    public sealed class UnifiedEncryptOperation : IUnifiedEntityTestOperation
    {
        private readonly ClientEncryption _clientEncryption;
        private readonly BsonValue _value;
        private readonly EncryptOptions _options;

        public UnifiedEncryptOperation(ClientEncryption clientEncryption, BsonValue value, EncryptOptions options)
        {
            _clientEncryption = Ensure.IsNotNull(clientEncryption, nameof(clientEncryption));
            _value = value;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _clientEncryption.Encrypt(_value, _options, cancellationToken);

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
                var result = await _clientEncryption.EncryptAsync(_value, _options, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public sealed class UnifiedEncryptOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedEncryptOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedEncryptOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var clientEncryption = _entityMap.ClientEncryptions[targetSessionId];

            BsonValue value = null;
            string keyAltName = null;
            string algorithm = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "value":
                        value = argument.Value;
                        break;
                    case "opts":
                        foreach (var option in argument.Value.AsBsonDocument)
                        {
                            switch (option.Name)
                            {
                                case "keyAltName": keyAltName = option.Value.AsString; break;
                                case "algorithm": algorithm = option.Value.AsString; break;
                                default: throw new FormatException($"Invalid {nameof(EncryptOptions)} option argument name: '{option.Name}'.");
                            }
                        }
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedEncryptOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedEncryptOperation(clientEncryption, value, new EncryptOptions(algorithm, keyAltName));
        }
    }
}
