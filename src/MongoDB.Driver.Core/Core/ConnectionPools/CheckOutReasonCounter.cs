/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal enum CheckOutReason
    {
        Cursor,
        Transaction
    }

    internal interface ICheckOutReasonTracker
    {
        CheckOutReason? CheckOutReason { get; }
        void SetCheckOutReasonIfNotAlreadySet(CheckOutReason reason);
    }

    internal sealed class CheckOutReasonCounter
    {
        public int _cursorCheckOutsCount = 0;
        public int _transactionCheckOutsCount = 0;

        public int GetCheckOutsCount(CheckOutReason reason) =>
            reason switch
            {
                CheckOutReason.Cursor => _cursorCheckOutsCount,
                CheckOutReason.Transaction => _transactionCheckOutsCount,
                _ => throw new InvalidOperationException($"Invalid checkout reason {reason}.")
            };

        public void Increment(CheckOutReason reason)
        {
            switch (reason)
            {
                case CheckOutReason.Cursor:
                    Interlocked.Increment(ref _cursorCheckOutsCount);
                    break;
                case CheckOutReason.Transaction:
                    Interlocked.Increment(ref _transactionCheckOutsCount);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid checkout reason {reason}.");
            }
        }

        public void Decrement(CheckOutReason? reason)
        {
            switch (reason)
            {
                case null:
                    break;
                case CheckOutReason.Cursor:
                    Interlocked.Decrement(ref _cursorCheckOutsCount);
                    break;
                case CheckOutReason.Transaction:
                    Interlocked.Decrement(ref _transactionCheckOutsCount);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid checkout reason {reason}.");
            }
        }
    }
}
