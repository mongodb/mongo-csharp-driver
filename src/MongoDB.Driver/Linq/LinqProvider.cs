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

using System.Threading;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents the different LINQ providers.
    /// </summary>
    public abstract class LinqProvider
    {
        #region static
        // public static fields
        /// <summary>
        /// The LINQ provider that was first shipped with version 2.0 of the driver,
        /// </summary>
        public static readonly LinqProvider V2 = new LinqProviderV2();

        /// <summary>
        /// The LINQ provider that is planned to be the default in version 3.0 of the driver and can be optionally used before that.
        /// </summary>
        public static readonly LinqProvider V3 = new LinqProviderV3();
        #endregion

        // internal methods
        internal abstract IMongoQueryable<TDocument> AsQueryable<TDocument>(IMongoCollection<TDocument> collection, IClientSessionHandle session, AggregateOptions options, CancellationToken cancellationToken);
    }
}
