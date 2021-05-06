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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators.Finalizers
{
    internal class SingleOrDefaultFinalizer<TOutput> : IExecutableQueryFinalizer<TOutput, TOutput>
    {
        public TOutput Finalize(IAsyncCursor<TOutput> cursor, CancellationToken cancellationToken)
        {
            var output = cursor.ToList(cancellationToken);
            return output.SingleOrDefault();
        }

        public async Task<TOutput> FinalizeAsync(IAsyncCursor<TOutput> cursor, CancellationToken cancellationToken)
        {
            var output = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            return output.SingleOrDefault();
        }
    }
}
