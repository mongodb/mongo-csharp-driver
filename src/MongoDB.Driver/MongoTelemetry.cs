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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver;

/// <summary>
/// Provides access to MongoDB driver's OpenTelemetry instrumentation.
/// </summary>
public static class MongoTelemetry
{
    private static readonly string s_driverVersion = ClientDocumentHelper.GetAssemblyVersion(typeof(MongoClient).Assembly);

    /// <summary>
    /// The ActivitySource used by MongoDB driver for OpenTelemetry tracing.
    /// Applications can subscribe to this source to receive MongoDB traces.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("MongoDB.Driver", s_driverVersion);

    internal static Activity StartOperationActivity(string operationName, string databaseName, string collectionName = null)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            return null;
        }

        var spanName = GetSpanName(operationName, databaseName, collectionName);
        var activity = ActivitySource.StartActivity(spanName, ActivityKind.Client);

        if (activity?.IsAllDataRequested == true)
        {
            activity.SetTag("db.system", "mongodb");
            activity.SetTag("db.operation.name", operationName);
            activity.SetTag("db.operation.summary", spanName);

            if (!string.IsNullOrEmpty(databaseName))
            {
                activity.SetTag("db.namespace", databaseName);
            }

            if (!string.IsNullOrEmpty(collectionName))
            {
                activity.SetTag("db.collection.name", collectionName);
            }
        }

        return activity;
    }

    internal static Activity StartTransactionActivity()
    {
        var activity = ActivitySource.StartActivity("transaction", ActivityKind.Client);

        if (activity?.IsAllDataRequested == true)
        {
            activity.SetTag("db.system", "mongodb");
        }

        return activity;
    }

    internal static string GetSpanName(string name, string databaseName, string collectionName)
    {
        if (!string.IsNullOrEmpty(collectionName))
        {
            return $"{name} {databaseName}.{collectionName}";
        }
        if (!string.IsNullOrEmpty(databaseName))
        {
            return $"{name} {databaseName}";
        }
        return name;
    }

    internal static Activity StartCommandActivity(
        string commandName,
        BsonDocument command,
        DatabaseNamespace databaseNamespace,
        ConnectionId connectionId,
        int queryTextMaxLength = 0)
    {
        var activity = ActivitySource.StartActivity(commandName, ActivityKind.Client);

        if (activity == null)
        {
            return null;
        }

        if (activity.IsAllDataRequested)
        {
            var collectionName = ExtractCollectionName(command);
            activity.SetTag("db.system", "mongodb");
            activity.SetTag("db.command.name", commandName);
            activity.SetTag("db.namespace", databaseNamespace.DatabaseName);

            if (!string.IsNullOrEmpty(collectionName))
            {
                activity.SetTag("db.collection.name", collectionName);
            }

            // db.query.summary uses the full format like operation-level spans
            var querySummary = GetSpanName(commandName, databaseNamespace.DatabaseName, collectionName);
            activity.SetTag("db.query.summary", querySummary);

            SetConnectionTags(activity, connectionId);

            if (command != null)
            {
                if (command.TryGetValue("lsid", out var lsid))
                {
                    // Materialize the lsid to avoid accessing disposed RawBsonDocument later
                    var materializedLsid = lsid.IsBsonDocument
                        ? new BsonDocument(lsid.AsBsonDocument)
                        : lsid;
                    activity.SetTag("db.mongodb.lsid", materializedLsid);
                }

                if (command.TryGetValue("txnNumber", out var txnNumber))
                {
                    activity.SetTag("db.mongodb.txn_number", txnNumber.ToInt64());
                }

                if (queryTextMaxLength > 0)
                {
                    SetQueryText(activity, command, queryTextMaxLength);
                }
            }
        }

        return activity;
    }

    internal static void RecordException(Activity activity, Exception exception)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        if (exception.StackTrace != null)
        {
            activity.SetTag("exception.stacktrace", exception.StackTrace);
        }
        activity.SetStatus(ActivityStatusCode.Error);
    }

    private static void SetConnectionTags(Activity activity, ConnectionId connectionId)
    {
        var endPoint = connectionId?.ServerId?.EndPoint;
        switch (endPoint)
        {
            case IPEndPoint ipEndPoint:
                activity.SetTag("server.address", ipEndPoint.Address.ToString());
                activity.SetTag("server.port", (long)ipEndPoint.Port);
                activity.SetTag("network.transport", "tcp");
                break;
            case DnsEndPoint dnsEndPoint:
                activity.SetTag("server.address", dnsEndPoint.Host);
                activity.SetTag("server.port", (long)dnsEndPoint.Port);
                activity.SetTag("network.transport", "tcp");
                break;
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            case UnixDomainSocketEndPoint unixEndPoint:
                activity.SetTag("network.transport", "unix");
                activity.SetTag("server.address", unixEndPoint.ToString());
                break;
#endif
        }

        if (connectionId != null)
        {
            if (connectionId.LongServerValue.HasValue)
            {
                activity.SetTag("db.mongodb.server_connection_id", connectionId.LongServerValue.Value);
            }
            activity.SetTag("db.mongodb.driver_connection_id", connectionId.LongLocalValue);
        }
    }

    private static void SetQueryText(Activity activity, BsonDocument command, int maxLength)
    {
        var commandToLog = FilterSensitiveData(command);
        var commandText = commandToLog.ToJson();

        if (commandText.Length > maxLength)
        {
            commandText = commandText.Substring(0, maxLength);
        }

        activity?.SetTag("db.query.text", commandText);
    }

    private static BsonDocument FilterSensitiveData(BsonDocument command)
    {
        var filtered = new BsonDocument(command);
        filtered.Remove("lsid");
        filtered.Remove("$db");
        filtered.Remove("$clusterTime");
        filtered.Remove("signature");
        return filtered;
    }

    private static string ExtractCollectionName(BsonDocument command)
    {
        if (command == null) return null;

        var firstElement = command.GetElement(0);
        if (firstElement.Value.IsString)
        {
            var value = firstElement.Value.AsString;
            if (value != "1" && value != "admin" && !string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return null;
    }
}
