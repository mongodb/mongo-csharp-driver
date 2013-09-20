/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    public class FailPoint : IDisposable
    {
        public const string MaxTimeAlwaysTimeout = "maxTimeAlwaysTimeOut";

        private readonly MongoDatabase _adminDatabase;
        private readonly string _name;
        private readonly IDisposable _request;
        private readonly MongoServer _server;
        private readonly MongoServerInstance _serverInstance;
        private bool _disposed;

        public FailPoint(string name, MongoServer server)
            : this(name, server, ReadPreference.Primary)
        {
        }

        public FailPoint(string name, MongoServer server, ReadPreference readPreference)
        {
            _name = name;
            _server = server;
            _adminDatabase = server.GetDatabase("admin");
            _request = server.RequestStart(_adminDatabase, readPreference);
            _serverInstance = server.RequestConnection.ServerInstance;
        }

        public void AlwaysOn()
        {
            SetMode("alwaysOn");
        }

        public void Off()
        {
            SetMode("off");
        }

        public void Times(int n)
        {
            SetMode(new BsonDocument("times", n));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    try
                    {
                        _request.Dispose();
                    }
                    finally
                    {
                        // use a new request to set the mode off because the original connection could be closed by now
                        using (_server.RequestStart(_adminDatabase, _serverInstance))
                        {
                            SetMode("off");
                        }
                    }
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        private void SetMode(BsonValue mode)
        {
            var command = new CommandDocument
            {
                { "configureFailPoint", _name },
                { "mode", mode }
            };
            _adminDatabase.RunCommand(command);
        }
    }
}
