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

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// The state of a transaction.
    /// </summary>
    public class CoreTransaction
    {
        // private fields
        private int _statementId;
        private readonly long _transactionNumber;
        private readonly TransactionOptions _transactionOptions;

        // public constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreTransaction" /> class.
        /// </summary>
        /// <param name="transactionNumber">The transaction number.</param>
        /// <param name="transactionOptions">The transaction options.</param>
        public CoreTransaction(long transactionNumber, TransactionOptions transactionOptions)
        {
            _transactionNumber = transactionNumber;
            _transactionOptions = transactionOptions;
            _statementId = 0;
        }

        // public properties
        /// <summary>
        /// Gets the statement identifier.
        /// </summary>
        /// <value>
        /// The statement identifier.
        /// </value>
        public int StatementId => _statementId;

        /// <summary>
        /// Gets the transaction number.
        /// </summary>
        /// <value>
        /// The transaction number.
        /// </value>
        public long TransactionNumber => _transactionNumber;

        /// <summary>
        /// Gets the transaction options.
        /// </summary>
        /// <value>
        /// The transaction options.
        /// </value>
        public TransactionOptions TransactionOptions => _transactionOptions;

        // public methods
        /// <summary>
        /// Advances the statement identifier.
        /// </summary>
        /// <param name="numberOfStatements">The number of statements to advance by.</param>
        public void AdvanceStatementId(int numberOfStatements)
        {
            _statementId += numberOfStatements;
        }
    }
}
