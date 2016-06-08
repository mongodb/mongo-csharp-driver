/* Copyright 2015 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS.Tests.Specifications.gridfs
{
    public abstract class GridFSTestBase : IGridFSTest
    {
        // fields
        private BsonDocument _data;
        private BsonDocument _testDefinition;

        // constructors
        protected GridFSTestBase(BsonDocument data, BsonDocument testDefinition)
        {
            _data = data;
            _testDefinition = testDefinition;
        }

        // public methods
        public virtual bool CanRun(out string reason)
        {
            reason = null;
            return true;
        }

        public void Run(GridFSBucket bucket, bool async)
        {
            Arrange(bucket);
            Act(bucket, async);
            Assert(bucket);
        }

        // protected methods
        protected abstract void Act(GridFSBucket bucket, bool async);

        protected virtual void Arrange(GridFSBucket bucket)
        {
            InitializeFilesCollection(bucket);
            InitializeChunksCollection(bucket);
            RunArrangeCommands(bucket);
        }

        protected virtual void Assert(List<BsonDocument> filesCollectionDocuments, List<BsonDocument> chunks, List<BsonDocument> expectedFilesCollectionDocuments, List<BsonDocument> expectedChunks)
        {
            filesCollectionDocuments.Should().BeEquivalentTo(expectedFilesCollectionDocuments);
            chunks.Should().BeEquivalentTo(expectedChunks);
        }

        protected virtual void Assert(GridFSBucket bucket)
        {
            var actualFilesCollectionDocuments = GetActualFilesCollectionDocuments(bucket);
            var actualChunks = GetActualChunks(bucket);

            InitializeExpectedFilesCollection(bucket);
            InitializeExpectedChunksCollection(bucket);
            RunAssertCommands(bucket, actualFilesCollectionDocuments, actualChunks);

            var expectedFilesCollectionDocuments = GetExpectedFilesCollectionDocuments(bucket);
            var expectedChunks = GetExpectedChunks(bucket);

            Assert(actualFilesCollectionDocuments, actualChunks, expectedFilesCollectionDocuments, expectedChunks);
        }

        protected virtual void PreprocessAssertCommands(List<BsonDocument> commands, List<BsonDocument> actualFilesCollectionDocuments, List<BsonDocument> actualChunks)
        {
            PreprocessAssertCommands(commands, actualFilesCollectionDocuments, actualChunks, result: null);
        }

        protected virtual void PreprocessAssertCommands(List<BsonDocument> commands, List<BsonDocument> actualFilesCollectionDocuments, List<BsonDocument> actualChunks, BsonValue result)
        {
            foreach (var command in commands)
            {
                switch (command[0].AsString)
                {
                    case "expected.files": PreprocessFilesCollectionAssertCommand(command, result, actualFilesCollectionDocuments); break;
                    case "expected.chunks": PreprocessChunksCollectionAssertCommand(command, result, actualChunks); break;
                }
            }
        }

        // private methods
        private List<BsonDocument> GetActualChunks(GridFSBucket bucket)
        {
            var chunksCollection = GetChunksCollection(bucket);
            return GetCollectionDocuments(chunksCollection);
        }

        private List<BsonDocument> GetActualFilesCollectionDocuments(GridFSBucket bucket)
        {
            var filesCollection = GetFilesCollection(bucket);
            return GetCollectionDocuments(filesCollection);
        }

        private List<BsonDocument> GetArrangeCommands()
        {
            if (_testDefinition.Contains("arrange"))
            {
                var arrange = _testDefinition["arrange"].AsBsonDocument;
                if (arrange.Contains("data"))
                {
                    return arrange["data"].AsBsonArray.Select(v => v.AsBsonDocument.DeepClone().AsBsonDocument).ToList();
                }
            }

            return new List<BsonDocument>();
        }

        private List<BsonDocument> GetAssertCommands()
        {
            if (_testDefinition.Contains("assert"))
            {
                var assert = _testDefinition["assert"].AsBsonDocument;
                if (assert.Contains("data"))
                {
                    return assert["data"].AsBsonArray.Select(v => v.AsBsonDocument.DeepClone().AsBsonDocument).ToList();
                }
            }

            return new List<BsonDocument>();
        }

        private List<BsonDocument> GetCollectionDocuments(IMongoCollection<BsonDocument> collection)
        {
            return collection.FindSync(new BsonDocument()).ToList();
        }

        private IMongoCollection<BsonDocument> GetChunksCollection(GridFSBucket bucket)
        {
            var database = bucket.Database;
            var chunksCollectionName = bucket.Options.BucketName + ".chunks";
            return database.GetCollection<BsonDocument>(chunksCollectionName);
        }

        private List<BsonDocument> GetExpectedChunks(GridFSBucket bucket)
        {
            var expectedChunksCollection = GetExpectedChunksCollection(bucket);
            return GetCollectionDocuments(expectedChunksCollection);
        }

        private IMongoCollection<BsonDocument> GetExpectedChunksCollection(GridFSBucket bucket)
        {
            var database = bucket.Database;
            var collectionName = "expected.chunks";
            return database.GetCollection<BsonDocument>(collectionName);
        }

        private IMongoCollection<BsonDocument> GetExpectedFilesCollection(GridFSBucket bucket)
        {
            var database = bucket.Database;
            var collectionName = "expected.files";
            return database.GetCollection<BsonDocument>(collectionName);
        }

        private List<BsonDocument> GetExpectedFilesCollectionDocuments(GridFSBucket bucket)
        {
            var expectedFilesCollection = GetExpectedFilesCollection(bucket);
            return GetCollectionDocuments(expectedFilesCollection);
        }

        private IMongoCollection<BsonDocument> GetFilesCollection(GridFSBucket bucket)
        {
            var database = bucket.Database;
            var filesCollectionName = bucket.Options.BucketName + ".files";
            return database.GetCollection<BsonDocument>(filesCollectionName);
        }

        private List<BsonDocument> GetInitialFilesCollectionDocuments()
        {
            return _data["files"].AsBsonArray
                .Select(d => d.DeepClone())
                .Cast<BsonDocument>()
                .ToList();
        }

        private List<BsonDocument> GetInitialChunks()
        {
            return _data["chunks"].AsBsonArray
                .Select(d => d.DeepClone())
                .Cast<BsonDocument>()
                .ToList();
        }

        private void InitializeChunksCollection(GridFSBucket bucket)
        {
            var chunksCollection = GetChunksCollection(bucket);
            InitializeChunksCollection(chunksCollection);
        }

        private void InitializeChunksCollection(IMongoCollection<BsonDocument> chunksCollection)
        {
            var chunks = GetInitialChunks();
            InitializeCollection(chunksCollection, chunks);
        }

        private void InitializeCollection(IMongoCollection<BsonDocument> collection, List<BsonDocument> documents)
        {
            collection.DeleteMany(new BsonDocument());
            if (documents.Count > 0)
            {
                collection.InsertMany(documents);
            }
        }

        private void InitializeExpectedChunksCollection(GridFSBucket bucket)
        {
            var expectedChunksCollection = GetExpectedChunksCollection(bucket);
            InitializeChunksCollection(expectedChunksCollection);
        }

        private void InitializeExpectedFilesCollection(GridFSBucket bucket)
        {
            var expectedFilesCollection = GetExpectedFilesCollection(bucket);
            InitializeFilesCollection(expectedFilesCollection);
        }

        private void InitializeFilesCollection(GridFSBucket bucket)
        {
            var filesCollection = GetFilesCollection(bucket);
            InitializeFilesCollection(filesCollection);
        }

        private void InitializeFilesCollection(IMongoCollection<BsonDocument> filesCollection)
        {
            var filesCollectionDocuments = GetInitialFilesCollectionDocuments();
            InitializeCollection(filesCollection, filesCollectionDocuments);
        }

        private void PreprocessChunksCollectionAssertCommand(BsonDocument command, BsonValue result, List<BsonDocument> actualFilesCollectionDocuments)
        {
            switch (command.GetElement(0).Name)
            {
                case "insert": PreprocessChunksCollectionInsertCommandCommand(command, result, actualFilesCollectionDocuments); break;
            }
        }

        private void PreprocessChunksCollectionInsertCommandCommand(BsonDocument command, BsonValue result, List<BsonDocument> actualChunksCollectionDocuments)
        {
            foreach (BsonDocument document in command["documents"].AsBsonArray)
            {
                ReplaceResult(document, result);
                var filesId = document["files_id"];
                var n = document["n"];
                var actual = actualChunksCollectionDocuments.Single(d => d["files_id"] == filesId && d["n"] == n);
                ReplaceActual(document, actual);
            }
        }

        private void PreprocessFilesCollectionAssertCommand(BsonDocument command, BsonValue result, List<BsonDocument> actualFilesCollectionDocuments)
        {
            switch (command.GetElement(0).Name)
            {
                case "insert": PreprocessFilesCollectionInsertCommandCommand(command, result, actualFilesCollectionDocuments); break;
            }
        }

        private void PreprocessFilesCollectionInsertCommandCommand(BsonDocument command, BsonValue result, List<BsonDocument> actualFilesCollectionDocuments)
        {
            foreach (BsonDocument document in command["documents"].AsBsonArray)
            {
                ReplaceResult(document, result);
                var id = document["_id"];
                var actual = actualFilesCollectionDocuments.Single(d => d["_id"] == id);
                ReplaceActual(document, actual);
            }
        }

        private void ReplaceActual(BsonDocument document, BsonDocument actual)
        {
            foreach (var element in document.ToList())
            {
                var value = element.Value;

                if (value.IsString && value.AsString == "*actual")
                {
                    document[element.Name] = actual[element.Name];
                }
            }
        }

        private void ReplaceResult(BsonDocument document, BsonValue result)
        {
            foreach (var element in document.ToList())
            {
                var value = element.Value;

                if (value.IsString && value.AsString == "*result")
                {
                    document[element.Name] = result;
                }
            }
        }

        private void RunArrangeCommands(GridFSBucket bucket)
        {
            var commands = GetArrangeCommands();
            RunCommands(bucket, commands);
        }

        private void RunAssertCommands(GridFSBucket bucket, List<BsonDocument> actualFilesCollectionDocuments, List<BsonDocument> actualChunks)
        {
            var commands = GetAssertCommands();
            PreprocessAssertCommands(commands, actualFilesCollectionDocuments, actualChunks);
            RunCommands(bucket, commands);
        }

        private void RunCommand(GridFSBucket bucket, BsonDocument command)
        {
            var commandName = command.Names.First();
            switch (commandName)
            {
                case "delete": RunDeleteCommand(bucket, command); break;
                case "insert": RunInsertCommand(bucket, command); break;
                case "update": RunUpdateCommand(bucket, command); break;
                default: throw new ArgumentException("Unexpected command.");
            }
        }

        private void RunCommands(GridFSBucket bucket, IEnumerable<BsonDocument> commands)
        {
            foreach (var command in commands)
            {
                RunCommand(bucket, command);
            }
        }

        private void RunDeleteCommand(GridFSBucket bucket, BsonDocument command)
        {
            var collectionName = command["delete"].AsString;
            var collection = bucket.Database.GetCollection<BsonDocument>(collectionName);
            var requests = new List<WriteModel<BsonDocument>>();
            foreach (BsonDocument deleteStatement in command["deletes"].AsBsonArray)
            {
                var filter = deleteStatement["q"].AsBsonDocument;
                var limit = deleteStatement["limit"].ToInt32();
                WriteModel<BsonDocument> request;
                if (limit == 1)
                {
                    request = new DeleteOneModel<BsonDocument>(filter);
                }
                else
                {
                    request = new DeleteManyModel<BsonDocument>(filter);
                }
                requests.Add(request);
            }
            collection.BulkWrite(requests);
        }

        private void RunInsertCommand(GridFSBucket bucket, BsonDocument command)
        {
            var collectionName = command["insert"].AsString;
            var collection = bucket.Database.GetCollection<BsonDocument>(collectionName);
            var requests = new List<WriteModel<BsonDocument>>();
            foreach (BsonDocument document in command["documents"].AsBsonArray)
            {

                var request = new InsertOneModel<BsonDocument>(document);
                requests.Add(request);
            }
            collection.BulkWrite(requests);
        }

        private void RunUpdateCommand(GridFSBucket bucket, BsonDocument command)
        {
            var collectionName = command["update"].AsString;
            var collection = bucket.Database.GetCollection<BsonDocument>(collectionName);
            var requests = new List<WriteModel<BsonDocument>>();
            foreach (BsonDocument updateStatement in command["updates"].AsBsonArray)
            {
                var filter = updateStatement["q"].AsBsonDocument;
                var update = updateStatement["u"].AsBsonDocument;
                var upsert = updateStatement.GetValue("upsert", false).ToBoolean();
                var multi = updateStatement.GetValue("multi", false).ToBoolean();
                WriteModel<BsonDocument> request;
                if (multi)
                {
                    request = new UpdateManyModel<BsonDocument>(filter, update) { IsUpsert = upsert };
                }
                else
                {
                    request = new UpdateOneModel<BsonDocument>(filter, update) { IsUpsert = upsert };
                }
                requests.Add(request);
            }
            collection.BulkWrite(requests);
        }
    }
}
