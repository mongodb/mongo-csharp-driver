/* Copyright 2019-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp2622Tests
    {
        // public methods
        [Fact]
        public void InsertOne_to_oftype_collection_should_generate_id()
        {
            var collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestClass(typeof(CSharp2622Tests));
            var databaseName = collectionNamespace.DatabaseNamespace.DatabaseName;
            var collectionName = collectionNamespace.CollectionName;
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<C>(collectionName);
            var ofTypeCollection = collection.OfType<D>();
            database.DropCollection(collectionName);
            var document = new D { X = 1 };

            ofTypeCollection.InsertOne(document);

            var insertedDocuments = collection.FindSync("{}").ToList();
            insertedDocuments.Count.Should().Be(1);
            insertedDocuments[0].Should().BeOfType<D>();
            var insertedDocument = (D)insertedDocuments[0];
            insertedDocument.Id.Should().NotBe(ObjectId.Empty);
            insertedDocument.X.Should().Be(1);
        }

        // nested types
        public class C
        {
            public ObjectId Id { get; set; }
        }

        public class D : C
        {
            public int X { get; set; }
        }
    }
}
