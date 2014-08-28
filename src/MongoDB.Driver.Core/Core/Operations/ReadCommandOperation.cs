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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a read command operation.
    /// </summary>
    public class ReadCommandOperation : ReadCommandOperation<BsonDocument>
    {
        // constructors
        public ReadCommandOperation(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    /// <summary>
    /// Represents a read command operation.
    /// </summary>
    public class ReadCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IReadOperation<TCommandResult>
    {
        #region static
        // static fields
        private static ConcurrentDictionary<string, bool> __knownReadCommands = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // static constructor
        static ReadCommandOperation()
        {
            var knownReadCommands = new[]
            {
                "buildInfo",
                "collStats",
                "connectionStatus",
                "count",
                "cursorInfo",
                "dbStats",
                "distinct",
                "features",
                "geoNear",
                "geoSearch",
                "geoWalk",
                "getCmdLineOpts",
                "getParameter",
                "group",
                "hostInfo",
                "isMaster",
                "listCommands",
                "listDatabases",
                "parallelCollectionScan",
                "ping",
                "replSetGetStatus",
                "serverStatus",
                "setParameter",
                "text",
                "whatsmyuri"
            };

            foreach (var command in knownReadCommands)
            {
                __knownReadCommands.TryAdd(command, true);
            }
        }

        // static properties
        public static IEnumerable<string> KnownReadCommands
        {
            get { return __knownReadCommands.Keys; }
        }

        // static methods
        public static void AddKnownReadCommand(string commandName)
        {
            __knownReadCommands.TryAdd(commandName, true);
        }

        private static void EnsureIsKnownReadCommand(string commandName)
        {
            bool value;
            if (!__knownReadCommands.TryGetValue(commandName, out value))
            {
                var message = string.Format("'{0}' is not a known read command. Either use a WriteCommandOperation instead, or add the command name to the KnownReadCommands using AddKnownReadCommand.", commandName);
                throw new ArgumentException(message, "Command");
            }
        }

        private static void EnsureIsReadAggregateCommand(BsonDocument command)
        {
            var pipeline = command["pipeline"].AsBsonArray;
            if (pipeline.Any(s => s.AsBsonDocument.GetElement(0).Name.Equals("$out", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The pipeline for an aggregate command contains a $out operator. Use a WriteCommandOperation instead.");
            }
        }

        public static void EnsureIsReadCommand(BsonDocument command)
        {
            var commandName = command.GetElement(0).Name;

            if (commandName.Equals("aggregate", StringComparison.OrdinalIgnoreCase))
            {
                EnsureIsReadAggregateCommand(command);
                return;
            }

            if (commandName.Equals("mapReduce", StringComparison.OrdinalIgnoreCase))
            {
                EnsureIsReadMapReduceCommand(command);
                return;
            }

            EnsureIsKnownReadCommand(commandName);
        }

        private static void EnsureIsReadMapReduceCommand(BsonDocument command)
        {
            BsonValue output;
            if (command.TryGetValue("out", out output))
            {
                if (output.BsonType == BsonType.Document)
                {
                    var action = output.AsBsonDocument.GetElement(0).Name;
                    if (action.Equals("inline", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            throw new ArgumentException("The mapReduce command outputs results to a collection. Use a WriteCommandOperation instead.");
        }
        #endregion

        // fields
        private Action<BsonDocument> _ensureIsReadCommandAction;

        // constructors
        public ReadCommandOperation(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
            _ensureIsReadCommandAction = EnsureIsReadCommand;
        }

        // properties
        public Action<BsonDocument> EnsureIsReadCommandAction
        {
            get { return _ensureIsReadCommandAction; }
            set { _ensureIsReadCommandAction = value; }
        }

        // methods
        public async Task<TCommandResult> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            if (_ensureIsReadCommandAction != null)
            {
                _ensureIsReadCommandAction(Command);
            }

            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetReadConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteCommandAsync(connectionSource, binding.ReadPreference, slidingTimeout, cancellationToken);
            }
        }
    }
}
