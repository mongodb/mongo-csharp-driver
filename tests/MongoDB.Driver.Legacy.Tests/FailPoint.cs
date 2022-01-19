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

using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests
{
    public static class FailPointName
    {
        // public constants
        public const string MaxTimeAlwaysTimeout = "maxTimeAlwaysTimeOut";
    }

    public class FailPoint : IDisposable
    {
        // private fields
        private readonly MongoDatabase _adminDatabase;
        private readonly string _name;
        private readonly IDisposable _request;
        private readonly MongoServer _server;
        private readonly MongoServerInstance _serverInstance;
        private bool _disposed;
        private bool _wasSet;

        // constructors
        public FailPoint(string name, MongoServer server, MongoServerInstance serverInstance)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (server == null) { throw new ArgumentNullException("server"); }
            if (serverInstance == null) { throw new ArgumentNullException("serverInstance"); }

            if (server.RequestServerInstance != null)
            {
                throw new InvalidOperationException("FailPoint cannot be used when you are already in a RequestStart.");
            }

            _name = name;
            _server = server;
            _serverInstance = serverInstance;
            _adminDatabase = server.GetDatabase("admin");
            _request = server.RequestStart(serverInstance);
        }

        // public methods
        public bool IsSupported()
        {
            return IsThisFailPointSupported();
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
                        if (_wasSet)
                        {
                            // use a new request to set the mode off because the original connection could be closed if an error occurred
                            using (_server.RequestStart(_serverInstance))
                            {
                                SetMode("off");
                            }
                        }
                    }
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        public void SetAlwaysOn()
        {
            SetMode("alwaysOn");
            _wasSet = true;
        }

        public void SetTimes(int n)
        {
            SetMode(new BsonDocument("times", n));
            _wasSet = true;
        }

        // private methods
        private bool IsThisFailPointSupported()
        {
            // some failpoints aren't supported everywhere
            switch (_name)
            {
                case FailPointName.MaxTimeAlwaysTimeout:
                    if (_serverInstance.InstanceType == MongoServerInstanceType.ShardRouter)
                    {
                        return false;
                    }
                    break;
            }

            return true;
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
