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

using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class CompositeWriteOperation<TResult> : IWriteOperation<TResult>
    {
        private readonly (IWriteOperation<TResult> Operation, bool IsMainOperation)[] _operations;

        public CompositeWriteOperation(params (IWriteOperation<TResult>, bool IsMainOperation)[] operations)
        {
            _operations = Ensure.IsNotNull(operations, nameof(operations));
            Ensure.IsGreaterThanZero(operations.Length, nameof(operations.Length));
            Ensure.That(operations.Count(o => o.IsMainOperation) == 1, message: $"{nameof(CompositeWriteOperation<TResult>)} must have a single main operation.");
        }

        public string OperationName => null;

        public TResult Execute(OperationContext operationContext, IWriteBinding binding)
        {
            TResult result = default;
            foreach (var operationInfo in _operations)
            {
                var itemResult = operationInfo.Operation.Execute(operationContext, binding);
                if (operationInfo.IsMainOperation)
                {
                    result = itemResult;
                }
            }

            return result;
        }

        public async Task<TResult> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            TResult result = default;
            foreach (var operationInfo in _operations)
            {
                var itemResult = await operationInfo.Operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
                if (operationInfo.IsMainOperation)
                {
                    result = itemResult;
                }
            }

            return result;
        }
    }
}
