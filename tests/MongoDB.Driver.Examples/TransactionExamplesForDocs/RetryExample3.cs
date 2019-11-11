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
using MongoDB.Bson;

namespace MongoDB.Driver.Examples.TransactionExamplesForDocs
{
    public class RetryExample3
    {
        // Start Transactions Retry Example 3
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

        public void CommitWithRetry(IClientSessionHandle session)
        {
            while (true)
            {
                try
                {
                    session.CommitTransaction();
                    Console.WriteLine("Transaction committed.");
                    break;
                }
                catch (MongoException exception)
                {
                    // can retry commit
                    if (exception.HasErrorLabel("UnknownTransactionCommitResult"))
                    {
                        Console.WriteLine("UnknownTransactionCommitResult, retrying commit operation");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Error during commit: {exception.Message}.");
                        throw;
                    }
                }
            }
        }

        // updates two collections in a transaction
        public void UpdateEmployeeInfo(IMongoClient client, IClientSessionHandle session)
        {
            var employeesCollection = client.GetDatabase("hr").GetCollection<BsonDocument>("employees");
            var eventsCollection = client.GetDatabase("reporting").GetCollection<BsonDocument>("events");

            session.StartTransaction(new TransactionOptions(
                readConcern: ReadConcern.Snapshot,
                writeConcern: WriteConcern.WMajority,
                readPreference: ReadPreference.Primary));

            try
            {
                employeesCollection.UpdateOne(
                    session,
                    Builders<BsonDocument>.Filter.Eq("employee", 3),
                    Builders<BsonDocument>.Update.Set("status", "Inactive"));
                eventsCollection.InsertOne(
                    session,
                    new BsonDocument
                    {
                        { "employee", 3 },
                        { "status", new BsonDocument { { "new", "Inactive" }, { "old", "Active" } } }
                    });
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Caught exception during transaction, aborting: {exception.Message}.");
                session.AbortTransaction();
                throw;
            }

            CommitWithRetry(session);
        }

        public void UpdateEmployeeInfoWithTransactionRetry(IMongoClient client)
        {
            // start a session
            using (var session = client.StartSession())
            {
                try
                {
                    RunTransactionWithRetry(UpdateEmployeeInfo, client, session);
                }
                catch (Exception exception)
                {
                    // do something with error
                    Console.WriteLine($"Non transient exception caught during transaction: ${exception.Message}.");
                }
            }
        }
        // End Transactions Retry Example 3
    }
}
