/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedWaitOperation : IUnifiedEntityTestOperation
    {
        private readonly TimeSpan _ms;

        public UnifiedWaitOperation(TimeSpan? ms)
        {
            _ms = Ensure.HasValue(ms, nameof(ms)).Value;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            Thread.Sleep(_ms);

            return OperationResult.Empty();
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_ms);

            return OperationResult.Empty();
        }
    }

    public class UnifiedWaitOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedWaitOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedWaitOperation Build(BsonDocument arguments)
        {
            TimeSpan? ms = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "ms":
                        ms = TimeSpan.FromMilliseconds(argument.Value.AsInt32);
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedWaitOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedWaitOperation(ms);
        }
    }
}
