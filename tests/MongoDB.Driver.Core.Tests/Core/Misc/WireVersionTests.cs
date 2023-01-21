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

using System;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Misc
{
    public class WireVersionTests
    {
        [Fact]
        public void Server_maxWireVersion_should_be_in_supported_range()
        {
            RequireServer.Check().StableServer(stable: true);

            var serverMaxWireVersion = CoreTestConfiguration.MaxWireVersion;

            var isOverlaped = WireVersion.SupportedWireVersionRange.Overlaps(new Range<int>(serverMaxWireVersion, serverMaxWireVersion));

            isOverlaped.Should().BeTrue($"Server MaxWireVersion: {serverMaxWireVersion} is not in supported range for the driver: {WireVersion.SupportedWireVersionRange}");
        }

        [Theory]
        [InlineData(14, "5.1")]
        [InlineData(1000, "Unknown (wire version 1000)")]
        public void GetServerVersionForErrorMessage_should_return_expected_serverVersion_message(int wireVersion, string message)
        {
            WireVersion.GetServerVersionForErrorMessage(wireVersion).Should().Be(message);
        }

        [Fact]
        public void SupportedWireRange_should_be_correct()
        {
            WireVersion.SupportedWireVersionRange.Should().Be(new Range<int>(6, 19));
        }

        [Fact]
        public void ToServerVersion_should_throw_if_wireVersion_less_than_0()
        {
            var exception = Record.Exception(() => WireVersion.ToServerVersion(-1));

            exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject.ParamName.Should().Be("wireVersion");
        }

        [Theory]
        [InlineData(99, null, null)]
        [InlineData(20, null, null)]
        [InlineData(19, 6, 2)]
        [InlineData(18, 6, 1)]
        [InlineData(17, 6, 0)]
        [InlineData(16, 5, 3)]
        [InlineData(15, 5, 2)]
        [InlineData(14, 5, 1)]
        [InlineData(10, 4, 7)]
        [InlineData(0, 0, 0)]
        public void ToServerVersion_with_semanticVersion_should_get_correct_serverVersion(int wireVersion, int? expectedMajorVersion, int? expectedMinorVersion)
        {
            var serverVersion = WireVersion.ToServerVersion(wireVersion);

            if (expectedMajorVersion.HasValue && expectedMinorVersion.HasValue)
            {
                serverVersion.Should().Be(new SemanticVersion(expectedMajorVersion.Value, expectedMinorVersion.Value, 0));
            }
            else
            {
                serverVersion.Should().BeNull();
            }
        }
    }
}
