﻿/* Copyright 2013-2014 MongoDB Inc.
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
        public static IConnectionSourceHandle GetReadConnectionSource(this IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return binding.GetReadConnectionSourceAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }

    public static class IWriteBindingExtensionMethods
    {
        // static methods
        public static IConnectionSourceHandle GetWriteConnectionSource(this IWriteBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return binding.GetWriteConnectionSourceAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }

    public static class IConnectionSourceExtensionMethods
    {
        // static methods
        public static IConnectionHandle GetConnection(this IConnectionSource connectionSource, CancellationToken cancellationToken = default(CancellationToken))
        {
            return connectionSource.GetConnectionAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }
}
