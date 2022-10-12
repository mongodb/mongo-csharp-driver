/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.Logging;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public sealed class UnifiedLogMatcher
    {
        private UnifiedValueMatcher _valueMatcher;

        public UnifiedLogMatcher(UnifiedValueMatcher valueMatcher)
        {
            _valueMatcher = valueMatcher;
        }

        public void AssertLogsMatch(LogEntry[] actualLogs, BsonArray expectedLogs)
        {
            actualLogs.Length.Should().Be(expectedLogs.Count);

            for (int i = 0; i < expectedLogs.Count; i++)
            {
                var expectedLogDocument = expectedLogs[i].AsBsonDocument;
                var actualLog = actualLogs[i];

                var expectedCategory = UnifiedLogHelper.ParseCategory(expectedLogDocument["component"].AsString);
                var expectedLogLevel = UnifiedLogHelper.ParseLogLevel(expectedLogDocument["level"].AsString);

                actualLog.Category.Should().Be(expectedCategory);
                actualLog.LogLevel.Should().Be(expectedLogLevel);

                var actualLogDocument = new BsonDocument(actualLog.State.Select(ToBsonElement));
                _valueMatcher.AssertValuesMatch(actualLogDocument, expectedLogDocument["data"]);
            }
        }

        private static BsonElement ToBsonElement(KeyValuePair<string, object> pair) =>
             new(MongoUtils.ToCamelCase(pair.Key), BsonValue.Create(pair.Value));
    }
}
