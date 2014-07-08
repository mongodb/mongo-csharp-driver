/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.SyncExtensionMethods
{
    public static class IReadBindingExtensionMethods
    {
        // static methods
        public static IConnectionSource GetReadConnectionSource(this IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return binding.GetReadConnectionSourceAsync(timeout, cancellationToken).GetAwaiter().GetResult();
        }
    }

    public static class IWriteBindingExtensionMethods
    {
        // static methods
        public static IConnectionSource GetWriteConnectionSource(this IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return binding.GetWriteConnectionSourceAsync(timeout, cancellationToken).GetAwaiter().GetResult();
        }
    }

    public static class IConnectionSourceExtensionMethods
    {
        // static methods
        public static IConnection GetConnection(this IConnectionSource connectionSource, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return connectionSource.GetConnectionAsync(timeout, cancellationToken).GetAwaiter().GetResult();
        }
    }
}
