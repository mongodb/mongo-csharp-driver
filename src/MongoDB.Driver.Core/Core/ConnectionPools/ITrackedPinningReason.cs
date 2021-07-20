/*Copyright 2021 - present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
*you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
*Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Threading;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal enum CheckedOutReason
    {
        NotSet,
        Cursor,
        Transaction
    }

    internal interface ITrackedPinningReason
    {
        CheckedOutReason? CheckedOutReason { get; }
        void SetPinningCheckoutReasonIfNotAlreadySet(CheckedOutReason reason);
    }

    internal sealed class CheckedOutTracker
    {
        public int _unspecifiedCheckoutsNumber = 0;
        public int _cursorCheckoutsNumber = 0;
        public int _transactionCheckoutsNumber = 0;

        public int GetCheckedOutNumber(CheckedOutReason reason) =>
            reason switch
            {
                CheckedOutReason.NotSet => _unspecifiedCheckoutsNumber,
                CheckedOutReason.Cursor => _cursorCheckoutsNumber,
                CheckedOutReason.Transaction => _transactionCheckoutsNumber,
                // should not be reached
                _ => throw new InvalidOperationException("Unsupported checked out reason."),
            };

        public void CheckOut(CheckedOutReason reason)
        {
            switch (reason)
            {
                case CheckedOutReason.NotSet:
                    Interlocked.Increment(ref _unspecifiedCheckoutsNumber);
                    break;
                case CheckedOutReason.Cursor:
                    Interlocked.Increment(ref _cursorCheckoutsNumber);
                    break;
                case CheckedOutReason.Transaction:
                    Interlocked.Increment(ref _transactionCheckoutsNumber);
                    break;
                default:
                    // should not be reached
                    throw new InvalidOperationException($"Unsupported checked out reason {reason}.");
            }
        }

        public void CheckInIfNotNull(CheckedOutReason? reason)
        {
            if (reason.HasValue)
            {
                switch (reason)
                {
                    case CheckedOutReason.NotSet:
                        Interlocked.Decrement(ref _unspecifiedCheckoutsNumber);
                        break;
                    case CheckedOutReason.Cursor:
                        Interlocked.Decrement(ref _cursorCheckoutsNumber);
                        break;
                    case CheckedOutReason.Transaction:
                        Interlocked.Decrement(ref _transactionCheckoutsNumber);
                        break;
                    default:
                        // should not be reached
                        throw new InvalidOperationException($"Unsupported checked out reason {reason}.");
                }
            }
        }
    }
}
