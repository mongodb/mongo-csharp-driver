/* Copyright 2018-present MongoDB Inc.
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

using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Servers
{
    public static class ServerMonitorFactoryReflector
    {
        internal static IConnectionFactory _connectionFactory(this ServerMonitorFactory obj) => (IConnectionFactory)Reflector.GetFieldValue(obj, nameof(_connectionFactory));
        internal static IEventSubscriber _eventSubscriber(this ServerMonitorFactory obj) => (IEventSubscriber)Reflector.GetFieldValue(obj, nameof(_eventSubscriber));
        internal static ServerMonitorSettings _serverMonitorSettings(this ServerMonitorFactory obj) => (ServerMonitorSettings)Reflector.GetFieldValue(obj, nameof(_serverMonitorSettings));
    }
}
