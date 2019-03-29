/* Copyright 2019-present MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Clusters
{
    public class DnsClientWrapperTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new DnsClientWrapper();

            subject.LookupClient.Should().NotBeNull();
        }

        [Fact]
        public void LookupClient_should_return_expected_result()
        {
            var subject = new DnsClientWrapper();

            var result = subject.LookupClient;

            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData("_mongodb._tcp.test5.test.build.10gen.cc", new[] { "localhost.test.build.10gen.cc.:27017" }, false)]
        [InlineData("_mongodb._tcp.test5.test.build.10gen.cc", new[] { "localhost.test.build.10gen.cc.:27017" }, true)]
        public void ResolveSrvRecords_should_return_expected_result(string service, string[] expectedEndPoints, bool async)
        {
            var subject = new DnsClientWrapper();

            List<SrvRecord> result;
            if (async)
            {
                result = subject.ResolveSrvRecords(service, CancellationToken.None);
            }
            else
            {
                result = subject.ResolveSrvRecordsAsync(service, CancellationToken.None).GetAwaiter().GetResult();
            }

            var actualEndPoints = result.Select(s => EndPointHelper.ToString(s.EndPoint)).ToList();
            actualEndPoints.Should().BeEquivalentTo(expectedEndPoints);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveSrvRecords_should_throw_when_service_is_null(bool async)
        {
            var subject = new DnsClientWrapper();

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.ResolveSrvRecords(null, CancellationToken.None));
            }
            else
            {
                exception = Record.Exception(() => subject.ResolveSrvRecordsAsync(null, CancellationToken.None).GetAwaiter().GetResult());
            }

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("service");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveSrvRecords_should_throw_when_cancellation_is_already_requested(bool async)
        {
            var subject = new DnsClientWrapper();
            var service = "_mongodb._tcp.test5.test.build.10gen.cc";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.ResolveSrvRecords(service, cts.Token));
            }
            else
            {
                exception = Record.Exception(() => subject.ResolveSrvRecordsAsync(service, cts.Token).GetAwaiter().GetResult());
            }

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Theory]
        [InlineData("test5.test.build.10gen.cc", "replicaSet=repl0&authSource=thisDB", false)]
        [InlineData("test5.test.build.10gen.cc", "replicaSet=repl0&authSource=thisDB", true)]
        public void ResolveTxtRecords_should_return_expected_result(string domainName, string expectedString, bool async)
        {
            var subject = new DnsClientWrapper();

            List<TxtRecord> result;
            if (async)
            {
                result = subject.ResolveTxtRecords(domainName, CancellationToken.None);
            }
            else
            {
                result = subject.ResolveTxtRecordsAsync(domainName, CancellationToken.None).GetAwaiter().GetResult();
            }

            result.Should().HaveCount(1);
            result[0].Strings.Should().HaveCount(1);
            result[0].Strings[0].Should().Be(expectedString);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveTxtRecords_should_throw_when_domainName_is_null(bool async)
        {
            var subject = new DnsClientWrapper();

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.ResolveTxtRecords(null, CancellationToken.None));
            }
            else
            {
                exception = Record.Exception(() => subject.ResolveTxtRecordsAsync(null, CancellationToken.None).GetAwaiter().GetResult());
            }

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("domainName");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveTxtRecords_should_throw_when_cancellation_is_already_requested(bool async)
        {
            var subject = new DnsClientWrapper();
            var domainName = "test5.test.build.10gen.cc";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.ResolveTxtRecords(domainName, cts.Token));
            }
            else
            {
                exception = Record.Exception(() => subject.ResolveTxtRecordsAsync(domainName, cts.Token).GetAwaiter().GetResult());
            }

            exception.Should().BeOfType<OperationCanceledException>();
        }
    }
}
