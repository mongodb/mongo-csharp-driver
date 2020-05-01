/* Copyright 2018-present MongoDB Inc.
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

namespace MongoDB.Driver.Examples.TransactionExamplesForDocs
{
    public class RetryExample1
    {
        // Start Transactions Retry Example 1
        public void RunTransactionWithRetry(Action<IMongoClient, IClientSessionHandle> txnFunc, IMongoClient client, IClientSessionHandle session)
        {
            while (true)
            {
                try
                {
                    txnFunc(client, session); // performs transaction
                    break;
                }
                catch (MongoException exception)
                {
                    Console.WriteLine($"Transaction aborted. Caught exception during transaction: ${exception.Message}.");

                    // if transient error, retry the whole transaction
                    if (exception.HasErrorLabel("TransientTransactionError"))
                    {
                        Console.WriteLine("TransientTransactionError, retrying transaction.");
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
        // End Transactions Retry Example 1
    }
}
