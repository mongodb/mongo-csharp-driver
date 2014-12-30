/* Copyright 2010-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    public sealed class ProjectingAsyncCursor<TFrom, TTo> : IAsyncCursor<TTo>
    {
        private readonly IAsyncCursor<TFrom> _wrapped;
        private readonly Func<IEnumerable<TFrom>, IEnumerable<TTo>> _projector;

        public ProjectingAsyncCursor(IAsyncCursor<TFrom> wrapped, Func<IEnumerable<TFrom>, IEnumerable<TTo>> projector)
        {
            _wrapped = Ensure.IsNotNull(wrapped, "wrapped");
            _projector = Ensure.IsNotNull(projector, "projector");
        }

        public IEnumerable<TTo> Current
        {
            get { return _projector(_wrapped.Current); }
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return _wrapped.MoveNextAsync(cancellationToken);
        }

        public void Dispose()
        {
            _wrapped.Dispose();
        }
    }
}
