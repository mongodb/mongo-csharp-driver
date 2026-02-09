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

    // OpenTelemetry semantic convention attribute names
    private const string DbSystemAttribute = "db.system";
    private const string DbOperationNameAttribute = "db.operation.name";
    private const string DbOperationSummaryAttribute = "db.operation.summary";
    private const string DbCommandNameAttribute = "db.command.name";
    private const string DbNamespaceAttribute = "db.namespace";
    private const string DbCollectionNameAttribute = "db.collection.name";
    private const string DbQuerySummaryAttribute = "db.query.summary";
    private const string DbQueryTextAttribute = "db.query.text";
    private const string ServerAddressAttribute = "server.address";
    private const string ServerPortAttribute = "server.port";
    private const string NetworkTransportAttribute = "network.transport";
    private const string DbMongoDbLsidAttribute = "db.mongodb.lsid";
    private const string DbMongoDbTxnNumberAttribute = "db.mongodb.txn_number";
    private const string DbMongoDbServerConnectionIdAttribute = "db.mongodb.server_connection_id";
    private const string DbMongoDbDriverConnectionIdAttribute = "db.mongodb.driver_connection_id";
    private const string DbMongoDbCursorIdAttribute = "db.mongodb.cursor_id";
    private const string ExceptionTypeAttribute = "exception.type";
    private const string ExceptionMessageAttribute = "exception.message";
    private const string ExceptionStacktraceAttribute = "exception.stacktrace";
    private const string DbResponseStatusCodeAttribute = "db.response.status_code";

    /// <summary>
    /// The name of the ActivitySource used by MongoDB driver for OpenTelemetry tracing.
    /// Use this name when configuring OpenTelemetry: <c>.AddSource(MongoTelemetry.ActivitySourceName)</c>
    /// </summary>
    public const string ActivitySourceName = "MongoDB.Driver";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, __driverVersion);

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
            { DbSystemAttribute, "mongodb" },
            { DbCommandNameAttribute, commandName },
            { DbNamespaceAttribute, databaseNamespace.DatabaseName },
            { DbQuerySummaryAttribute, querySummary }
        };

        if (!string.IsNullOrEmpty(collectionName))
        {
            tags.Add(DbCollectionNameAttribute, collectionName);
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
                activity.SetTag(DbMongoDbLsidAttribute, materializedLsid);
            }

            if (command.TryGetValue("txnNumber", out var txnNumber))
            {
                activity.SetTag(DbMongoDbTxnNumberAttribute, txnNumber.ToInt64());
            }

            if (queryTextMaxLength > 0)
            {
                SetQueryText(activity, command, queryTextMaxLength);
            }
        }

        return activity;
    }

    internal static void CompleteCommandActivity(Activity activity, BsonDocument reply)
    {
        if (activity == null)
        {
            return;
        }

        if (TryGetCursorId(reply, out var cursorId))
        {
            activity.SetTag(DbMongoDbCursorIdAttribute, cursorId);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

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
            { DbSystemAttribute, "mongodb" },
            { DbOperationNameAttribute, operationName },
            { DbOperationSummaryAttribute, spanName }
        };

        if (!string.IsNullOrEmpty(databaseName))
        {
            tags.Add(DbNamespaceAttribute, databaseName);
        }

        if (!string.IsNullOrEmpty(collectionName))
        {
            tags.Add(DbCollectionNameAttribute, collectionName);
        }

        return ActivitySource.StartActivity(ActivityKind.Client, tags: tags, name: spanName);
    }

    internal static Activity StartTransactionActivity()
    {
        return ActivitySource.StartActivity(ActivityKind.Client, tags: new TagList { { DbSystemAttribute, "mongodb" } }, name: "transaction");
    }

    internal static void RecordException(Activity activity, Exception exception, bool isOperationLevel = false)
    {
        if (activity == null)
        {
            return;
        }

        // At operation level, skip exceptions already recorded on the command span
        if (isOperationLevel && exception is (MongoCommandException and not MongoWriteConcernException) or MongoExecutionTimeoutException)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            return;
        }

        activity.SetTag(ExceptionTypeAttribute, exception.GetType().FullName);
        activity.SetTag(ExceptionMessageAttribute, exception.Message);
        if (exception.StackTrace != null)
        {
            activity.SetTag(ExceptionStacktraceAttribute, exception.StackTrace);
        }

        if (!isOperationLevel)
        {
            var code = exception switch
            {
                MongoCommandException cmd => cmd.Code,
                MongoExecutionTimeoutException timeout => timeout.Code,
                _ => -1
            };

            if (code != -1)
            {
                activity.SetTag(DbResponseStatusCodeAttribute, code.ToString());
            }
        }

        activity.SetStatus(ActivityStatusCode.Error);
    }

    private static void AddConnectionTagsForSampling(ref TagList tags, ConnectionId connectionId)
    {
        var endPoint = connectionId?.ServerId?.EndPoint;
        switch (endPoint)
        {
            case IPEndPoint ipEndPoint:
                tags.Add(ServerAddressAttribute, ipEndPoint.Address.ToString());
                tags.Add(ServerPortAttribute, (long)ipEndPoint.Port);
                tags.Add(NetworkTransportAttribute, "tcp");
                break;
            case DnsEndPoint dnsEndPoint:
                tags.Add(ServerAddressAttribute, dnsEndPoint.Host);
                tags.Add(ServerPortAttribute, (long)dnsEndPoint.Port);
                tags.Add(NetworkTransportAttribute, "tcp");
                break;
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            case UnixDomainSocketEndPoint unixEndPoint:
                tags.Add(NetworkTransportAttribute, "unix");
                tags.Add(ServerAddressAttribute, unixEndPoint.ToString());
                break;
#endif
        }
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

    private static BsonDocument FilterSensitiveData(BsonDocument command)
    {
        var filtered = new BsonDocument(command);
        filtered.Remove("lsid");
        filtered.Remove("$db");
        filtered.Remove("$clusterTime");
        filtered.Remove("signature");
        return filtered;
    }

    private static string GetSpanName(string name, string databaseName, string collectionName)
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

    private static void SetAdditionalConnectionTags(Activity activity, ConnectionId connectionId)
    {
        if (connectionId != null)
        {
            if (connectionId.LongServerValue.HasValue)
            {
                activity.SetTag(DbMongoDbServerConnectionIdAttribute, connectionId.LongServerValue.Value);
            }
            activity.SetTag(DbMongoDbDriverConnectionIdAttribute, connectionId.LongLocalValue);
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

        activity.SetTag(DbQueryTextAttribute, commandText);
    }

    private static bool TryGetCursorId(BsonDocument reply, out long cursorId)
    {
        cursorId = 0;
        if (reply == null) return false;

        if (reply.TryGetValue("cursor", out var cursorValue) && cursorValue.IsBsonDocument)
        {
            var cursorDoc = cursorValue.AsBsonDocument;
            if (cursorDoc.TryGetValue("id", out var idValue) && idValue.IsInt64)
            {
                cursorId = idValue.AsInt64;
                return cursorId != 0;
            }
        }

        return false;
    }
}