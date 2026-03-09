/* Copyright 2013-present MongoDB Inc.
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

using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Core.Operations
{
    internal interface IReadOperation<TResult>
    {
        string OperationName { get; }
        TResult Execute(OperationContext operationContext, IReadBinding binding);
        Task<TResult> ExecuteAsync(OperationContext operationContext, IReadBinding binding);
    }

    internal interface IWriteOperation<TResult>
    {
        string OperationName { get; }
        TResult Execute(OperationContext operationContext, IWriteBinding binding);
        Task<TResult> ExecuteAsync(OperationContext operationContext, IWriteBinding binding);
    }
}
