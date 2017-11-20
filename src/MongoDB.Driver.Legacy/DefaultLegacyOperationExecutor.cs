/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    // used when the application uses the deprecated MongoServer, MongoDatabase and MongoCollection constructors
    internal class DefaultLegacyOperationExecutor : IOperationExecutor
    {
        public TResult ExecuteReadOperation<TResult>(IReadBinding binding, IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            return operation.Execute(binding, cancellationToken);
        }

        public Task<TResult> ExecuteReadOperationAsync<TResult>(IReadBinding binding, IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            return operation.ExecuteAsync(binding, cancellationToken);
        }

        public TResult ExecuteWriteOperation<TResult>(IWriteBinding binding, IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            return operation.Execute(binding, cancellationToken);
        }

        public Task<TResult> ExecuteWriteOperationAsync<TResult>(IWriteBinding binding, IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            return operation.ExecuteAsync(binding, cancellationToken);
        }

        public IClientSessionHandle StartImplicitSession(CancellationToken cancellationToken)
        {
            return new ClientSessionHandle(new NoClientSession());
        }

        public Task<IClientSessionHandle> StartImplicitSessionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IClientSessionHandle>(new ClientSessionHandle(new NoClientSession()));
        }
    }
}
