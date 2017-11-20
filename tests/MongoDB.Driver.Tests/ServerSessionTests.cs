/* Copyright 2017 MongoDB Inc.
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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ServerSessionTests
    {
        [Fact]
        public void GenerateSessionId_should_return_expected_result()
        {
            var result = ServerSessionReflector.GenerateSessionId();

            result.ElementCount.Should().Be(1);
            result.GetElement(0).Name.Should().Be("id");
            result["id"].BsonType.Should().Be(BsonType.Binary);
            var id = result["id"].AsBsonBinaryData;
            id.SubType.Should().Be(BsonBinarySubType.UuidStandard);
            id.Bytes.Length.Should().Be(16);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new ServerSession();

            result.Id.Should().NotBeNull();
            result.LastUsedAt.Should().NotHaveValue();
        }

        [Fact]
        public void Id_should_return_expected_result()
        {
            var subject = new ServerSession();

            var result = subject.Id;

            result.Should().NotBeNull();
        }

        [Fact]
        public void LastUsedAt_should_return_expected_result_when_WasUsed_has_never_been_called()
        {
            var subject = new ServerSession();

            var result = subject.LastUsedAt;

            result.Should().NotHaveValue();
        }

        [Fact]
        public void LastUsedAt_should_return_expected_result_when_WasUsed_has_been_called()
        {
            var subject = new ServerSession();
            subject.WasUsed();

            var result = subject.LastUsedAt;

            result.Should().BeCloseTo(DateTime.UtcNow);
        }

        [Fact]
        public void Dispose_should_do_nothing()
        {
            var subject = new ServerSession();

            subject.Dispose();

            subject.WasUsed(); // call some method to assert no ObjectDisposedException is thrown
        }

        [Fact]
        public void WasUsed_should_have_expected_result()
        {
            var subject = new ServerSession();

            subject.WasUsed();

            subject.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow);
        }
    }

    internal static class ServerSessionReflector
    {
        public static BsonDocument GenerateSessionId()
        {
            var methodInfo = typeof(ServerSession).GetMethod("GenerateSessionId", BindingFlags.NonPublic | BindingFlags.Static);
            return (BsonDocument)methodInfo.Invoke(null, new object[0]);
        }
    }
}
