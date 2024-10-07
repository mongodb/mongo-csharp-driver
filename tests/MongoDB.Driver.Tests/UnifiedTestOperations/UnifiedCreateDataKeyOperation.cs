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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCreateDataKeyOperation : IUnifiedEntityTestOperation
    {
        private readonly ClientEncryption _clientEncryption;
        private readonly DataKeyOptions _dataKeyOptions;
        private readonly string _kmsProvider;

        public UnifiedCreateDataKeyOperation(ClientEncryption clientEncryption, string kmsProvider, DataKeyOptions dataKeyOptions)
        {
            _clientEncryption = Ensure.IsNotNull(clientEncryption, nameof(clientEncryption));
            _dataKeyOptions = dataKeyOptions;
            _kmsProvider = kmsProvider;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _clientEncryption.CreateDataKey(_kmsProvider, _dataKeyOptions, cancellationToken);

                return OperationResult.FromResult(new BsonBinaryData(result, GuidRepresentation.Standard));
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
                var result = await _clientEncryption.CreateDataKeyAsync(_kmsProvider, _dataKeyOptions, cancellationToken);

                return OperationResult.FromResult(new BsonBinaryData(result, GuidRepresentation.Standard));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedCreateDataKeyOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateDataKeyOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateDataKeyOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var clientEncryption = _entityMap.ClientEncryptions[targetSessionId];

            string kmsProvider = null;
            var options = new DataKeyOptions();

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "kmsProvider":
                        kmsProvider = argument.Value.AsString;
                        break;
                    case "opts":
                        foreach (var option in argument.Value.AsBsonDocument)
                        {
                            options = option.Name switch
                            {
                                "keyMaterial" => options.With(keyMaterial: option.Value.AsBsonBinaryData),
                                "masterKey" => options = options.With(masterKey: option.Value.AsBsonDocument),
                                "keyAltNames" => options = options.With(alternateKeyNames: option.Value.AsBsonArray.Select(i => i.AsString).ToList().AsReadOnly()),
                                _ => throw new FormatException($"Invalid {nameof(DataKeyOptions)} option argument name: '{option.Name}'.")
                            };
                        }
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedCreateDataKeyOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCreateDataKeyOperation(clientEncryption, kmsProvider, options);
        }
    }
}
