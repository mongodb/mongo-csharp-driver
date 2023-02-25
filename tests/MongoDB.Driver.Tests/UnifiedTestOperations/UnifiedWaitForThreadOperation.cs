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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedWaitForThreadOperation : IUnifiedSpecialTestOperation
    {
        private readonly Task _thread;

        public UnifiedWaitForThreadOperation(Task thread)
        {
            _thread = Ensure.IsNotNull(thread, nameof(thread));
        }

        public void Execute()
        {
            _thread.GetAwaiter().GetResult();
        }
    }

    public sealed class UnifiedWaitForThreadOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedWaitForThreadOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedWaitForThreadOperation Build(BsonDocument arguments)
        {
            Task thread = null;
            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "thread":
                        thread = _entityMap.Threads[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedWaitForThreadOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedWaitForThreadOperation(thread);
        }
    }
}
