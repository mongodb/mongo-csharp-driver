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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class InsertOperation : WriteOperation
    {
        private readonly bool _assignIdOnInsert;
        private readonly bool _checkElementNames;
        private readonly Type _documentType;
        private readonly IEnumerable _documents;
        private readonly InsertFlags _flags;
        private readonly object _idGeneratorContainer;

        public InsertOperation(
            string databaseName,
            string collectionName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            WriteConcern writeConcern,
            bool assignIdOnInsert,
            bool checkElementNames,
            Type documentType,
            IEnumerable documents,
            InsertFlags flags,
            object idGeneratorContainer)
            : base(databaseName, collectionName, readerSettings, writerSettings, writeConcern)
        {
            _assignIdOnInsert = assignIdOnInsert;
            _checkElementNames = checkElementNames;
            _documentType = documentType;
            _documents = documents;
            _flags = flags;
            _idGeneratorContainer = idGeneratorContainer;
        }

        public IEnumerable<WriteConcernResult> Execute(MongoConnection connection)
        {
            WriteConcernException finalException = null;
            List<WriteConcernResult> results = (WriteConcern.Enabled) ? new List<WriteConcernResult>() : null;

            using (var bsonBuffer = new BsonBuffer(new MultiChunkBuffer(BsonChunkPool.Default), true))
            {
                var readerSettings = GetNodeAdjustedReaderSettings(connection.ServerInstance);
                var writerSettings = GetNodeAdjustedWriterSettings(connection.ServerInstance);
                var message = new MongoInsertMessage(writerSettings, CollectionFullName, _checkElementNames, _flags);
                message.WriteToBuffer(bsonBuffer); // must be called before AddDocument

                var writeConcernEnabled = WriteConcern.Enabled;
                var continueOnError = (_flags & InsertFlags.ContinueOnError) != 0;

                foreach (var document in _documents)
                {
                    if (document == null)
                    {
                        throw new ArgumentException("Batch contains one or more null documents.");
                    }

                    if (_assignIdOnInsert)
                    {
                        var serializer = BsonSerializer.LookupSerializer(document.GetType());
                        var idProvider = serializer as IBsonIdProvider;
                        if (idProvider != null)
                        {
                            object id;
                            Type idNominalType;
                            IIdGenerator idGenerator;
                            if (idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator))
                            {
                                if (idGenerator != null && idGenerator.IsEmpty(id))
                                {
                                    id = idGenerator.GenerateId(_idGeneratorContainer, document);
                                    idProvider.SetDocumentId(document, id);
                                }
                            }
                        }
                    }
                    message.AddDocument(bsonBuffer, _documentType, document);

                    if (message.MessageLength > connection.ServerInstance.MaxMessageLength)
                    {
                        byte[] lastDocument = message.RemoveLastDocument(bsonBuffer);

                        if (writeConcernEnabled && !continueOnError)
                        {
                            try
                            {
                                var result = SendMessageWithWriteConcern(connection, bsonBuffer, message.RequestId, readerSettings, writerSettings, WriteConcern);
                                results.Add(result);
                            }
                            catch (WriteConcernException ex)
                            {
                                results.Add((WriteConcernResult)ex.CommandResult);
                                ex.Data["results"] = results;
                                throw ex;
                            }
                        }
                        else if (writeConcernEnabled && continueOnError)
                        {
                            try
                            {
                                var result = SendMessageWithWriteConcern(connection, bsonBuffer, message.RequestId, readerSettings, writerSettings, WriteConcern);
                                results.Add(result);
                            }
                            catch (WriteConcernException ex)
                            {
                                finalException = ex;
                                results.Add((WriteConcernResult)ex.CommandResult);
                            }
                        }
                        else if (!writeConcernEnabled && !continueOnError)
                        {
                            try
                            {
                                SendMessageWithWriteConcern(connection, bsonBuffer, message.RequestId, readerSettings, writerSettings, WriteConcern.Acknowledged);
                            }
                            catch (WriteConcernException)
                            {
                                return null;
                            }
                        }
                        else if (!writeConcernEnabled && continueOnError)
                        {
                            SendMessageWithWriteConcern(connection, bsonBuffer, message.RequestId, readerSettings, writerSettings, WriteConcern.Unacknowledged);
                        }

                        message.ResetBatch(bsonBuffer, lastDocument);
                    }
                }

                if (writeConcernEnabled)
                {
                    try
                    {
                        var result = SendMessageWithWriteConcern(connection, bsonBuffer, message.RequestId, readerSettings, writerSettings, WriteConcern);
                        results.Add(result);
                    }
                    catch (WriteConcernException ex)
                    {
                        finalException = ex;
                        results.Add((WriteConcernResult)ex.CommandResult);
                    }

                    if (finalException != null)
                    {
                        finalException.Data["results"] = results;
                        throw finalException;
                    }

                    return results;
                }
                else
                {
                    SendMessageWithWriteConcern(connection, bsonBuffer, message.RequestId, readerSettings, writerSettings, WriteConcern.Unacknowledged);
                    return null;
                }
            }
        }
    }
}
