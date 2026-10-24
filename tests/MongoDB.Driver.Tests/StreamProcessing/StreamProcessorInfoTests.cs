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

using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.StreamProcessing;
using Xunit;

namespace MongoDB.Driver.Tests.StreamProcessing
{
    public class StreamProcessorInfoTests
    {
        private static StreamProcessorInfo CreateFrom(BsonDocument raw)
        {
            // Constructor is internal; use reflection so tests don't depend on
            // a public hook just for unit tests.
            var ctor = typeof(StreamProcessorInfo).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(BsonDocument) },
                null);
            ctor.Should().NotBeNull();
            return (StreamProcessorInfo)ctor.Invoke(new object[] { raw });
        }

        [Fact]
        public void Getters_expose_populated_fields()
        {
            var raw = new BsonDocument
            {
                { "id", "proc-1" },
                { "name", "smokeTestProcessor" },
                { "state", "CREATED" },
                { "pipeline", new BsonArray { new BsonDocument("$source", new BsonDocument("connectionName", "sample_stream_solar")) } },
                { "pipelineVersion", 2 },
                { "tier", "SP2" },
                { "streamMetaFieldName", "_stream_meta" },
                { "enableAutoScaling", true },
                { "failoverEnabled", false },
                { "activeRegion", "us-east-1" },
                { "workspaceDefaultRegion", "us-east-1" },
                { "modifiedBy", "user-1" },
                { "hasStarted", false },
                { "errorMsg", "" },
                { "errorRetryable", false }
            };

            var info = CreateFrom(raw);
            info.Id.Should().Be("proc-1");
            info.Name.Should().Be("smokeTestProcessor");
            info.State.Should().Be("CREATED");
            info.PipelineVersion.Should().Be(2);
            info.Tier.Should().Be("SP2");
            info.StreamMetaFieldName.Should().Be("_stream_meta");
            info.AutoScalingEnabled.Should().BeTrue();
            info.FailoverEnabled.Should().BeFalse();
            info.ActiveRegion.Should().Be("us-east-1");
            info.WorkspaceDefaultRegion.Should().Be("us-east-1");
            info.ModifiedBy.Should().Be("user-1");
            info.HasStarted.Should().BeFalse();
            info.ErrorMessage.Should().BeEmpty();
            info.ErrorRetryable.Should().BeFalse();
            info.Pipeline.Should().HaveCount(1);
            info.Pipeline[0]["$source"]["connectionName"].AsString.Should().Be("sample_stream_solar");
        }

        [Fact]
        public void Getters_return_defaults_when_fields_missing()
        {
            var info = CreateFrom(new BsonDocument
            {
                { "name", "p" },
                { "state", "CREATED" }
            });

            info.Id.Should().BeNull();
            info.PipelineVersion.Should().NotHaveValue();
            info.Tier.Should().BeNull();
            info.Dlq.Should().BeNull();
            info.StreamMetaFieldName.Should().BeNull();
            info.ActiveRegion.Should().BeNull();
            info.WorkspaceDefaultRegion.Should().BeNull();
            info.ModifiedBy.Should().BeNull();
            info.AutoScalingEnabled.Should().BeFalse();
            info.FailoverEnabled.Should().BeFalse();
            info.HasStarted.Should().BeFalse();
            info.ErrorMessage.Should().BeEmpty();
            info.ErrorRetryable.Should().BeFalse();
            info.ErrorCode.Should().NotHaveValue();
            info.Pipeline.Should().BeEmpty();
        }

        [Fact]
        public void Error_fields_exposed_when_set()
        {
            var info = CreateFrom(new BsonDocument
            {
                { "name", "p" },
                { "state", "FAILED" },
                { "errorMsg", "something went wrong" },
                { "errorRetryable", true },
                { "errorCode", 125 }
            });

            info.ErrorMessage.Should().Be("something went wrong");
            info.ErrorRetryable.Should().BeTrue();
            info.ErrorCode.Should().Be(125);
        }
    }
}
