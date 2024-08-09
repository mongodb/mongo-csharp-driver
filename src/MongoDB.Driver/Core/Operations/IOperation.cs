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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Core.Operations
{
    internal interface IReadOperation<TResult>
    {
        TResult Execute(IReadBinding binding, CancellationToken cancellationToken);
        Task<TResult> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken);
    }

    internal interface IWriteOperation<TResult>
    {
        TResult Execute(IWriteBinding binding, CancellationToken cancellationToken);
        Task<TResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken);
    }
}
