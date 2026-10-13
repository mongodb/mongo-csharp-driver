/* Copyright 2026-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Driver.StreamProcessing;
using Xunit;

namespace MongoDB.Driver.Tests.StreamProcessing
{
    public class StreamProcessingClientTests
    {
        [Theory]
        [InlineData("mongodb://atlas-stream-699c842ef433fe6001480b17-etif1.virginia-usa.a.query.mongodb.net/")]
        [InlineData("mongodb://user:pass@atlas-stream-xyz.us-east-1.a.query.mongodb.net:27017/?retryWrites=true")]
        [InlineData("mongodb://user:pass@atlas-stream-699c842ef433fe6001480b17-etif1.virginia-usa.a.query.mongodb-stage.net")]
        [InlineData("MONGODB://atlas-stream-xyz.us-east-1.a.query.mongodb.net/")]
        public void IsWorkspaceUri_returns_true_for_workspace_endpoints(string uri)
        {
            StreamProcessingClient.IsWorkspaceUri(uri).Should().BeTrue();
        }

        [Theory]
        [InlineData("mongodb://localhost:27017/")]
        [InlineData("mongodb+srv://cluster0.example.mongodb.net/")]
        [InlineData("mongodb://atlas-stream-x.example.com/")]
        [InlineData("mongodb://abc.virginia-usa.a.query.mongodb.net/")]
        [InlineData("")]
        [InlineData(null)]
        public void IsWorkspaceUri_returns_false_for_other_endpoints(string uri)
        {
            StreamProcessingClient.IsWorkspaceUri(uri).Should().BeFalse();
        }

        [Fact]
        public void Constructor_rejects_non_workspace_uri()
        {
            var ex = Record.Exception(() => new StreamProcessingClient("mongodb://localhost:27017/"));
            ex.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("workspace endpoint URI");
        }

        [Fact]
        public void Constructor_rejects_srv_scheme()
        {
            var ex = Record.Exception(() => new StreamProcessingClient(
                "mongodb+srv://atlas-stream-x.us-east-1.a.query.mongodb.net/"));
            ex.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("workspace endpoint URI");
        }

        [Fact]
        public void Constructor_rejects_null_uri()
        {
            var ex = Record.Exception(() => new StreamProcessingClient(null));
            ex.Should().BeOfType<ArgumentNullException>();
        }
    }
}
