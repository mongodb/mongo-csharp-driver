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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetEffectiveReadPreferenceTestCases))]
        public void GetEffectiveReadPreferenceTests(
            ReadPreference expectedReadPreference,
            ReadPreference defaultReadPreference,
            IClientSessionHandle session)
        {
            var result = session.GetEffectiveReadPreference(defaultReadPreference);

            result.Should().Be(expectedReadPreference);
        }

        public static IEnumerable<object[]> GetEffectiveReadPreferenceTestCases()
        {
            var noTransactionSession = CreateSessionMock(null);
            var inTransactionSession = CreateSessionMock(new TransactionOptions(readPreference: ReadPreference.Nearest));
            var inTransactionNoPreferenceSession = CreateSessionMock(new TransactionOptions());

            yield return [ReadPreference.Primary, null, noTransactionSession];
            yield return [ReadPreference.SecondaryPreferred, ReadPreference.SecondaryPreferred, noTransactionSession];

            yield return [ReadPreference.Nearest, ReadPreference.SecondaryPreferred, inTransactionSession];

            yield return [ReadPreference.Primary, null, inTransactionNoPreferenceSession];
            yield return [ReadPreference.SecondaryPreferred, ReadPreference.SecondaryPreferred, inTransactionNoPreferenceSession];
        }

        private static IClientSessionHandle CreateSessionMock(TransactionOptions transactionOptions)
        {
            var sessionMock = new Mock<IClientSessionHandle>();
            if (transactionOptions != null)
            {
                sessionMock.SetupGet(s => s.IsInTransaction).Returns(true);
                var coreSessionMock = new Mock<ICoreSessionHandle>();
                coreSessionMock.SetupGet(s => s.CurrentTransaction).Returns(new CoreTransaction(0, transactionOptions));
                sessionMock.SetupGet(s => s.WrappedCoreSession).Returns(coreSessionMock.Object);
            }

            return sessionMock.Object;
        }
    }
}

