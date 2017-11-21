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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ReadConcernHelperTests
    {
        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_document_should_contain_the_expected_values(
            [Values(false, true)]bool supportsSessions, 
            [Values(false, true)]bool causallyConsistent, 
            [Values(null, 10L)]long? operationTime,
            [Values(null, ReadConcernLevel.Majority)]ReadConcernLevel? level)
        {
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(supportsSessions: supportsSessions);
            BsonTimestamp timestamp = operationTime == null ? null : new BsonTimestamp(operationTime.Value);
            var session = OperationTestHelper.CreateSession(causallyConsistent, timestamp);

            var readConcern = ReadConcern.Default;
            if (level != null)
            {
                readConcern = readConcern.With(level.Value);
            }

            var command = new BsonDocument();
            ReadConcernHelper.AppendReadConcern(command, readConcern, connectionDescription, session);

            if ((!supportsSessions || !causallyConsistent || operationTime == null) && level == null)
            {
                command.ElementCount.Should().Be(0);
            }
            else
            {
                command.Contains("readConcern").Should().BeTrue();
            }

            if (level != null)
            {
                command["readConcern"]["level"].AsString.Should().Be("majority");
            }

            if (supportsSessions && causallyConsistent && operationTime != null)
            {
                command["readConcern"]["afterClusterTime"].AsBsonTimestamp.Value.Should().Be(operationTime.Value);
            }
        }
    }
}
