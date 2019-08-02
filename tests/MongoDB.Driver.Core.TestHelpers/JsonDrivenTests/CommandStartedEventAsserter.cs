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
using FluentAssertions;
using FluentAssertions.Execution;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.TestHelpers.JsonDrivenTests
{
    public class CommandStartedEventAsserter : AspectAsserter<CommandStartedEvent>
    {
        // protected methods
        protected override void AssertAspect(CommandStartedEvent actualEvent, string name, BsonValue expectedValue)
        {
            switch (name)
            {
                case "command":
                    AssertCommandAspects(actualEvent.Command, expectedValue.AsBsonDocument);
                    return;

                case "command_name":
                    actualEvent.CommandName.Should().Be(expectedValue.AsString);
                    return;

                case "database_name":
                    actualEvent.DatabaseNamespace.DatabaseName.Should().Be(expectedValue.AsString);
                    return;

                default:
                    throw new FormatException($"Invalid CommandStartedEvent aspect: {name}.");
            }
        }

        // private methods
        private void AdaptExpectedUpdateModels(List<BsonDocument> actualModels, List<BsonDocument> expectedModels)
        {
            if (actualModels.Count != expectedModels.Count)
            {
                return;
            }

            for (var i = 0; i < actualModels.Count; i++)
            {
                var actualModel = actualModels[i];
                var expectedModel = expectedModels[i];

                if (expectedModel.Contains("multi") && expectedModel["multi"] == false && !actualModel.Contains("multi"))
                {
                    expectedModel.Remove("multi");
                }

                if (expectedModel.Contains("upsert") && expectedModel["upsert"] == false && !actualModel.Contains("upsert"))
                {
                    expectedModel.Remove("upsert");
                }
            }
        }

        private void AssertCommandAspects(BsonDocument actualCommand, BsonDocument aspects)
        {
            RecursiveFieldSetter.SetAll(actualCommand, "getMore", 42L);
            RecursiveFieldSetter.SetAll(actualCommand, "afterClusterTime", 42);
            RecursiveFieldSetter.SetAll(actualCommand, "recoveryToken", 42);

            foreach (var aspect in aspects)
            {
                AssertCommandAspect(actualCommand, aspect.Name, aspect.Value);
            }
        }

        private void AssertCommandAspect(BsonDocument actualCommand, string name, BsonValue expectedValue)
        {
            var commandName = actualCommand.ElementCount == 0 ? "<unknown>" : actualCommand.GetElement(0).Name;

            if (expectedValue.IsBsonNull)
            {
                switch (name)
                {
                    case "autocommit":
                    case "readConcern":
                    case "recoveryToken":
                    case "startTransaction":
                    case "txnNumber":
                    case "writeConcern":
                    case "maxTimeMS":
                        if (actualCommand.Contains(name))
                        {
                            throw new AssertionFailedException($"Did not expect field '{name}' in command: {actualCommand.ToJson()}.");
                        }
                        return;
                }
            }

            if (!actualCommand.Contains(name))
            {
                // some missing fields are only missing because the C# driver omits default values
                switch (name)
                {
                    case "new":
                        if (commandName == "findAndModify" && expectedValue == false)
                        {
                            return;
                        }
                        break;
                }

                throw new AssertionFailedException($"Expected field '{name}' in command: {actualCommand.ToJson()}.");
            }

            var actualValue = actualCommand[name];
            if (name == "updates")
            {
                AdaptExpectedUpdateModels(actualValue.AsBsonArray.Cast<BsonDocument>().ToList(), expectedValue.AsBsonArray.Cast<BsonDocument>().ToList());
            }

            var namesToUseOrderInsensitiveComparisonWith = new[] { "writeConcern", "maxTimeMS" };
            var useOrderInsensitiveComparison = namesToUseOrderInsensitiveComparisonWith.Contains(name);

            if (!(useOrderInsensitiveComparison ? BsonValueEquivalencyComparer.Compare(actualValue, expectedValue) : actualValue.Equals(expectedValue)))
            {
                switch (name)
                {
                    case "out":
                        if (commandName == "mapReduce") 
                        {
                            if (expectedValue is BsonString &&
                                actualValue.IsBsonDocument &&
                                actualValue.AsBsonDocument.Contains("replace") &&
                                actualValue["replace"] == expectedValue.AsString)
                            {
                                // allow short form for "out" to be equivalent to the long form
                                // Assumes that the driver is correctly generating the following
                                // fields: db, sharded, nonAtomic
                                return;
                            }
                        }
                        break;
                }

                throw new AssertionFailedException($"Expected field '{name}' in command '{commandName}' to be {expectedValue.ToJson()} but found {actualValue.ToJson()}.");
            }
        }
    }
}
