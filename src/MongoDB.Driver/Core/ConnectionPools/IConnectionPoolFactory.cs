/* Copyright 2013-present MongoDB Inc.
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
using System.Net;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal interface IConnectionExceptionHandler
    {
        void HandleExceptionOnOpen(Exception exception);
    }

    internal interface IConnectionPoolFactory
    {
        IConnectionPool CreateConnectionPool(ServerId serverId, EndPoint endPoint, IConnectionExceptionHandler connectionExceptionHandler);
    }
}
