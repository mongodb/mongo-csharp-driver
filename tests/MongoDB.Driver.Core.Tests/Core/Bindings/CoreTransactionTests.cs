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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class CoreTransactionTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(1, 2)] long transactionNumber,
            [Values(false, true)] bool nullTransactionOptions)
       {
            var transactionOptions = nullTransactionOptions ? null : new TransactionOptions();

            var result = new CoreTransaction(transactionNumber, transactionOptions);

            result.StatementId.Should().Be(0);
            result.TransactionNumber.Should().Be(transactionNumber);
            result.TransactionOptions.Should().BeSameAs(transactionOptions);
        }

        [Theory]
        [ParameterAttributeData]
        public void StatementId_should_return_expected_result(
            [Values(1, 2)] int value)
        {
            var subject = CreateSubject(statementId: value);

            var result = subject.StatementId;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void TransactionNumber_should_return_expected_result(
            [Values(1, 2)] long value)
        {
            var subject = CreateSubject(transactionNumber: value);

            var result = subject.TransactionNumber;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void TransactionOptions_should_return_expected_result(
            [Values(false, true)] bool nullTransactionOptions)
        {
            var value = nullTransactionOptions ? null : new TransactionOptions();
            var subject = CreateSubject(transactionOptions: value);

            var result = subject.TransactionOptions;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void AdvanceStatementId_should_have_expected_result(
            [Values(1, 2)] int statementId,
            [Values(1, 2)] int numberOfStatements)
        {
            var subject = CreateSubject(statementId: statementId);
            var expectedStatementId = statementId + numberOfStatements;

            subject.AdvanceStatementId(numberOfStatements);

            subject.StatementId.Should().Be(expectedStatementId);
        }

        // private methods
        private CoreTransaction CreateSubject(
            int statementId = 0,
            long transactionNumber = 0,
            TransactionOptions transactionOptions = null)
        {
            var subject = new CoreTransaction(transactionNumber, transactionOptions);
            subject.AdvanceStatementId(statementId);
            return subject;
        }
    }
}
