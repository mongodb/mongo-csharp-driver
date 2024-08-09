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

using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class MessageJsonEncoderBaseTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var textReader = new StringReader("");
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();

            var result = new FakeMessageJsonEncoder(textReader, textWriter, encoderSettings);

            result._encoderSettings().Should().BeSameAs(encoderSettings);
            result._textReader().Should().BeSameAs(textReader);
            result._textWriter().Should().BeSameAs(textWriter);
        }

        // nested types
        private class FakeMessageJsonEncoder : MessageJsonEncoderBase
        {
            public FakeMessageJsonEncoder(TextReader textReader, TextWriter textWriter, MessageEncoderSettings encoderSettings)
                : base(textReader, textWriter, encoderSettings)
            {
            }
        }
    }

    public static class MessageJsonEncoderBaseReflector
    {
        public static MessageEncoderSettings _encoderSettings(this MessageJsonEncoderBase obj)
        {
            var fieldInfo = typeof(MessageJsonEncoderBase).GetField("_encoderSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            return (MessageEncoderSettings)fieldInfo.GetValue(obj);
        }

        public static TextReader _textReader(this MessageJsonEncoderBase obj)
        {
            var fieldInfo = typeof(MessageJsonEncoderBase).GetField("_textReader", BindingFlags.NonPublic | BindingFlags.Instance);
            return (TextReader)fieldInfo.GetValue(obj);
        }

        public static TextWriter _textWriter(this MessageJsonEncoderBase obj)
        {
            var fieldInfo = typeof(MessageJsonEncoderBase).GetField("_textWriter", BindingFlags.NonPublic | BindingFlags.Instance);
            return (TextWriter)fieldInfo.GetValue(obj);
        }
    }
}
