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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Encryption;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongocryptdFactoryTests
    {
        [Theory]
        [InlineData("mongocryptdURI", "mongodb://localhost:11111", "mongodb://localhost:11111")]
        [InlineData(null, null, "mongodb://localhost:27020")]
        public void CreateMongocryptdConnectionString_should_create_expected_connection_string(string optionKey, string optionValue, string expectedConnectionString)
        {
            var extraOptions = new Dictionary<string, object>();
            if (optionKey != null)
            {
                extraOptions.Add(optionKey, optionValue);
            }
            var subject = new MongocryptdFactory(extraOptions);
            var connectionString = subject.CreateMongocryptdConnectionString();
            connectionString.Should().Be(expectedConnectionString);
        }

        [SkippableTheory]
        [InlineData("{ mongocryptdBypassSpawn : true }", null, null, false)]
        [InlineData(null, "mongocryptd#extension#", "--idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdBypassSpawn : false }", "mongocryptd#extension#", "--idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdBypassSpawn : false, mongocryptdSpawnPath : 'c:/mongocryptd.exe' }", "c:/mongocryptd.exe", "--idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdBypassSpawn : false, mongocryptdSpawnPath : 'c:/mgcr.exe' }", "c:/mgcr.exe", "--idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdBypassSpawn : false, mongocryptdSpawnPath : 'c:/mgcr.exe' }", "c:/mgcr.exe", "--idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        // args string
        [InlineData("{ mongocryptdSpawnArgs : '--arg1 A --arg2 B' }", "mongocryptd#extension#", "--arg1 A --arg2 B --idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdSpawnArgs : '--arg1 A --arg2 B --idleShutdownTimeoutSecs 50' }", "mongocryptd#extension#", "--arg1 A --arg2 B --idleShutdownTimeoutSecs 50 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdSpawnArgs : '--arg1 A --arg2 B --logpath path.txt' }", "mongocryptd#extension#", "--arg1 A --arg2 B --logpath path.txt --idleShutdownTimeoutSecs 60", true)]
        [InlineData("{ mongocryptdSpawnArgs : '--arg1 A --arg2 B --logpath path.txt --logappend' }", "mongocryptd#extension#", "--arg1 A --arg2 B --logpath path.txt --logappend --idleShutdownTimeoutSecs 60", true)]
        [InlineData("{ mongocryptdSpawnArgs : '--arg1 A --arg2 B --logappend' }", "mongocryptd#extension#", "--arg1 A --arg2 B --logappend --idleShutdownTimeoutSecs 60 --logpath #logpath#", true)]
        // args IEnumerable
        [InlineData("{ mongocryptdSpawnArgs : ['arg1 A', 'arg2 B'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --idleShutdownTimeoutSecs 60 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdSpawnArgs : ['arg1 A', 'arg2 B', 'idleShutdownTimeoutSecs 50'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --idleShutdownTimeoutSecs 50 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdSpawnArgs : ['arg1 A', '--arg2 B', '--idleShutdownTimeoutSecs 50'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --idleShutdownTimeoutSecs 50 --logpath #logpath# --logappend", true)]
        [InlineData("{ mongocryptdSpawnArgs : ['arg1 A', 'arg2 B', '--logpath path.txt'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --logpath path.txt --idleShutdownTimeoutSecs 60", true)]
        [InlineData("{ mongocryptdSpawnArgs : ['arg1 A', 'arg2 B', '--logpath path.txt', '--logappend'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --logpath path.txt --logappend --idleShutdownTimeoutSecs 60", true)]
        [InlineData("{ mongocryptdSpawnArgs : ['arg1 A', 'arg2 B', '--logappend'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --logappend --idleShutdownTimeoutSecs 60 --logpath #logpath#", true)]
        [InlineData("{ mongocryptdBypassSpawn : false, mongocryptdSpawnArgs : [ '--arg1 A', '--arg2 B', '--idleShutdownTimeoutSecs 50'] }", "mongocryptd#extension#", "--arg1 A --arg2 B --idleShutdownTimeoutSecs 50 --logpath #logpath# --logappend", true)]
        public void Mongocryptd_should_be_spawned_with_correct_extra_arguments(
            string stringExtraOptions,
            string expectedPath,
            string expectedArgs,
            bool shouldBeSpawned)
        {
            string emptyLogPath;
            string platformExtension;
#if WINDOWS
            emptyLogPath = "nul";
            platformExtension = ".exe";
#else
            emptyLogPath = "/dev/null";
            platformExtension = "";
#endif
            stringExtraOptions = stringExtraOptions?.Replace("#logpath#", emptyLogPath);
            expectedArgs = expectedArgs?.Replace("#logpath#", emptyLogPath);
            expectedPath = expectedPath?.Replace("#extension#", platformExtension);

            var bsonDocumentExtraOptions =
                stringExtraOptions != null
                 ? BsonDocument.Parse(stringExtraOptions)
                 : new BsonDocument();

            var extraOptions = bsonDocumentExtraOptions
                .Elements
                .ToDictionary(k => k.Name, v => CreateTypedExtraOptions(v.Value));

            var subject = new MongocryptdFactory(extraOptions);

            var result = subject.ShouldMongocryptdBeSpawned(out var path, out var args);
            result.Should().Be(shouldBeSpawned);
            path.Should().Be(expectedPath);
            args.Should().Be(expectedArgs);

            object CreateTypedExtraOptions(BsonValue value)
            {
                if (value.IsBsonArray)
                {
                    return value.AsBsonArray; // IEnumerable
                }
                else if (value.IsBoolean)
                {
                    return (bool)value; // bool
                }
                else
                {
                    return (string)value; // string
                }
            }
        }
    }

    internal static class MongocryptdFactoryReflector
    {
        public static string CreateMongocryptdConnectionString(this MongocryptdFactory mongocryptdHelper)
        {
            return (string)Reflector.Invoke(mongocryptdHelper, nameof(CreateMongocryptdConnectionString));
        }

        public static bool ShouldMongocryptdBeSpawned(this MongocryptdFactory mongocryptdHelper, out string path, out string args)
        {
            return (bool)Reflector.Invoke(mongocryptdHelper, nameof(ShouldMongocryptdBeSpawned), out path, out args);
        }
    }
}
