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
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Configuration;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class CommandEventHelperTests
    {
        [Theory]
        [InlineData("{ xyz : 1 }", false)]
        [InlineData("{ aUTHENTICATE: 1 }", true)]
        [InlineData("{ sASLSTART : 1 }", true)]
        [InlineData("{ sASLCONTINUE : 1 }", true)]
        [InlineData("{ gETNONCE : 1 }", true)]
        [InlineData("{ cREATEUSER : 1 }", true)]
        [InlineData("{ uPDATEUSER : 1, }", true)]
        [InlineData("{ cOPYDBSASLSTART : 1 }", true)]
        [InlineData("{ cOPYDB : 1 }", true)]
        [InlineData("{ " + OppressiveLanguageConstants.LegacyHelloCommandName + " : 1 }", false)]
        [InlineData("{ " + OppressiveLanguageConstants.LegacyHelloCommandName + " : 1, sPECULATIVEAUTHENTICATE : null }", true)]
        [InlineData("{ hello : 1, helloOk : true }", false)]
        [InlineData("{ hello : 1, helloOk : true, sPECULATIVEAUTHENTICATE : null }", true)]
        public void ShouldRedactCommand_should_return_expected_result(string commandJson, bool expectedResult)
        {
            var command = BsonDocument.Parse(commandJson);

            var result = CommandEventHelperReflector.ShouldRedactCommand(command);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void ShouldTrackState_should_be_correct(
            [Values(false, true)] bool logCommands,
            [Values(false, true)] bool captureCommandSucceeded,
            [Values(false, true)] bool captureCommandFailed,
            [Values(false, true)] bool traceCommands)
        {
            var mockLogger = new Mock<ILogger<LogCategories.Command>>();
            mockLogger.Setup(m => m.IsEnabled(LogLevel.Debug)).Returns(logCommands);

            var eventCapturer = new EventCapturer();
            // Capture unrelated event, so events filtering is enabled.
            eventCapturer.Capture<SdamInformationEvent>();
            if (captureCommandSucceeded)
            {
                eventCapturer.Capture<CommandSucceededEvent>(_ => true);
            }
            if (captureCommandFailed)
            {
                eventCapturer.Capture<CommandFailedEvent>(_ => true);
            }

            var eventLogger = new EventLogger<LogCategories.Command>(eventCapturer, mockLogger.Object);
            var tracingOptions = traceCommands ? new TracingOptions() : new TracingOptions { Disabled = true };
            var commandHelper = new CommandEventHelper(eventLogger, tracingOptions);

            commandHelper._shouldTrackState().Should().Be(logCommands || captureCommandSucceeded || captureCommandFailed || traceCommands);
        }
    }

    internal static class CommandEventHelperReflector
    {
        public static bool _shouldTrackState(this CommandEventHelper commandEventHelper) =>
            (bool)Reflector.GetFieldValue(commandEventHelper, nameof(_shouldTrackState));


        public static bool ShouldRedactCommand(BsonDocument command) =>
            (bool)Reflector.InvokeStatic(typeof(CommandEventHelper), nameof(ShouldRedactCommand), command);
    }
}
