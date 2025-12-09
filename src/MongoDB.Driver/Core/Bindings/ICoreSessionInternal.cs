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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Bindings;

// TODO: Merge this interface into ICoreSession on major release
internal interface ICoreSessionInternal
{
    void AbortTransaction(AbortTransactionOptions options, CancellationToken cancellationToken = default);
    Task AbortTransactionAsync(AbortTransactionOptions options, CancellationToken cancellationToken = default);
    void CommitTransaction(CommitTransactionOptions options, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CommitTransactionOptions options, CancellationToken cancellationToken = default);
    void StartTransaction(TransactionOptions transactionOptions, bool isTracingEnabled);
}
