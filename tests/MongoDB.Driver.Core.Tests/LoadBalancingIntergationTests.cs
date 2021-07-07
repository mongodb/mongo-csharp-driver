/* Copyright 2021-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Core.Tests
{
    /// <summary>
    /// Tests in this file emulate internal steps in operations that can work with cursors or transactions.
    /// </summary>
    public class LoadBalancingIntergationTests : OperationTestBase
    {
        public LoadBalancingIntergationTests()
        {
            _collectionNamespace = CollectionNamespace.FromFullName("db.coll");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void BulkWrite_should_pin_connection_as_expected(
            [Values(1, 3)] int attempts,
            [Values(false, true)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                for (int i = 1; i <= attempts; i++)
                {
                    ICoreSessionHandle session;
                    DisposableBindingBundle<IReadWriteBindingHandle, RetryableWriteContext> writeBindingsBundle;

                    using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                    {
                        AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();
                        using (writeBindingsBundle = CreateEffectiveReadWriteBindingsAndRetryableWriteContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i);

                            writeBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            // +1 because now channel handle is pinned to session
                            AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 2);
                            eventCapturer.Any().Should().BeFalse();

                            _ = CreateAndRunBulkOperation(writeBindingsBundle.RetryableContext, async);

                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 2);
                            AssertCommand(eventCapturer, "insert", noMoreEvents: true);
                        }
                        AssertSessionReferenceCount(session, 1);
                        AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 1);
                    }
                    AssertCommand(eventCapturer, "abortTransaction", noMoreEvents: false);
                    AssertCheckInOnlyEvents(eventCapturer);
                    AssertSessionReferenceCount(session, 0);
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void BulkWrite_and_cursor_should_share_pinned_connection_under_the_same_transaction_2(
            [Values(false, true)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                ICoreSessionHandle session;
                DisposableBindingBundle<IReadWriteBindingHandle, RetryableWriteContext> writeBindingsBundle;
                DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle;
                IAsyncCursor<BsonDocument> asyncCursor;

                using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                {
                    AssertSessionReferenceCount(session, 1);

                    eventCapturer.Any().Should().BeFalse();

                    // bulk operation
                    using (writeBindingsBundle = CreateEffectiveReadWriteBindingsAndRetryableWriteContext(cluster, session.Fork(), async))
                    {
                        AssertSessionReferenceCount(session, 3);
                        AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 1);
                        AssertCheckOutOnlyEvents(eventCapturer, 1);

                        writeBindingsBundle.RetryableContext.PinConnectionIfRequired();

                        AssertSessionReferenceCount(session, 3);
                        // +1 because now channel handle is pinned to session
                        AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 2);
                        eventCapturer.Any().Should().BeFalse();

                        _ = CreateAndRunBulkOperation(writeBindingsBundle.RetryableContext, async);

                        AssertSessionReferenceCount(session, 3);
                        AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 2);
                        AssertCommand(eventCapturer, "insert", noMoreEvents: true);
                    }
                    AssertSessionReferenceCount(session, 1);
                    AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 1);

                    // find operation
                    using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                    {
                        AssertSessionReferenceCount(session, 3);
                        // +1 gives pinnedChannel.Fork()
                        // +2 creating retryableContext by ChannelReadWriteBinding
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 4);
                        eventCapturer.Any().Should().BeFalse();

                        readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                        AssertSessionReferenceCount(session, 3);
                        // -2 gives disposing initial retryable context
                        // +1 gives pinning, +1 gives creating cursor
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 3);
                        eventCapturer.Any().Should().BeFalse();

                        asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                        AssertSessionReferenceCount(session, 4);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 4);
                        AssertCommand(eventCapturer, "find", noMoreEvents: true);
                    }
                }

                MoveNext(asyncCursor, async).Should().BeTrue(); // no op
                MoveNext(asyncCursor, async).Should().BeTrue(); // getMore
                AssertCommand(eventCapturer, "getMore", noMoreEvents: true);
                MoveNext(asyncCursor, async).Should().BeTrue(); // getMore
                AssertCommand(eventCapturer, "getMore", noMoreEvents: true);
                MoveNext(asyncCursor, async).Should().BeTrue(); // cursorId = 0
                AssertCommand(eventCapturer, "getMore", noMoreEvents: false);
                AssertCommand(eventCapturer, "abortTransaction", noMoreEvents: false);
                AssertCheckInOnlyEvents(eventCapturer);
                MoveNext(asyncCursor, async).Should().BeFalse();

                AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
                AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 0);
                AssertSessionReferenceCount(session, 0);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void BulkWrite_and_cursor_should_share_pinned_connection_under_the_same_transaction(
            [Values(1, 3)] int attempts,
            [Values(false, true)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                for (int i = 1; i <= attempts; i++)
                {
                    ICoreSessionHandle session;
                    DisposableBindingBundle<IReadWriteBindingHandle, RetryableWriteContext> writeBindingsBundle;

                    using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                    {
                        AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();

                        // bulk operation
                        using (writeBindingsBundle = CreateEffectiveReadWriteBindingsAndRetryableWriteContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i);

                            writeBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            // +1 because now channel handle is pinned to session
                            AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 2);
                            eventCapturer.Any().Should().BeFalse();

                            _ = CreateAndRunBulkOperation(writeBindingsBundle.RetryableContext, async);

                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 2);
                            AssertCommand(eventCapturer, "insert", noMoreEvents: true);
                        }
                        AssertSessionReferenceCount(session, 1);
                        AssertChannelReferenceCount(writeBindingsBundle.RetryableContext.Channel, 1);

                        // find operation
                        DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle;
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            // +1 gives pinnedChannel.Fork()
                            // +2 creating retryableContext by ChannelReadWriteBinding
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 4);
                            eventCapturer.Any().Should().BeFalse();

                            readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            // -2 gives disposing initial retryable context
                            // +1 gives pinning, +1 gives creating cursor
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 3);
                            eventCapturer.Any().Should().BeFalse();

                            var asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                            AssertSessionReferenceCount(session, 4);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 4);
                            AssertCommand(eventCapturer, "find", noMoreEvents: true);

                            asyncCursor.Dispose();

                            AssertCommand(eventCapturer, "killCursors", noMoreEvents: true);
                        }
                    }
                    AssertCommand(eventCapturer, "abortTransaction", noMoreEvents: false);
                    AssertCheckInOnlyEvents(eventCapturer);
                    AssertSessionReferenceCount(session, 0);
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_pin_connection_as_expected(
            [Values(1, 3)] int attempts,
            [Values(false, true)] bool implicitSession,
            [Values(false, true)] bool forceCursorClose,
            [Values(false, true)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                for (int i = 1; i <= attempts; i++)
                {
                    ICoreSessionHandle session;
                    DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle;
                    IAsyncCursor<BsonDocument> asyncCursor;

                    using (session = CreateSession(cluster, implicitSession))
                    {
                        AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i);

                            readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                            AssertSessionReferenceCount(session, 4);
                            // +1 gives creating a cursor
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 2);
                            AssertChannelReferenceCount(asyncCursor, 2);
                            AssertCommand(eventCapturer, "find", noMoreEvents: true);
                        }
                        AssertSessionReferenceCount(session, 2);
                        // -1 because context.channelSource contains the same channel as the context.channel, so the second dispose is skipped
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                        AssertChannelReferenceCount(asyncCursor, 1);

                        MoveNext(asyncCursor, async).Should().BeTrue(); // no op
                        MoveNext(asyncCursor, async).Should().BeTrue();

                        AssertSessionReferenceCount(session, 2);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                        AssertChannelReferenceCount(asyncCursor, 1);
                        AssertCommand(eventCapturer, "getMore", noMoreEvents: true);

                        if (forceCursorClose)
                        {
                            asyncCursor.Dispose();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
                            AssertChannelReferenceCount(asyncCursor, 0);
                            AssertCommand(eventCapturer, "killCursors", noMoreEvents: false);
                            AssertCheckInOnlyEvents(eventCapturer);
                        }
                        else
                        {
                            MoveNext(asyncCursor, async).Should().BeTrue(); // returns cursorId = 0
                            MoveNext(asyncCursor, async).Should().BeFalse();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
                            AssertChannelReferenceCount(asyncCursor, null); // effective channel count is 0, but we cannot assert it anymore
                            AssertCommand(eventCapturer, "getMore", noMoreEvents: false);
                            AssertCheckInOnlyEvents(eventCapturer);
                        }
                    }
                    AssertSessionReferenceCount(session, 0);
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_pin_connection_in_transaction_with_new_sessions_as_expected(
            [Values(1, 3)] int attempts,
            [Values(false, true)] bool forceCursorClose,
            [Values(false)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                for (int i = 1; i <= attempts; i++)
                {
                    ICoreSessionHandle session;
                    DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle;
                    IAsyncCursor<BsonDocument> asyncCursor;

                    using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                    {
                        AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i);

                            readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            // +1 because now channel handle is pinned to session
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 2);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                            AssertSessionReferenceCount(session, 4);
                            // +1 gives creating a cursor 
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 3);
                            AssertChannelReferenceCount(asyncCursor, 3);
                            AssertCommand(eventCapturer, "find", noMoreEvents: true);
                        }
                        AssertSessionReferenceCount(session, 2);
                        // -1 because context.channelSource contains the same channel as the context.channel, so the second dispose is skipped
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 2);
                        AssertChannelReferenceCount(asyncCursor, 2);

                        MoveNext(asyncCursor, async).Should().BeTrue(); // no op
                        MoveNext(asyncCursor, async).Should().BeTrue();

                        AssertSessionReferenceCount(session, 2);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 2);
                        AssertChannelReferenceCount(asyncCursor, 2);
                        AssertCommand(eventCapturer, "getMore", noMoreEvents: true);

                        if (forceCursorClose)
                        {
                            asyncCursor.Dispose();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            AssertChannelSourceReferenceCount(asyncCursor, 0);
                            AssertChannelReferenceCount(asyncCursor, 1);
                            AssertCommand(eventCapturer, "killCursors", noMoreEvents: true);
                        }
                        else
                        {
                            MoveNext(asyncCursor, async).Should().BeTrue(); // cursorId = 0
                            MoveNext(asyncCursor, async).Should().BeFalse();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            AssertChannelSourceReferenceCount(asyncCursor, null);
                            AssertChannelReferenceCount(asyncCursor, null); // effective channel count is 1, but we cannot assert it anymore
                            AssertCommand(eventCapturer, "getMore", noMoreEvents: true);
                        }
                    }
                    AssertCommand(eventCapturer, "abortTransaction", noMoreEvents: false);
                    AssertCheckInOnlyEvents(eventCapturer);
                    AssertSessionReferenceCount(session, 0);
                    AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_pin_connection_in_transaction_with_the_same_session_as_expected(
            [Values(1, 4)] int attempts,
            [Values(false, true)] bool forceCursorClose,
            [Values(false)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            List<IAsyncCursor<BsonDocument>> cursors = new();
            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                ICoreSessionHandle session;
                DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle = null;

                using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                {
                    for (int i = 1; i <= attempts; i++)
                    {
                        AssertSessionReferenceCount(session, i); // dynamic value because we don't close cursors in the loop

                        IAsyncCursor<BsonDocument> asyncCursor;
                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertCheckOutOnlyEvents(eventCapturer, i, shouldNextAttemptTriggerCheckout: false);

                            readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                            AssertCommand(eventCapturer, "find", noMoreEvents: true);
                        }
                        MoveNext(asyncCursor, async).Should().BeTrue(); // no op
                        MoveNext(asyncCursor, async).Should().BeTrue();

                        AssertCommand(eventCapturer, "getMore", noMoreEvents: true);

                        cursors.Add(asyncCursor);
                    }
                }

                for (int i = 0; i < cursors.Count; i++)
                {
                    IAsyncCursor<BsonDocument> cursor = cursors[i];
                    if (forceCursorClose)
                    {
                        cursor.Dispose();

                        AssertCommand(eventCapturer, "killCursors", noMoreEvents: i < cursors.Count - 1);
                    }
                    else
                    {
                        MoveNext(cursor, async).Should().BeTrue(); // returns cursorId = 0
                        MoveNext(cursor, async).Should().BeFalse();

                        AssertCommand(eventCapturer, "getMore", noMoreEvents: i < cursors.Count - 1);
                    }
                }
                AssertCommand(eventCapturer, "abortTransaction", noMoreEvents: false);
                AssertCheckInOnlyEvents(eventCapturer);
                AssertSessionReferenceCount(session, 0);
                AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_unpin_connection_for_operations_under_the_same_transaction_after_abortTransaction_and_cursor_dispose(
            [Values(1, 3)] int attempts,
            [Values(false, true)] bool forceCursorClose,
            [Values(false, true)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>()
                .Capture<ConnectionFailedEvent>()
                .Capture<ConnectionClosedEvent>();

            List<IAsyncCursor<BsonDocument>> cursors = new();
            using (var cluster = CreateLoadBalancedCluster(eventCapturer))
            {
                eventCapturer.Clear();

                ICoreSessionHandle session;
                DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle = null;

                using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                {
                    for (int i = 1; i <= attempts; i++)
                    {
                        AssertSessionReferenceCount(session, i); // dynamic value because we don't close cursors in the loop

                        IAsyncCursor<BsonDocument> asyncCursor;
                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertCheckOutOnlyEvents(eventCapturer, i, shouldNextAttemptTriggerCheckout: false);

                            readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                            AssertCommand(eventCapturer, "find", noMoreEvents: true);
                        }
                        MoveNext(asyncCursor, async).Should().BeTrue(); // no op
                        MoveNext(asyncCursor, async).Should().BeTrue();

                        AssertCommand(eventCapturer, "getMore", noMoreEvents: true);

                        cursors.Add(asyncCursor);
                    }

                    AbortTransaction(session, async);
                    AssertCommand(eventCapturer, "abortTransaction", noMoreEvents: true);
                }

                for (int i = 0; i < cursors.Count; i++)
                {
                    IAsyncCursor<BsonDocument> cursor = cursors[i];
                    if (forceCursorClose)
                    {
                        cursor.Dispose();
                    }
                    else
                    {
                        var exception = Record.Exception(() => cursor.MoveNext());
                        exception
                            .Should()
                            .BeOfType<MongoCommandException>()
                            .Subject
                            .Message
                            .Should()
                            .StartWith("Command getMore failed: Cannot run getMore on cursor")
                            .And
                            .EndWith("without a txnNumber.");
                        cursor.Dispose();
                    }

                    AssertCommand(eventCapturer, "killCursors", noMoreEvents: i < cursors.Count - 1);
                }
                AssertCheckInOnlyEvents(eventCapturer);
                AssertSessionReferenceCount(session, 0);
                AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_wait_until_getMore_succeed_if_it_was_concurrently_closed(
            [Values(false, true)] bool implicitSession,
            [Values(false, true)] bool async)
        {
            SkipIfNotLoadBalancingMode();

            KillOpenTransactions();

            SetupData();

            ServiceIdHelper.IsServiceIdEmulationEnabled = true; // TODO: temporary solution to enable emulating serviceId in a server response

            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolCheckedOutConnectionEvent>()
                .Capture<ConnectionPoolCheckingOutConnectionEvent>()
                .Capture<ConnectionPoolCheckedInConnectionEvent>()
                .Capture<ConnectionPoolCheckingInConnectionEvent>()
                .Capture<CommandSucceededEvent>();

            var deferGetMoreTimeout = TimeSpan.FromMilliseconds(500);

            string appName = "ConcurrentCursorCloseApp";
            var timeoutFailPointCommand = BsonDocument.Parse($@"
            {{
                'configureFailPoint' : 'failCommand',
                'mode' : {{
                    'times' : 1
                }},
                'data' : {{
                    'failCommands' : [ 'getMore' ],
                    'appName' : '{appName}',
                    'blockConnection' : true,
                    'blockTimeMS' : {deferGetMoreTimeout.TotalMilliseconds}
                }}
            }}");

            using (var cluster = CreateLoadBalancedCluster(eventCapturer, appName: appName))
            {
                ICoreSessionHandle session;
                DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> readBindingsBundle;
                AsyncCursor<BsonDocument> asyncCursor;

                using (session = CreateSession(cluster, implicitSession))
                {
                    using (FailPoint.Configure(cluster, session, timeoutFailPointCommand))
                    {
                        eventCapturer.Clear();

                        AssertSessionReferenceCount(session, 2); // +1 for failpoint

                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 4); // +1 for failpoint
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, 1, shouldHelloBeCalled: false);

                            readBindingsBundle.RetryableContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 4); // +1 for failpoint
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = (AsyncCursor<BsonDocument>)CreateAndRunFindOperation(readBindingsBundle.RetryableContext, async);

                            // +1 for failpoint
                            AssertSessionReferenceCount(session, 5);
                            // +1 gives creating a cursor
                            AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 2);
                            AssertChannelReferenceCount(asyncCursor, 2);
                            AssertCommand(eventCapturer, "find", noMoreEvents: true);
                        }
                        AssertSessionReferenceCount(session, 3);
                        // -1 because context.channelSource contains the same channel as the context.channel, so the second dispose is skipped
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 1);
                        AssertChannelReferenceCount(asyncCursor, 1);

                        MoveNext(asyncCursor, async).Should().BeTrue(); // no op
                        asyncCursor._isInProgress().Should().BeFalse();
                        var concurrentMoveNextTask = Task.Factory.StartNew(() => MoveNext(asyncCursor, async).Should().BeTrue());
                        SpinWait.SpinUntil(() => asyncCursor._isInProgress(), deferGetMoreTimeout).Should().BeTrue();

                        asyncCursor.Dispose();
                        eventCapturer.Any().Should().BeFalse();

                        eventCapturer.WaitForOrThrowIfTimeout(e => e.Count() >= 4, deferGetMoreTimeout.Add(TimeSpan.FromSeconds(1))); // getMore + killCursors + checkingIn + checkedIn

                        concurrentMoveNextTask.GetAwaiter().GetResult(); // propagate exceptions
                        AssertSessionReferenceCount(session, 2);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableContext.Channel, 0);
                        AssertChannelReferenceCount(asyncCursor, 0);
                        AssertCommand(eventCapturer, "getMore", noMoreEvents: false);
                        AssertCommand(eventCapturer, "killCursors", noMoreEvents: false);
                        AssertCheckInOnlyEvents(eventCapturer);
                        AssertSessionReferenceCount(session, 2);
                    }
                    AssertSessionReferenceCount(session, 1);
                }
                AssertSessionReferenceCount(session, 0);
            }
        }

        // private methods
        private void AbortTransaction(ICoreSessionHandle session, bool async)
        {
            if (async)
            {
                session.AbortTransactionAsync().GetAwaiter().GetResult();
            }
            else
            {
                session.AbortTransaction();
            }
        }

        private void AssertChannelReferenceCount(IChannelHandle channelHandle, int expectedValue)
        {
            channelHandle.Connection._reference_referenceCount().Should().Be(expectedValue);
        }

        private void AssertChannelReferenceCount(IAsyncCursor<BsonDocument> cursor, int? expectedValue)
        {
            var referenceCount = cursor?._channelSource()?._reference_instance_channel_connection()?._reference_referenceCount();
            if (expectedValue.HasValue)
            {
                referenceCount.Should().Be(expectedValue);
            }
            else
            {
                referenceCount.Should().NotHaveValue();
            }
        }

        private void AssertChannelSourceReferenceCount(IAsyncCursor<BsonDocument> cursor, int? expectedValue)
        {
            var referenceCount = cursor?._channelSource()?._reference_referenceCount();
            if (expectedValue.HasValue)
            {
                referenceCount.Should().Be(expectedValue);
            }
            else
            {
                referenceCount.Should().NotHaveValue();
            }
        }

        private void AssertCheckInOnlyEvents(EventCapturer eventCapturer)
        {
            eventCapturer.Next().Should().BeOfType<ConnectionPoolCheckingInConnectionEvent>();
            eventCapturer.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            eventCapturer.Any().Should().BeFalse();
        }

        private void AssertCheckOutOnlyEvents(EventCapturer eventCapturer, int attempt, bool shouldNextAttemptTriggerCheckout = true)
        {
            AssertCheckOutOnlyEvents(eventCapturer, attempt, shouldHelloBeCalled: attempt == 1, shouldNextAttemptTriggerCheckout);
        }

        private void AssertCheckOutOnlyEvents(EventCapturer eventCapturer, int attempt, bool shouldHelloBeCalled, bool shouldNextAttemptTriggerCheckout = true)
        {
            if (attempt == 1 || shouldNextAttemptTriggerCheckout)
            {
                eventCapturer.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
                if (shouldHelloBeCalled) // in other cases we will reuse the first connection
                {
                    eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Subject.CommandName.Should().Be("isMaster");
                    eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Subject.CommandName.Should().Be("buildInfo");
                }
                eventCapturer.Next().Should().BeOfType<ConnectionPoolCheckedOutConnectionEvent>();
            }

            eventCapturer.Any().Should().BeFalse();
        }

        private void AssertCommand(EventCapturer eventCapturer, string commandName, bool noMoreEvents)
        {
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Subject.CommandName.Should().Be(commandName);
            eventCapturer.Any().Should().Be(!noMoreEvents);
        }

        private void AssertSessionReferenceCount(ICoreSessionHandle coreSession, int expectedValue)
        {
            coreSession._wrapped_referenceCount().Should().Be(expectedValue);
        }

        private BulkWriteOperationResult CreateAndRunBulkOperation(RetryableWriteContext context, bool async)
        {
            var bulkInsertOperation = new BulkInsertOperation(
                _collectionNamespace,
                new[] { new InsertRequest(new BsonDocument()) },
                _messageEncoderSettings);

            if (async)
            {
                return bulkInsertOperation.ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return bulkInsertOperation.Execute(context, CancellationToken.None);
            }
        }

        private IAsyncCursor<BsonDocument> CreateAndRunFindOperation(RetryableReadContext context, bool async)
        {
            var findOperation = new FindCommandOperation<BsonDocument>(
                _collectionNamespace,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings)
            {
                BatchSize = 1
            };

            if (async)
            {
                return findOperation.ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return findOperation.Execute(context, CancellationToken.None);
            }
        }

        private DisposableBindingBundle<IReadBindingHandle, RetryableReadContext> CreateEffectiveReadBindingsAndRetryableReadContext(ICluster cluster, ICoreSessionHandle sessionHandle, bool async)
        {
            var readPreference = ReadPreference.Primary;

            var effectiveReadBindings = ChannelPinningHelper.CreateEffectiveReadBinding(cluster, sessionHandle, readPreference);
            var retryableReadContext = async
                ? RetryableReadContext.CreateAsync(effectiveReadBindings, retryRequested: false, CancellationToken.None).GetAwaiter().GetResult()
                : RetryableReadContext.Create(effectiveReadBindings, retryRequested: false, CancellationToken.None);

            return new DisposableBindingBundle<IReadBindingHandle, RetryableReadContext>(effectiveReadBindings, retryableReadContext);
        }

        private DisposableBindingBundle<IReadWriteBindingHandle, RetryableWriteContext> CreateEffectiveReadWriteBindingsAndRetryableWriteContext(ICluster cluster, ICoreSessionHandle sessionHandle, bool async)
        {
            var effectiveReadBindings = ChannelPinningHelper.CreateEffectiveReadWriteBinding(cluster, sessionHandle);
            var retryableReadContext = async
                ? RetryableWriteContext.CreateAsync(effectiveReadBindings, retryRequested: false, CancellationToken.None).GetAwaiter().GetResult()
                : RetryableWriteContext.Create(effectiveReadBindings, retryRequested: false, CancellationToken.None);

            return new DisposableBindingBundle<IReadWriteBindingHandle, RetryableWriteContext>(effectiveReadBindings, retryableReadContext);
        }

        private ICluster CreateLoadBalancedCluster(EventCapturer eventCapturer, string appName = null) =>
            CoreTestConfiguration.CreateCluster(builder =>
            {
                return builder
                    .ConfigureCluster(cs => cs.With(loadBalanced: true))
                    .ConfigureConnection(cc => cc.With(applicationName: appName))
                    .Subscribe(eventCapturer);
            });

        private ICoreSessionHandle CreateSession(ICluster cluster, bool isImplicit, bool withTransaction = false)
        {
            if (isImplicit)
            {
                return NoCoreSession.NewHandle();
            }
            else
            {
                var session = cluster.StartSession();
                if (withTransaction)
                {
                    session.StartTransaction();
                }
                return session;
            }
        }

        private bool MoveNext(IAsyncCursor<BsonDocument> cursor, bool async) =>
            async ? cursor.MoveNextAsync().GetAwaiter().GetResult() : cursor.MoveNext();

        private void SetupData(bool insertInitialData = true)
        {
            DropCollection();
            if (insertInitialData)
            {
                Insert(new BsonDocument(), new BsonDocument());
            }
        }

        private void SkipIfNotLoadBalancingMode()
        {
#if DEBUG
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.Linux);
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.MacOS);
            // Make sure that LB is started. "nginx" is a LB we use for windows testing
            RequireEnvironment.Check().ProcessStarted("nginx");
            Environment.SetEnvironmentVariable("MONGODB_URI", "mongodb://localhost:17017?loadBalanced=true");
            Environment.SetEnvironmentVariable("MONGODB_URI_WITH_MULTIPLE_MONGOSES", "mongodb://localhost:17018?loadBalanced=true");
            RequireServer
                .Check()
                .LoadBalancing(enabled: true, ignorePreviousSetup: true)
                .Authentication(authentication: false); // auth server requires credentials in connection string
#else
            RequireEnvironment.Check().EnvironmentVariable("SINGLE_MONGOS_LB_URI"); // these env variables are used only on the scripting side
            RequireEnvironment.Check().EnvironmentVariable("MULTI_MONGOS_LB_URI");
            // EG currently supports LB only for Ubuntu
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.Windows);
            RequirePlatform.Check().SkipWhen(SupportedOperatingSystem.MacOS);
            RequireServer.Check().ClusterType(ClusterType.LoadBalanced);
#endif
        }

        // nested types
        private class DisposableBindingBundle<TBinding, TRetryableContext> : IDisposable where TBinding : IDisposable where TRetryableContext : IDisposable
        {
            private readonly TBinding _bindingHandle;
            private readonly TRetryableContext _retryableContext;

            public DisposableBindingBundle(TBinding bindingHandle, TRetryableContext retryableContext)
            {
                _bindingHandle = bindingHandle;
                _retryableContext = retryableContext;
            }

            public TBinding BindingHandle => _bindingHandle;
            public TRetryableContext RetryableContext => _retryableContext;

            public void Dispose()
            {
                _retryableContext.Dispose();
                _bindingHandle.Dispose();
            }
        }
    }

    internal static class LoadBalancedReflector
    {
        public static IConnectionHandle _reference_instance_channel_connection(this IChannelSource channelSource)
        {
            var reference = Reflector.GetFieldValue(channelSource, "_reference");
            var instance = Reflector.GetFieldValue(reference, "_instance");
            var channel = Reflector.GetFieldValue(instance, "_channel");
            return (IConnectionHandle)Reflector.GetFieldValue(channel, "_connection");
        }

        public static IChannelSource _channelSource(this IAsyncCursor<BsonDocument> asyncCursor)
        {
            return (IChannelSource)Reflector.GetFieldValue(asyncCursor, "_channelSource");
        }

        public static int _wrapped_referenceCount(this ICoreSessionHandle coreSessionHandle)
        {
            var wrapped = Reflector.GetFieldValue(coreSessionHandle, "_wrapped");
            return (int)Reflector.GetFieldValue(wrapped, "_referenceCount");
        }

        public static int _reference_referenceCount(this IConnectionHandle connectionHandle)
        {
            var reference = Reflector.GetFieldValue(connectionHandle, "_reference");
            return (int)Reflector.GetFieldValue(reference, "_referenceCount");
        }

        public static int _reference_referenceCount(this IChannelSource channelSourceHandle)
        {
            var reference = Reflector.GetFieldValue(channelSourceHandle, "_reference");
            return (int)Reflector.GetFieldValue(reference, "_referenceCount");
        }
    }
}
