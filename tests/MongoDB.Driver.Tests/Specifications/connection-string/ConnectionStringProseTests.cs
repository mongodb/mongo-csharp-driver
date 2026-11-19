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

using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.connection_string;

public class ConnectionStringProseTests
{
    [Fact]
    public void Redacted_string_representation()
    {
        var mongoClientSettings =
            MongoClientSettings.FromConnectionString("mongodb://SENTINELuser5678:SENTINELpassword1234@localhost:27017/db");

        mongoClientSettings.ToString().Should().NotContainEquivalentOf("SENTINEL");
    }

    [Fact]
    public void Representation_connection_string_that_did_not_parse()
    {
        var exception = Record.Exception(
            () => MongoClientSettings.FromConnectionString("mongodb://SENTINELuser5678:SENTINELpassword1234@localhost:99999/db"));

        exception.Should().NotBeNull().And.BeOfType<MongoConfigurationException>();
        exception.ToString().Should().NotContainEquivalentOf("SENTINEL");
    }
}
