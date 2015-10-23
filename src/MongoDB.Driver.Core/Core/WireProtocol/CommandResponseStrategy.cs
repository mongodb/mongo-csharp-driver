/* Copyright 2013-2015 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol
{
    /// <summary>
    /// Strategy for handling the response from a command.
    /// </summary>
    public abstract class CommandResponseStrategy<TCommandResult>
    {
        /// <summary>
        /// Gets the default command response strategy.
        /// </summary>
        public static CommandResponseStrategy<TCommandResult> Read { get; } = new DefaultCommandResponseStrategy();

        /// <summary>
        /// Gets a strategy that throws away the response and returns the provided result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static CommandResponseStrategy<TCommandResult> ThrowAway(TCommandResult result)
        {
            return new ThrowAwayResponseCommandResponseStrategy(result);
        }

        internal abstract TCommandResult Decide(Func<TCommandResult> read);

        internal abstract Task<TCommandResult> DecideAsync(Func<Task<TCommandResult>> readAsync);

        private class DefaultCommandResponseStrategy : CommandResponseStrategy<TCommandResult>
        {
            internal override TCommandResult Decide(Func<TCommandResult> read)
            {
                return read();
            }

            internal override Task<TCommandResult> DecideAsync(Func<Task<TCommandResult>> readAsync)
            {
                return readAsync();
            }
        }

        private class ThrowAwayResponseCommandResponseStrategy : CommandResponseStrategy<TCommandResult>
        {
            private readonly TCommandResult _result;

            public ThrowAwayResponseCommandResponseStrategy(TCommandResult result)
            {
                _result = result;
            }

            internal override TCommandResult Decide(Func<TCommandResult> read)
            {
                Task.Run(read).IgnoreExceptions();
                return _result;
            }

            internal override Task<TCommandResult> DecideAsync(Func<Task<TCommandResult>> readAsync)
            {
                readAsync().IgnoreExceptions();
                return Task.FromResult(_result);
            }
        }
    }
}
