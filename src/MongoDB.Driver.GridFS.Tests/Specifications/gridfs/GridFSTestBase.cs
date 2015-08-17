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
using System.Threading.Tasks;
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

        public async Task RunAsync(GridFSBucket bucket)
        {
            await ArrangeAsync(bucket);
            await ActAsync(bucket);
            await AssertAsync(bucket);
        }

        // protected methods
        protected abstract Task ActAsync(GridFSBucket bucket);

        protected virtual async Task ArrangeAsync(GridFSBucket bucket)
        {
            await InitializeFilesCollectionAsync(bucket);
            await InitializeChunksCollectionAsync(bucket);
            await RunArrangeCommandsAsync(bucket);
        }

        protected virtual void Assert(List<BsonDocument> filesCollectionDocuments, List<BsonDocument> chunks, List<BsonDocument> expectedFilesCollectionDocuments, List<BsonDocument> expectedChunks)
        {
            filesCollectionDocuments.Should().BeEquivalentTo(expectedFilesCollectionDocuments);
            chunks.Should().BeEquivalentTo(expectedChunks);
        }

        protected virtual async Task AssertAsync(GridFSBucket bucket)
        {
            var actualFilesCollectionDocuments = await GetActualFilesCollectionDocumentsAsync(bucket);
            var actualChunks = await GetActualChunksAsync(bucket);

            await InitializeExpectedFilesCollectionAsync(bucket);
            await InitializeExpectedChunksCollectionAsync(bucket);
            await RunAssertCommandsAsync(bucket, actualFilesCollectionDocuments, actualChunks);

            var expectedFilesCollectionDocuments = await GetExpectedFilesCollectionDocumentsAsync(bucket);
            var expectedChunks = await GetExpectedChunksAsync(bucket);

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
        private Task<List<BsonDocument>> GetActualChunksAsync(GridFSBucket bucket)
        {
            var chunksCollection = GetChunksCollection(bucket);
            return GetCollectionDocumentsAsync(chunksCollection);
        }

        private Task<List<BsonDocument>> GetActualFilesCollectionDocumentsAsync(GridFSBucket bucket)
        {
            var filesCollection = GetFilesCollection(bucket);
            return GetCollectionDocumentsAsync(filesCollection);
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

        private Task<List<BsonDocument>> GetCollectionDocumentsAsync(IMongoCollection<BsonDocument> collection)
        {
            return collection.Find(new BsonDocument()).ToListAsync();
        }

        private IMongoCollection<BsonDocument> GetChunksCollection(GridFSBucket bucket)
        {
            var database = bucket.Database;
            var chunksCollectionName = bucket.Options.BucketName + ".chunks";
            return database.GetCollection<BsonDocument>(chunksCollectionName);
        }

        private Task<List<BsonDocument>> GetExpectedChunksAsync(GridFSBucket bucket)
        {
            var expectedChunksCollection = GetExpectedChunksCollection(bucket);
            return GetCollectionDocumentsAsync(expectedChunksCollection);
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

        private Task<List<BsonDocument>> GetExpectedFilesCollectionDocumentsAsync(GridFSBucket bucket)
        {
            var expectedFilesCollection = GetExpectedFilesCollection(bucket);
            return GetCollectionDocumentsAsync(expectedFilesCollection);
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

        private async Task InitializeChunksCollectionAsync(GridFSBucket bucket)
        {
            var chunksCollection = GetChunksCollection(bucket);
            await InitializeChunksCollectionAsync(chunksCollection);
        }

        private async Task InitializeChunksCollectionAsync(IMongoCollection<BsonDocument> chunksCollection)
        {
            var chunks = GetInitialChunks();
            await InitializeCollectionAsync(chunksCollection, chunks);
        }

        private async Task InitializeCollectionAsync(IMongoCollection<BsonDocument> collection, List<BsonDocument> documents)
        {
            await collection.DeleteManyAsync(new BsonDocument());
            if (documents.Count > 0)
            {
                await collection.InsertManyAsync(documents);
            }
        }

        private async Task InitializeExpectedChunksCollectionAsync(GridFSBucket bucket)
        {
            var expectedChunksCollection = GetExpectedChunksCollection(bucket);
            await InitializeChunksCollectionAsync(expectedChunksCollection);
        }

        private async Task InitializeExpectedFilesCollectionAsync(GridFSBucket bucket)
        {
            var expectedFilesCollection = GetExpectedFilesCollection(bucket);
            await InitializeFilesCollectionAsync(expectedFilesCollection);
        }

        private async Task InitializeFilesCollectionAsync(GridFSBucket bucket)
        {
            var filesCollection = GetFilesCollection(bucket);
            await InitializeFilesCollectionAsync(filesCollection);
        }

        private async Task InitializeFilesCollectionAsync(IMongoCollection<BsonDocument> filesCollection)
        {
            var filesCollectionDocuments = GetInitialFilesCollectionDocuments();
            await InitializeCollectionAsync(filesCollection, filesCollectionDocuments);
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

        private async Task RunArrangeCommandsAsync(GridFSBucket bucket)
        {
            var commands = GetArrangeCommands();
            await RunCommandsAsync(bucket.Database, commands);
        }

        private async Task RunAssertCommandsAsync(GridFSBucket bucket, List<BsonDocument> actualFilesCollectionDocuments, List<BsonDocument> actualChunks)
        {
            var commands = GetAssertCommands();
            PreprocessAssertCommands(commands, actualFilesCollectionDocuments, actualChunks);
            await RunCommandsAsync(bucket.Database, commands);
        }

        private async Task RunCommandsAsync(IMongoDatabase database, IEnumerable<BsonDocument> commands)
        {
            foreach (var command in commands)
            {
                await database.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(command));
            }
        }
    }
}
