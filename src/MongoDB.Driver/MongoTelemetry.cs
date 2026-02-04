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
/// Provides access to MongoDB .NET/C# driver's OpenTelemetry instrumentation.
/// </summary>
public static class MongoTelemetry
{
    private static readonly string __driverVersion = ClientDocumentHelper.GetAssemblyVersion(typeof(MongoClient).Assembly);

    /// <summary>
    /// The name of the ActivitySource used by MongoDB driver for OpenTelemetry tracing.
    /// Use this name when configuring OpenTelemetry: <c>.AddSource(MongoTelemetry.ActivitySourceName)</c>
    /// </summary>
    public const string ActivitySourceName = "MongoDB.Driver";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, __driverVersion);

    internal static Activity StartOperationActivity(OperationContext operationContext)
    {
        return operationContext.IsTracingEnabled
            ? StartOperationActivity(operationContext.OperationName, operationContext.DatabaseName, operationContext.CollectionName)
            : null;
    }

    internal static Activity StartOperationActivity(string operationName, string databaseName, string collectionName = null)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            return null;
        }

        // Early return if no listeners to avoid tag construction overhead
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var spanName = GetSpanName(operationName, databaseName, collectionName);

        var tags = new TagList
        {
            { "db.system", "mongodb" },
            { "db.operation.name", operationName },
            { "db.operation.summary", spanName }
        };

        if (!string.IsNullOrEmpty(databaseName))
        {
            tags.Add("db.namespace", databaseName);
        }

        if (!string.IsNullOrEmpty(collectionName))
        {
            tags.Add("db.collection.name", collectionName);
        }

        return ActivitySource.StartActivity(ActivityKind.Client, tags: tags, name: spanName);
    }

    internal static Activity StartTransactionActivity()
    {
        return ActivitySource.StartActivity(ActivityKind.Client, tags: new TagList { { "db.system", "mongodb" } }, name: "transaction");
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
        var collectionName = ExtractCollectionName(command);
        var querySummary = GetSpanName(commandName, databaseNamespace.DatabaseName, collectionName);

        var tags = new TagList
        {
            { "db.system", "mongodb" },
            { "db.command.name", commandName },
            { "db.namespace", databaseNamespace.DatabaseName },
            { "db.query.summary", querySummary }
        };

        if (!string.IsNullOrEmpty(collectionName))
        {
            tags.Add("db.collection.name", collectionName);
        }

        AddConnectionTagsForSampling(ref tags, connectionId);

        var activity = ActivitySource.StartActivity(ActivityKind.Client, tags: tags, name: commandName);

        if (activity?.IsAllDataRequested == true)
        {
            SetAdditionalConnectionTags(activity, connectionId);

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

        return activity;
    }

    internal static void RecordException(Activity activity, Exception exception, bool isOperationLevel = false)
    {
        if (activity == null)
        {
            return;
        }

        // At operation level, skip server exceptions as they're already recorded on command span
        if (isOperationLevel && exception is MongoServerException)
        {
            activity.SetStatus(ActivityStatusCode.Error);
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

    private static void AddConnectionTagsForSampling(ref TagList tags, ConnectionId connectionId)
    {
        var endPoint = connectionId?.ServerId?.EndPoint;
        switch (endPoint)
        {
            case IPEndPoint ipEndPoint:
                tags.Add("server.address", ipEndPoint.Address.ToString());
                tags.Add("server.port", (long)ipEndPoint.Port);
                tags.Add("network.transport", "tcp");
                break;
            case DnsEndPoint dnsEndPoint:
                tags.Add("server.address", dnsEndPoint.Host);
                tags.Add("server.port", (long)dnsEndPoint.Port);
                tags.Add("network.transport", "tcp");
                break;
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            case UnixDomainSocketEndPoint unixEndPoint:
                tags.Add("network.transport", "unix");
                tags.Add("server.address", unixEndPoint.ToString());
                break;
#endif
        }
    }

    private static void SetAdditionalConnectionTags(Activity activity, ConnectionId connectionId)
    {
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