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

            result.IsEmpty.Should().BeTrue();
            result.State.Should().Be(CoreTransactionState.Starting);
            result.TransactionNumber.Should().Be(transactionNumber);
            result.TransactionOptions.Should().BeSameAs(transactionOptions);
        }

        [Theory]
        [InlineData(-1, -1, true)]
        [InlineData(CoreTransactionState.Aborted, -1, true)]
        [InlineData(CoreTransactionState.Committed, -1, true)]
        [InlineData(CoreTransactionState.InProgress, -1, false)]
        [InlineData(CoreTransactionState.InProgress, CoreTransactionState.Aborted, false)]
        [InlineData(CoreTransactionState.InProgress, CoreTransactionState.Committed, false)]
        [InlineData(CoreTransactionState.InProgress, CoreTransactionState.InProgress, false)]
        public void IsEmpty_should_return_expected_result(CoreTransactionState transactionState1, CoreTransactionState transactionState2, bool expectedResult)
        {
            var subject = CreateSubject();
            if (transactionState1 != (CoreTransactionState)(-1))
            {
                subject.SetState(transactionState1);
            }
            if (transactionState2 != (CoreTransactionState)(-1))
            {
                subject.SetState(transactionState2);
            }

            var result = subject.IsEmpty;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(CoreTransactionState.Aborted)]
        [InlineData(CoreTransactionState.Committed)]
        [InlineData(CoreTransactionState.InProgress)]
        public void State_should_return_expected_result(CoreTransactionState value)
        {
            var subject = CreateSubject();
            subject.SetState(value);

            var result = subject.State;

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
        [InlineData(CoreTransactionState.Aborted, true)]
        [InlineData(CoreTransactionState.Committed, true)]
        [InlineData(CoreTransactionState.InProgress, false)]
        public void SetState_should_have_expected_result(CoreTransactionState value, bool expectedIsEmpty)
        {
            var subject = CreateSubject();

            subject.SetState(value);

            subject.State.Should().Be(value);
            subject.IsEmpty.Should().Be(expectedIsEmpty);
        }

        // private methods
        private CoreTransaction CreateSubject(
            long transactionNumber = 0,
            TransactionOptions transactionOptions = null)
        {
            return new CoreTransaction(transactionNumber, transactionOptions);
        }
    }
}
