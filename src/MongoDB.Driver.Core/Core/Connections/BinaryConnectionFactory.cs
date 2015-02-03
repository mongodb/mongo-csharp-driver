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

using System.Net;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a factory of BinaryConnections.
    /// </summary>
    internal class BinaryConnectionFactory : IConnectionFactory
    {
        #region static
        // static fields
        private static readonly IConnectionInitializer __connectionInitializer;

        // static constructor
        static BinaryConnectionFactory()
        {
            __connectionInitializer = new ConnectionInitializer();
        }
        #endregion

        // fields
        private readonly IConnectionListener _listener;
        private readonly ConnectionSettings _settings;
        private readonly IStreamFactory _streamFactory;

        // constructors
        public BinaryConnectionFactory()
            : this(new ConnectionSettings(), new TcpStreamFactory(), null)
        {
        }

        public BinaryConnectionFactory(ConnectionSettings settings, IStreamFactory streamFactory, IConnectionListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _streamFactory = Ensure.IsNotNull(streamFactory, "streamFactory");
            _listener = listener;
        }

        // methods
        public IConnection CreateConnection(ServerId serverId, EndPoint endPoint)
        {
            Ensure.IsNotNull(serverId, "serverId");
            Ensure.IsNotNull(endPoint, "endPoint");
            return new BinaryConnection(serverId, endPoint, _settings, _streamFactory, __connectionInitializer, _listener);
        }
    }
}
