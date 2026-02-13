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
using System.Diagnostics;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// Manages Activity.Current scope when executing operations within a transaction.
    /// Temporarily sets Activity.Current to the transaction activity so operation activities nest correctly,
    /// then restores to the original value to prevent AsyncLocal flow issues.
    /// </summary>
    internal sealed class TransactionActivityScope : IDisposable
    {
        private readonly Activity _originalActivity;

        private TransactionActivityScope(Activity transactionActivity)
        {
            _originalActivity = Activity.Current;
            Activity.Current = transactionActivity;
        }

        public static TransactionActivityScope CreateIfNeeded(CoreTransaction transaction)
        {
            var transactionActivity = transaction?.TransactionActivity;
            return transactionActivity != null
                ? new TransactionActivityScope(transactionActivity)
                : null;
        }

        public void Dispose()
        {
            Activity.Current = _originalActivity;
        }
    }
}
