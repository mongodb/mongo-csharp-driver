/* Copyright 2010-2014 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    internal sealed class OperationExecutor : IOperationExecutor
    {
        public Task<TResult> ExecuteReadOperationAsync<TResult>(Core.Bindings.IReadBinding binding, Core.Operations.IReadOperation<TResult> operation, TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public Task<TResult> ExecuteWriteOperationAsync<TResult>(Core.Bindings.IWriteBinding binding, Core.Operations.IWriteOperation<TResult> operation, TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
