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
using Xunit;
using PoolState = MongoDB.Driver.Core.ConnectionPools.ExclusiveConnectionPool.PoolState;
using State = MongoDB.Driver.Core.ConnectionPools.ExclusiveConnectionPool.State;

namespace MongoDB.Driver.Core.Tests.Core.ConnectionPools
{
    public class ExclusiveConnectionPoolStateTests
    {
        [Fact]
        public void PoolState_should_start_at_initial_state()
        {
            var poolState = new PoolState("TestPool");
            poolState.State.Should().Be(State.Uninitialized);
        }

        [Theory]
        [InlineData(new[] { State.Disposed })]
        [InlineData(new[] { State.Disposed, State.Disposed })]
        [InlineData(new[] { State.Paused, State.Ready })]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable })]
        [InlineData(new[] { State.Paused, State.Paused })]
        [InlineData(new[] { State.Paused, State.Disposed })]
        [InlineData(new[] { State.Paused, State.Disposed, State.Disposed })]
        [InlineData(new[] { State.Paused, State.Ready, State.Disposed })]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable, State.Disposed })]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable, State.ReadyNonPausable })]
        [InlineData(new[] { State.Paused, State.Ready, State.Ready })]
        [InlineData(new[] { State.Paused, State.Ready, State.Paused, State.Ready })]
        [InlineData(new[] { State.Paused, State.Ready, State.Paused, State.Ready, State.Disposed })]
        internal void PoolState_should_transition_on_valid_transitions(State[] states)
        {
            _ = CreatePoolStateAndValidate(states);
        }

        [Theory]
        [InlineData(null, State.Uninitialized)]
        [InlineData(null, State.Ready)]
        [InlineData(null, State.ReadyNonPausable)]
        [InlineData(new[] { State.Paused }, State.Uninitialized)]
        [InlineData(new[] { State.Paused, State.Ready }, State.Uninitialized)]
        [InlineData(new[] { State.Paused, State.Ready }, State.ReadyNonPausable)]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable }, State.Uninitialized)]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable }, State.Paused)]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable }, State.Ready)]
        [InlineData(new[] { State.Disposed }, State.Uninitialized)]
        [InlineData(new[] { State.Disposed }, State.Ready)]
        [InlineData(new[] { State.Disposed }, State.ReadyNonPausable)]
        [InlineData(new[] { State.Disposed }, State.Paused)]
        internal void PoolState_should_throw_on_invalid_transitions(State[] validStates, State invalidState)
        {
            var poolState = CreatePoolStateAndValidate(validStates);

            var exception = Record.Exception(() => poolState.TransitionState(invalidState));

            if (poolState.State == State.Disposed)
            {
                exception.Should().BeOfType<ObjectDisposedException>();
            }
            else
            {
                exception.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Fact]
        internal void PoolState_ThrowIfDisposed_should_throw_when_disposed()
        {
            var poolState = CreatePoolStateAndValidate(State.Disposed);

            var exception = Record.Exception(() => poolState.ThrowIfDisposed());
            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new[] { State.Paused })]
        [InlineData(new[] { State.Paused, State.Ready })]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable })]
        internal void PoolState_ThrowIfDisposed_should_not_throw_when_not_disposed(State[] states)
        {
            var poolState = CreatePoolStateAndValidate(states);
            poolState.ThrowIfDisposed();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new[] { State.Disposed })]
        [InlineData(new[] { State.Paused })]
        internal void PoolState_ThrowIfDisposedOrNotReady_should_throw_when_not_ready(State[] states)
        {
            var poolState = CreatePoolStateAndValidate(states);
            var exception = Record.Exception(() => poolState.ThrowIfNotReady());

            switch (poolState.State)
            {
                case State.Disposed:
                    exception.Should().BeOfType<ObjectDisposedException>();
                    break;
                case State.Paused:
                    exception.Should().BeOfType<MongoConnectionPoolPausedException>();
                    break;
                default:
                    exception.Should().BeOfType<InvalidOperationException>();
                    break;
            }
        }

        [Fact]
        internal void PoolState_ThrowIfDisposedOrNotReady_should_not_throw_when_ready()
        {
            var poolState = CreatePoolStateAndValidate(State.Paused, State.Ready);
            poolState.ThrowIfNotReady();

            poolState = CreatePoolStateAndValidate(State.Paused, State.ReadyNonPausable);
            poolState.ThrowIfNotReady();
        }

        [Fact]
        internal void PoolState_ThrowIfNotInitialized_should_throw_when_not_initialized()
        {
            var poolState = CreatePoolStateAndValidate();
            var exception = Record.Exception(() => poolState.ThrowIfNotInitialized());
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [InlineData(new[] { State.Disposed })]
        [InlineData(new[] { State.Paused })]
        [InlineData(new[] { State.Paused, State.Ready })]
        [InlineData(new[] { State.Paused, State.ReadyNonPausable })]
        internal void PoolState_ThrowIfNotInitialized_should_not_throw_when_initialized(State[] states)
        {
            var poolState = CreatePoolStateAndValidate(states);
            poolState.ThrowIfNotInitialized();
        }

        // private methods
        private PoolState CreatePoolStateAndValidate(params State[] states)
        {
            var poolState = new PoolState("TestPool");

            if (states != null)
            {
                foreach (var state in states)
                {
                    poolState.TransitionState(state);
                    poolState.State.Should().Be(state);
                }
            }

            return poolState;
        }
    }
}
