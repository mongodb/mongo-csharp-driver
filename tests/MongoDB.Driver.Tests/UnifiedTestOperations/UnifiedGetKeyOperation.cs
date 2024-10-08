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
    public class UnifiedGetKeyOperation : IUnifiedEntityTestOperation
    {
        private readonly ClientEncryption _clientEncryption;
        private readonly Guid _id;

        public UnifiedGetKeyOperation(ClientEncryption clientEncryption, Guid id)
        {
            _clientEncryption = Ensure.IsNotNull(clientEncryption, nameof(clientEncryption));
            _id = id;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _clientEncryption.GetKey(_id, cancellationToken);

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
                var result = await _clientEncryption.GetKeyAsync(_id, cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedGetKeyOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedGetKeyOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedGetKeyOperation Build(string targetSessionId, BsonDocument arguments)
        {
            var clientEncryption = _entityMap.ClientEncryptions[targetSessionId];

            Guid? id = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "id":
                        id = argument.Value.AsGuid;
                        break;

                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedGetKeyOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedGetKeyOperation(clientEncryption, id.Value);
        }
    }
}
