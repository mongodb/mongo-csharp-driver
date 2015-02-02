﻿/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Sync
{
    internal static class IOperationExtensions
    {
        // static methods
        public static TResult Execute<TResult>(this IReadOperation<TResult> operation, IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operation.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();
        }

        public static TResult Execute<TResult>(this IWriteOperation<TResult> operation, IWriteBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operation.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();
        }
    }
}
