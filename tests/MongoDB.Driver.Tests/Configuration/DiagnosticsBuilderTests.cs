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
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Events;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class DiagnosticsBuilderTests
{
    [Fact]
    public void Constructor_should_initialize_defaults()
    {
        var subject = new DiagnosticsBuilder();

        subject.LoggerFactory.Should().BeNull();
        subject.LogMaxDocumentSize.Should().Be(MongoInternalDefaults.Logging.MaxDocumentSize);
        subject.TracingEnabled.Should().BeTrue();
        subject.TracingQueryTextMaxLength.Should().Be(0);
        subject.GetSubscribers().Should().BeEmpty();
    }

    [Fact]
    public void Logging_and_tracing_properties_should_be_settable()
    {
        var loggerFactory = Mock.Of<ILoggerFactory>();
        var subject = new DiagnosticsBuilder();

        subject.LoggerFactory = loggerFactory;
        subject.LogMaxDocumentSize = 0;
        subject.TracingEnabled = false;
        subject.TracingQueryTextMaxLength = 512;

        subject.LoggerFactory.Should().BeSameAs(loggerFactory);
        subject.LogMaxDocumentSize.Should().Be(0);
        subject.TracingEnabled.Should().BeFalse();
        subject.TracingQueryTextMaxLength.Should().Be(512);
    }

    [Fact]
    public void LogMaxDocumentSize_should_throw_when_value_is_negative()
    {
        var subject = new DiagnosticsBuilder();

        var exception = Record.Exception(() => subject.LogMaxDocumentSize = -1);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("LogMaxDocumentSize");
        subject.LogMaxDocumentSize.Should().Be(MongoInternalDefaults.Logging.MaxDocumentSize);
    }

    [Fact]
    public void TracingQueryTextMaxLength_should_throw_when_value_is_negative()
    {
        var subject = new DiagnosticsBuilder();

        var exception = Record.Exception(() => subject.TracingQueryTextMaxLength = -1);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("TracingQueryTextMaxLength");
        subject.TracingQueryTextMaxLength.Should().Be(0);
    }

    [Fact]
    public void CreateLoggingSettings_should_return_null_without_a_logger_factory()
    {
        var subject = new DiagnosticsBuilder { LogMaxDocumentSize = 10 };

        subject.CreateLoggingSettings().Should().BeNull();
    }

    [Fact]
    public void CreateLoggingSettings_should_use_the_logger_factory_and_max_document_size()
    {
        var loggerFactory = Mock.Of<ILoggerFactory>();
        var subject = new DiagnosticsBuilder { LoggerFactory = loggerFactory, LogMaxDocumentSize = 10 };

        var result = subject.CreateLoggingSettings();

        result.LoggerFactory.Should().BeSameAs(loggerFactory);
        result.MaxDocumentSize.Should().Be(10);
    }

    [Fact]
    public void CreateTracingOptions_should_return_null_for_the_default_configuration()
    {
        new DiagnosticsBuilder().CreateTracingOptions().Should().BeNull();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 100)]
    [InlineData(false, 100)]
    public void CreateTracingOptions_should_reflect_a_non_default_configuration(bool enabled, int queryTextMaxLength)
    {
        var subject = new DiagnosticsBuilder { TracingEnabled = enabled, TracingQueryTextMaxLength = queryTextMaxLength };

        var result = subject.CreateTracingOptions();

        result.Disabled.Should().Be(!enabled);
        result.QueryTextMaxLength.Should().Be(queryTextMaxLength);
    }

    [Fact]
    public void Subscribe_with_handler_should_return_the_same_builder_and_subscribe_the_handler()
    {
        var subject = new DiagnosticsBuilder();
        var events = new System.Collections.Generic.List<ClusterOpeningEvent>();

        var result = subject.Subscribe<ClusterOpeningEvent>(events.Add);

        result.Should().BeSameAs(subject);
        var subscribers = subject.GetSubscribers();
        subscribers.Should().HaveCount(1);
        subscribers[0].TryGetEventHandler<ClusterOpeningEvent>(out var handler).Should().BeTrue();
        handler(default);
        events.Should().HaveCount(1);
        subscribers[0].TryGetEventHandler<ClusterClosingEvent>(out _).Should().BeFalse();
    }

    [Fact]
    public void Subscribe_with_handler_should_accumulate_handlers_for_the_same_event_type()
    {
        var subject = new DiagnosticsBuilder();
        var calls = new System.Collections.Generic.List<string>();

        subject
            .Subscribe<ClusterOpeningEvent>(_ => calls.Add("first"))
            .Subscribe<ClusterOpeningEvent>(_ => calls.Add("second"));

        var subscribers = subject.GetSubscribers();
        subscribers.Should().HaveCount(2);
        foreach (var subscriber in subscribers)
        {
            subscriber.TryGetEventHandler<ClusterOpeningEvent>(out var handler).Should().BeTrue();
            handler(default);
        }
        calls.Should().Equal("first", "second");
    }

    [Fact]
    public void Subscribe_with_subscriber_should_return_the_same_builder_and_add_the_subscriber()
    {
        var first = Mock.Of<IEventSubscriber>();
        var second = Mock.Of<IEventSubscriber>();
        var subject = new DiagnosticsBuilder();

        var result = subject.Subscribe(first).Subscribe(second);

        result.Should().BeSameAs(subject);
        subject.GetSubscribers().Should().Equal(first, second);
    }

    [Fact]
    public void Subscribe_should_keep_the_order_of_handlers_and_subscribers()
    {
        var subscriber = Mock.Of<IEventSubscriber>();
        var subject = new DiagnosticsBuilder();

        subject.Subscribe<ClusterOpeningEvent>(_ => { }).Subscribe(subscriber).Subscribe<ClusterClosingEvent>(_ => { });

        var subscribers = subject.GetSubscribers();
        subscribers.Should().HaveCount(3);
        subscribers[1].Should().BeSameAs(subscriber);
    }

    [Fact]
    public void Subscribe_with_handler_should_throw_when_handler_is_null()
    {
        var subject = new DiagnosticsBuilder();

        var exception = Record.Exception(() => subject.Subscribe<ClusterOpeningEvent>(null));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("handler");
    }

    [Fact]
    public void Subscribe_with_subscriber_should_throw_when_subscriber_is_null()
    {
        var subject = new DiagnosticsBuilder();

        var exception = Record.Exception(() => subject.Subscribe((IEventSubscriber)null));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("subscriber");
    }
}
