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
using MongoDB.Driver.Internal;

namespace MongoDB.DriverUnitTests
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

            if (server.RequestConnection != null)
            {
                throw new InvalidOperationException("FailPoint cannot be used when you are already in a RequestStart.");
            }

            _name = name;
            _server = server;
            _serverInstance = serverInstance;
            _adminDatabase = server.GetDatabase("admin");
            _request = server.RequestStart(_adminDatabase, serverInstance);
        }

        // public methods
        public bool IsSupported()
        {
            if (_serverInstance.BuildInfo.Version < new Version(2, 3, 0))
            {
                return false;
            }

            var parameterValue = GetParameterValue();

            // treat "0" and "false" as false even though JavaScript truthiness would consider them to be true
            if (parameterValue.IsString)
            {
                var s = parameterValue.AsString;
                if (s == "0" || s.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return parameterValue.ToBoolean();
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
                            using (_server.RequestStart(_adminDatabase, _serverInstance))
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
        private BsonValue GetParameterValue()
        {
            // allow environment variable to provide value in case authentication prevents use of getParameter command
            var environmentVariableValue = Environment.GetEnvironmentVariable("mongod.enableTestCommands");
            if (environmentVariableValue != null)
            {
                return environmentVariableValue;
            }

            var command = new CommandDocument
            {
                { "getParameter", 1 },
                { "enableTestCommands", 1 }
            };
            var result = _adminDatabase.RunCommand(command);
            return result.Response["enableTestCommands"];
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
