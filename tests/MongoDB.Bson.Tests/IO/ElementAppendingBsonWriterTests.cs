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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ElementAppendingBsonWriterTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var wrapped = new Mock<IBsonWriter>().Object;
            var elements = new List<BsonElement>(new[] { new BsonElement("a", 1) });
            Action<BsonWriterSettings> settingsConfigurator = s => { };

            var subject = new ElementAppendingBsonWriter(wrapped, elements, settingsConfigurator);

            subject.Wrapped.Should().BeSameAs(wrapped);
            subject._depth().Should().Be(0);
            subject._elements().Should().Equal(elements);
        }

        [Fact]
        public void constructor_should_enumerate_elements()
        {
            var wrapped = new Mock<IBsonWriter>().Object;
            var mockElements = new Mock<IEnumerable<BsonElement>>();
            mockElements.Setup(m => m.GetEnumerator()).Returns(Enumerable.Empty<BsonElement>().GetEnumerator());
            Action<BsonWriterSettings> settingsConfigurator = s => { };

            var subject = new ElementAppendingBsonWriter(wrapped, mockElements.Object, settingsConfigurator);

            mockElements.Verify(m => m.GetEnumerator(), Times.Once);
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var elements = new List<BsonElement>(new[] { new BsonElement("a", 1) });
            Action<BsonWriterSettings> settingsConfigurator = s => { };

            var exception = Record.Exception(() => new ElementAppendingBsonWriter(null, elements, settingsConfigurator));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void constructor_should_throw_when_elements_is_null()
        {
            var wrapped = new Mock<IBsonWriter>().Object;
            Action<BsonWriterSettings> settingsConfigurator = s => { };

            var exception = Record.Exception(() => new ElementAppendingBsonWriter(wrapped, null, settingsConfigurator));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("elements");
        }

        [Fact]
        public void constructor_should_use_default_settings_configurator_when_settings_configurator_is_null()
        {
            var wrapped = new Mock<IBsonWriter>().Object;
            var elements = new List<BsonElement>(new[] { new BsonElement("a", 1) });

            var subject = new ElementAppendingBsonWriter(wrapped, elements, null);

            subject._settingsConfigurator().Should().NotBeNull();
        }

        [Fact]
        public void default_settings_configurator_should_not_alter_settings()
        {
            var wrapped = new Mock<IBsonWriter>().Object;
            var mockElements = new Mock<IEnumerable<BsonElement>>();
            mockElements.Setup(m => m.GetEnumerator()).Returns(Enumerable.Empty<BsonElement>().GetEnumerator());
            Action<BsonWriterSettings> settingsConfigurator = s => { };
            var subject = new ElementAppendingBsonWriter(wrapped, mockElements.Object, settingsConfigurator);
            var configurator = subject._settingsConfigurator();
            var frozenSettings = new BsonBinaryWriterSettings().Freeze();

            configurator(frozenSettings); // would throw if configurator tried to alter any settings
        }

        [Fact]
        public void WriteEndDocument_should_decrement_depth()
        {
            var writer = new Mock<IBsonWriter>().Object;
            var elements = new BsonElement[0];
            var subject = new ElementAppendingBsonWriter(writer, elements, null);
            subject.WriteStartDocument();

            subject.WriteEndDocument();

            subject._depth().Should().Be(0);
        }

        [Fact]
        public void WriteEndDocument_should_push_settings_and_write_elements_when_depth_becomes_zero()
        {
            var mockWriter = new Mock<IBsonWriter>();
            var elements = new[] { new BsonElement("a", 1) };
            Action<BsonWriterSettings> configurator = s => { };
            var subject = new ElementAppendingBsonWriter(mockWriter.Object, elements, configurator);
            subject.WriteStartDocument();

            subject.WriteEndDocument();

            mockWriter.Verify(m => m.WriteStartDocument(), Times.Once);
            mockWriter.Verify(m => m.PushSettings(configurator), Times.Once);
            mockWriter.Verify(m => m.WriteName("a"), Times.Once);
            mockWriter.Verify(m => m.WriteInt32(1), Times.Once);
            mockWriter.Verify(m => m.PopSettings(), Times.Once);
            mockWriter.Verify(m => m.WriteEndDocument(), Times.Once);
        }

        [Fact]
        public void WriteEndDocument_should_not_write_elements_when_nested_WriteEndDocument_is_called()
        {
            var mockWriter = new Mock<IBsonWriter>();
            var elements = new[] { new BsonElement("a", 1) };
            var subject = new ElementAppendingBsonWriter(mockWriter.Object, elements, null);
            subject.WriteStartDocument();
            subject.WriteStartDocument();

            subject.WriteEndDocument();

            mockWriter.Verify(m => m.WriteStartDocument(), Times.Exactly(2));
            mockWriter.Verify(m => m.WriteEndDocument(), Times.Once);
            mockWriter.Verify(m => m.WriteName(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void WriteStartDocument_should_increment_depth()
        {
            var writer = new Mock<IBsonWriter>().Object;
            var elements = new BsonElement[0];
            var subject = new ElementAppendingBsonWriter(writer, elements, null);

            subject.WriteStartDocument();

            subject._depth().Should().Be(1);
        }

        [Fact]
        public void WriteStartDocument_should_call_wrapped()
        {
            var mockWriter = new Mock<IBsonWriter>();
            var elements = new BsonElement[0];
            var subject = new ElementAppendingBsonWriter(mockWriter.Object, elements, null);

            subject.WriteStartDocument();

            mockWriter.Verify(m => m.WriteStartDocument(), Times.Once);
        }
    }

    internal static class ElementAppendingBsonWriterReflector
    {
        public static int _depth(this ElementAppendingBsonWriter obj)
        {
            var fieldInfo = typeof(ElementAppendingBsonWriter).GetField("_depth", BindingFlags.NonPublic | BindingFlags.Instance);
            return (int)fieldInfo.GetValue(obj);
        }

        public static List<BsonElement> _elements(this ElementAppendingBsonWriter obj)
        {
            var fieldInfo = typeof(ElementAppendingBsonWriter).GetField("_elements", BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<BsonElement>)fieldInfo.GetValue(obj);
        }

        public static Action<BsonWriterSettings> _settingsConfigurator(this ElementAppendingBsonWriter obj)
        {
            var fieldInfo = typeof(ElementAppendingBsonWriter).GetField("_settingsConfigurator", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Action<BsonWriterSettings>)fieldInfo.GetValue(obj);
        }
    }
}
