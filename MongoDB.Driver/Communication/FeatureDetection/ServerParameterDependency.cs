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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver.Communication.FeatureDetection
{
    internal class ServerParameterDependency : IFeatureDependency
    {
        // private fields
        private readonly string _parameterName;

        // constructors
        public ServerParameterDependency(string parameterName)
        {
            _parameterName = parameterName;
        }

        // public methods
        public bool IsMet(FeatureContext context)
        {
            var parameterValue = GetParameterValue(context);

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

        // private methods
        private BsonValue GetParameterValue(FeatureContext context)
        {
            // allow environment variable to provide value in case authentication prevents use of getParameter command
            var environmentVariableName = "mongod." + _parameterName;
            var environmentVariableValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (environmentVariableValue != null)
            {
                return environmentVariableValue;
            }

            var command = new CommandDocument
            {
                { "getParameter", 1 },
                { _parameterName, 1 }
            };

            var commandOperation = new CommandOperation<CommandResult>(
                "admin", // databaseName
                new BsonBinaryReaderSettings(), // readerSettings
                new BsonBinaryWriterSettings(), // writerSettings
                command,
                QueryFlags.SlaveOk,
                null, // options
                null, // readPreference
                null, // serializationOptions
                BsonSerializer.LookupSerializer(typeof(CommandResult)) // resultSerializer
            );

            var result = commandOperation.Execute(context.Connection);
            return result.Response[_parameterName];
        }
    }
}
