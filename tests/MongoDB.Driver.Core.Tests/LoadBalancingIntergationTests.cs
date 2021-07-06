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
    public class LoadBalancingIntergationTests : OperationTestBase
    {
        public LoadBalancingIntergationTests()
        {
            _collectionNamespace = CollectionNamespace.FromFullName("db.coll");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_pin_connection_as_expected(
            [Range(1, 3)] int attempts,
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
                    DisposableReadBindingBundle readBindingsBundle;
                    IAsyncCursor<BsonDocument> asyncCursor;

                    using (session = CreateSession(cluster, implicitSession))
                    {
                        AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i);

                            readBindingsBundle.RetryableReadContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableReadContext, async);

                            AssertSessionReferenceCount(session, 4);
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                            AssertChannelReferenceCount(asyncCursor, 2);
                            AssertCommand(eventCapturer, "find", onlySingleCommand: true);
                        }
                        AssertSessionReferenceCount(session, 2); // -2 because bundle consist of two disposable steps
                        AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                        AssertChannelReferenceCount(asyncCursor, 1);

                        asyncCursor.MoveNext().Should().BeTrue(); // no op
                        asyncCursor.MoveNext().Should().BeTrue();

                        AssertSessionReferenceCount(session, 2);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                        AssertChannelReferenceCount(asyncCursor, 1);
                        AssertCommand(eventCapturer, "getMore", onlySingleCommand: true);

                        if (forceCursorClose)
                        {
                            asyncCursor.Dispose();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 0);
                            AssertChannelReferenceCount(asyncCursor, 0);
                            AssertCommand(eventCapturer, "killCursors", onlySingleCommand: false);
                            AssertCheckInOnlyEvents(eventCapturer);
                        }
                        else
                        {
                            asyncCursor.MoveNext().Should().BeTrue(); // returns cursorId = 0
                            asyncCursor.MoveNext().Should().BeFalse();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 0);
                            AssertChannelReferenceCount(asyncCursor, null); // effective channel count is 0, but we cannot asser it anymore
                            AssertCommand(eventCapturer, "getMore", onlySingleCommand: false);
                            AssertCheckInOnlyEvents(eventCapturer);
                        }
                    }
                    AssertSessionReferenceCount(session, 0);
                }
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
                DisposableReadBindingBundle readBindingsBundle;
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
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, 1, shouldHelloBeCalled: false);

                            readBindingsBundle.RetryableReadContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 4); // +1 for failpoint
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = (AsyncCursor<BsonDocument>)CreateAndRunFindOperation(readBindingsBundle.RetryableReadContext, async);

                            AssertSessionReferenceCount(session, 5); // +1 for failpoint
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                            AssertChannelReferenceCount(asyncCursor, 2);
                            AssertCommand(eventCapturer, "find", onlySingleCommand: true);
                        }
                        AssertSessionReferenceCount(session, 3); // -2 because bundle consist of two disposable steps and +1 for failpoint
                        AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                        AssertChannelReferenceCount(asyncCursor, 1);

                        asyncCursor.MoveNext(CancellationToken.None).Should().BeTrue(); // no op
                        asyncCursor._isInProgress().Should().BeFalse();
                        var concurrentMoveNextTask = Task.Factory.StartNew(() => asyncCursor.MoveNext(CancellationToken.None).Should().BeTrue());
                        SpinWait.SpinUntil(() => asyncCursor._isInProgress(), deferGetMoreTimeout).Should().BeTrue();

                        asyncCursor.Dispose();
                        eventCapturer.Any().Should().BeFalse();

                        eventCapturer.WaitForOrThrowIfTimeout(e => e.Count() >= 4, deferGetMoreTimeout.Add(TimeSpan.FromSeconds(1))); // getMore + killCursors + checkingIn + checkedIn

                        concurrentMoveNextTask.GetAwaiter().GetResult(); // propagate exceptions
                        AssertSessionReferenceCount(session, 2);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 0);
                        AssertChannelReferenceCount(asyncCursor, 0);
                        AssertCommand(eventCapturer, "getMore", onlySingleCommand: false);
                        AssertCommand(eventCapturer, "killCursors", onlySingleCommand: false);
                        AssertCheckInOnlyEvents(eventCapturer);
                        AssertSessionReferenceCount(session, 2);
                    }
                    AssertSessionReferenceCount(session, 1);
                }
                AssertSessionReferenceCount(session, 0);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_pin_connection_in_transaction_with_new_sessions_as_expected(
            [Range(1, 3)] int attempts,
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
                    DisposableReadBindingBundle readBindingsBundle;
                    IAsyncCursor<BsonDocument> asyncCursor;

                    using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                    {
                        AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            AssertSessionReferenceCount(session, 3);
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i);

                            readBindingsBundle.RetryableReadContext.PinConnectionIfRequired();

                            AssertSessionReferenceCount(session, 3);
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableReadContext, async);

                            AssertSessionReferenceCount(session, 4);
                            AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 3);
                            AssertChannelReferenceCount(asyncCursor, 3);
                            AssertCommand(eventCapturer, "find", onlySingleCommand: true);
                        }
                        AssertSessionReferenceCount(session, 2); // -2 because bundle consist of two disposable steps
                        AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                        AssertChannelReferenceCount(asyncCursor, 2);

                        var hasNext = asyncCursor.MoveNext(); // no op
                        hasNext = asyncCursor.MoveNext();

                        AssertSessionReferenceCount(session, 2);
                        AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                        AssertChannelReferenceCount(asyncCursor, 2);
                        AssertCommand(eventCapturer, "getMore", onlySingleCommand: true);

                        if (forceCursorClose)
                        {
                            asyncCursor.Dispose();

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            AssertChannelSourceReferenceCount(asyncCursor, 0);
                            AssertChannelReferenceCount(asyncCursor, 1);
                            AssertCommand(eventCapturer, "killCursors", onlySingleCommand: true);
                        }
                        else
                        {
                            hasNext = asyncCursor.MoveNext(); // cursorId = 0

                            AssertSessionReferenceCount(session, 1);
                            AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            AssertChannelSourceReferenceCount(asyncCursor, null);
                            AssertChannelReferenceCount(asyncCursor, null); // effective channel count is 1, but we cannot assert it anymore
                            AssertCommand(eventCapturer, "getMore", onlySingleCommand: true);
                        }
                    }
                    AssertCommand(eventCapturer, "abortTransaction", onlySingleCommand: false);
                    AssertCheckInOnlyEvents(eventCapturer);
                    AssertSessionReferenceCount(session, 0);
                    AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 0);
                }
            }
        }

        [SkippableTheory(Skip = "Investigate")]
        [ParameterAttributeData]
        public void Cursor_should_pin_connection_in_transaction_with_the_same_session_as_expected(
            [Range(1, 3)] int attempts,
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

                ICoreSessionHandle session;
                DisposableReadBindingBundle readBindingsBundle = null;
                IAsyncCursor<BsonDocument> asyncCursor;

                using (session = CreateSession(cluster, isImplicit: false, withTransaction: true))
                {
                    for (int i = 1; i <= attempts; i++)
                    {
                        //AssertSessionReferenceCount(session, 1);

                        eventCapturer.Any().Should().BeFalse();
                        using (readBindingsBundle = CreateEffectiveReadBindingsAndRetryableReadContext(cluster, session.Fork(), async))
                        {
                            //AssertSessionReferenceCount(session, 3);
                            //AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                            AssertCheckOutOnlyEvents(eventCapturer, i, shouldNextAttemptTriggerCheckout: false);

                            readBindingsBundle.RetryableReadContext.PinConnectionIfRequired();

                            //AssertSessionReferenceCount(session, 3);
                            //AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                            eventCapturer.Any().Should().BeFalse();

                            asyncCursor = CreateAndRunFindOperation(readBindingsBundle.RetryableReadContext, async);

                            //AssertSessionReferenceCount(session, 4);
                            //AssertReadBindingReferenceCount(readBindingsBundle.ReadBindingHandle, 1);
                            //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 3);
                            //AssertChannelReferenceCount(asyncCursor, 3);
                            AssertCommand(eventCapturer, "find", onlySingleCommand: true);
                        }
                        //AssertSessionReferenceCount(session, 2); // -2 because bundle consist of two disposable steps
                        //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                        //AssertChannelReferenceCount(asyncCursor, 2);

                        asyncCursor.MoveNext().Should().BeTrue(); // no op
                        asyncCursor.MoveNext().Should().BeTrue();

                        //AssertSessionReferenceCount(session, 2);
                        //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 2);
                        //AssertChannelReferenceCount(asyncCursor, 2);
                        AssertCommand(eventCapturer, "getMore", onlySingleCommand: true);

                        if (i == attempts) // call abort transaction only on the last attempt
                        {
                            if (forceCursorClose)
                            {
                                asyncCursor.Dispose();

                                //AssertSessionReferenceCount(session, 1);
                                //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                                //AssertChannelSourceReferenceCount(asyncCursor, 0);
                                //AssertChannelReferenceCount(asyncCursor, 1);
                                AssertCommand(eventCapturer, "killCursors", onlySingleCommand: true);
                            }
                            else
                            {
                                asyncCursor.MoveNext().Should().BeTrue(); // returns cursorId = 0
                                asyncCursor.MoveNext().Should().BeFalse();

                                //AssertSessionReferenceCount(session, 1);
                                //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 1);
                                //AssertChannelSourceReferenceCount(asyncCursor, null);
                                //AssertChannelReferenceCount(asyncCursor, null); // effectiv channel count is 1, but we cannot assert it anymore
                                AssertCommand(eventCapturer, "getMore", onlySingleCommand: true);
                            }
                        }
                    }
                }
                AssertCommand(eventCapturer, "abortTransaction", onlySingleCommand: false);
                AssertCheckInOnlyEvents(eventCapturer);
                //AssertSessionReferenceCount(session, 0);
                //AssertChannelReferenceCount(readBindingsBundle.RetryableReadContext.Channel, 0);
            }
        }

        // private methods
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

        private void AssertCommand(EventCapturer eventCapturer, string commandName, bool onlySingleCommand)
        {
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Subject.CommandName.Should().Be(commandName);
            eventCapturer.Any().Should().Be(!onlySingleCommand);
        }

        private void AssertReadBindingReferenceCount(IReadBindingHandle readBinding, int expectedValue) => readBinding._reference_referenceCount().Should().Be(expectedValue);
        private void AssertSessionReferenceCount(ICoreSessionHandle coreSession, int expectedValue)
        {
            coreSession._wrapped_referenceCount().Should().Be(expectedValue);
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

        private DisposableReadBindingBundle CreateEffectiveReadBindingsAndRetryableReadContext(ICluster cluster, ICoreSessionHandle sessionHandle, bool async)
        {
            var readPreference = ReadPreference.Primary;

            var effectiveReadBindings = ChannelPinningHelper.CreateEffectiveReadBinding(cluster, sessionHandle, readPreference);
            var retryableReadContext = async
                ? RetryableReadContext.CreateAsync(effectiveReadBindings, retryRequested: false, CancellationToken.None).GetAwaiter().GetResult()
                : RetryableReadContext.Create(effectiveReadBindings, retryRequested: false, CancellationToken.None);

            return new DisposableReadBindingBundle(effectiveReadBindings, retryableReadContext);
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
            RequireServer.Check().LoadBalancing(enabled: true, ignorePreviousSetup: true);
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
        private class DisposableReadBindingBundle : IDisposable
        {
            private readonly IReadBindingHandle _readBindingHandle;
            private readonly RetryableReadContext _retryableReadContext;

            public DisposableReadBindingBundle(IReadBindingHandle readBindingHandle, RetryableReadContext retryableReadContext)
            {
                _readBindingHandle = readBindingHandle;
                _retryableReadContext = retryableReadContext;
            }

            public IReadBindingHandle ReadBindingHandle => _readBindingHandle;
            public RetryableReadContext RetryableReadContext => _retryableReadContext;

            public void Dispose()
            {
                _retryableReadContext.Dispose();
                _readBindingHandle.Dispose();
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

        public static int _reference_referenceCount(this IReadBindingHandle readBindingHandle)
        {
            var reference = Reflector.GetFieldValue(readBindingHandle, "_reference");
            return (int)Reflector.GetFieldValue(reference, "_referenceCount");
        }
    }
}
