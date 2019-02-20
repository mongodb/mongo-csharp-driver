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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class UseExistingInstanceSerializerTests
    {
        [Fact]
        public void TestSetExistingInstanceAttributeOnValueTypePropertyThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => BsonClassMap.RegisterClassMap<TestValueTypeWithAttribute>(map => map.AutoMap()));
        }

        [Fact]
        public void TestManuallyConfiguringSetExistingInstanceOnValueTypePropertyThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => BsonClassMap.RegisterClassMap<TestValueTypeWithoutAttribute>(map => map.MapField(t => t.ValueType).SetUseExistingInstance(true)));
        }

        [Fact]
        public void TestUsingSetExistingInstanceConventionOnValueTypePropertyDoesNotThrowException()
        {
            ConventionRegistry.Register("Global use existing instance convention", new ConventionPack { new UseExistingInstanceConvention(true) }, t => t == typeof(TestValueTypeWithoutAttribute));
            BsonClassMap.RegisterClassMap<TestValueTypeWithoutAttribute>();
        }

        [Fact]
        public void TestDeserializationWithExistingBsonDocument()
        {
            var existingDocument = new BsonDocument { { "existingValue", new BsonInt32(1) } };
            var documentToDeserialize = new BsonDocument { { "newValue", new BsonInt32(2) } };

            var expectedDocument = new BsonDocument
            {
                { "existingValue", new BsonInt32(1) },
                { "newValue", new BsonInt32(2) }
            };

            using (var bsonReader = new BsonDocumentReader(documentToDeserialize))
            {
                var rehydrated = Deserialize(bsonReader, existingDocument);
                Assert.True(expectedDocument.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestDeserializationWithExistingBsonDocumentAndSameFields()
        {
            var existingDocument = new BsonDocument { { "field", new BsonInt32(1) } };
            var documentToDeserialize = new BsonDocument { { "field", new BsonInt32(2) } };

            var expectedDocument = new BsonDocument
            {
                { "field", new BsonInt32(2) }
            };

            using (var bsonReader = new BsonDocumentReader(documentToDeserialize))
            {
                var rehydrated = Deserialize(bsonReader, existingDocument);
                Assert.True(expectedDocument.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestDeserializationWithExistingEmptyBsonDocument()
        {
            var document = new BsonDocument { { "newValue", new BsonInt32(2) } };

            var expectedDocument = new BsonDocument
            {
                { "newValue", new BsonInt32(2) }
            };

            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = Deserialize(bsonReader, new BsonDocument());
                Assert.True(expectedDocument.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestDeserializationWithoutExistingBsonDocument()
        {
            var document = new BsonDocument { { "newValue", new BsonInt32(2) } };

            var expectedDocument = new BsonDocument
            {
                { "newValue", new BsonInt32(2) }
            };

            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = Deserialize(bsonReader, (BsonDocument)null);
                Assert.True(expectedDocument.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestExistingObjectWithReadOnlyCollection()
        {
            Assert.Throws<FormatException>(() => TestExistingObjectWithCollectionOfType<ReadOnlyCollection<int>, int>(() => new List<int>(new[] { 0, 1, 2 }).AsReadOnly(), new List<int> { 3, 4 }.AsReadOnly()));
        }

        [Fact]
        public void TestExistingObjectWithList()
        {
            TestExistingObjectWithCollectionOfType<List<int>, int>(() => new List<int>(new[] { 0, 1, 2 }), new List<int>(new[] { 3, 4 }));
            TestExistingObjectWithCollectionOfType<List<int>, int>(() => (List<int>)null, new List<int>(new[] { 3, 4 }));
        }

        [Fact]
        public void TestExistingObjectWithStack()
        {
            TestExistingObjectWithCollectionOfType<Stack<int>, int>(() => new Stack<int>(new[] { 0, 1, 2 }), new Stack<int>(new[] { 3, 4 }), expectResultsReversed: true);
            TestExistingObjectWithCollectionOfType<Stack<int>, int>(() => null as Stack<int>, new Stack<int>(new[] { 3, 4 }), expectResultsReversed: true);
        }

        [Fact]
        public void TestExistingObjectWithQueue()
        {
            TestExistingObjectWithCollectionOfType<Queue<int>, int>(() => new Queue<int>(new[] { 0, 1, 2 }), new Queue<int>(new[] { 3, 4 }));
            TestExistingObjectWithCollectionOfType<Queue<int>, int>(() => null as Queue<int>, new Queue<int>(new[] { 3, 4 }));
        }

        [Fact]
        public void TestExistingObjectWithArray()
        {
            TestExistingObjectWithCollectionOfType<int[], int>(() => new[] { 0, 1, 2 }, new[] { 3, 4 }, expectExcessItemsToBeTrimmed: false);
            TestExistingObjectWithCollectionOfType<int[], int>(() => null, new[] { 3, 4 }, expectExcessItemsToBeTrimmed: false);
        }

        [Fact]
        public void TestExistingObjectWithDictionary()
        {
            TestExistingObjectWithCollectionOfType<Dictionary<string, int>, KeyValuePair<string, int>>(() => new Dictionary<string, int> { { "0", 0 }, { "1", 1 }, { "2", 2 } }, new Dictionary<string, int> { { "3", 3 }, { "4", 4 } });
            TestExistingObjectWithCollectionOfType<Dictionary<string, int>, KeyValuePair<string, int>>(() => null, new Dictionary<string, int> { { "3", 3 }, { "4", 4 } });
        }

        [Fact]
        public void TestExistingObjectWithArrayAndTooMuchDeserializedData()
        {
            Assert.Throws<FormatException>(() => TestExistingObjectWithCollectionOfType<int[], int>(() => new int[] { 0, 1, 2 }, new int[] { 3, 4, 5, 6 }));
        }

        private void TestExistingObjectWithCollectionOfType<TCollection, TValue>(Func<TCollection> collectionFactory, TCollection valuesToBeDeserialized, bool expectExcessItemsToBeTrimmed = true, bool expectResultsReversed = false) where TCollection : IEnumerable<TValue>
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Test<TCollection>)))
            {
                BsonClassMap.RegisterClassMap<Test<TCollection>>(cm =>
                {
                    cm.AutoMap();
                    // map all readonly properties
                    cm.MapProperty(c => c.UseExistingInstanceReadOnlyCollection);
                    cm.MapProperty(c => c.DoNotUseExistingInstanceReadOnlyCollection);
                    cm.MapProperty(c => c.UseExistingInstanceReadOnlyInnerInstance);
                    cm.MapProperty(c => c.DoNotUseExistingInstanceReadOnlyInnerInstance);
                });
            }

            Test<TCollection>.CollectionFactory = () => valuesToBeDeserialized;
            Test<TCollection> test = new Test<TCollection>();

            var bsonDocument = test.ToBsonDocument();

            // 1. first test without a top level document
            Test<TCollection> doc = null;
            // hack which allows us to have the deserializer call the default constructor and still populate the collections
            Test<TCollection>.CollectionFactory = collectionFactory;
            TestUsingTopLevelInstance<TCollection, TValue>(collectionFactory, valuesToBeDeserialized, expectExcessItemsToBeTrimmed, bsonDocument, doc);

            // 2. then test with an existing top level instance
            doc = new Test<TCollection>(true);

            // we capture the current properties locally in order to check for instance identity after deserialization
            var useExistingInstanceReadOnlyCollection = doc.UseExistingInstanceReadOnlyCollection;
            var doNotUseExistingInstanceReadOnlyCollection = doc.DoNotUseExistingInstanceReadOnlyCollection;
            var useExistingInstanceWritableCollection = doc.UseExistingInstanceWritableCollection;
            var doNotUseExistingInstanceWritableCollection = doc.DoNotUseExistingInstanceWritableCollection;
            var useExistingInstanceReadOnlyInnerInstance = doc.UseExistingInstanceReadOnlyInnerInstance;
            var doNotUseExistingInstanceReadOnlyInnerInstance = doc.DoNotUseExistingInstanceReadOnlyInnerInstance;
            var useExistingInstanceWritableInnerInstance = doc.UseExistingInstanceWritableInnerInstance;
            var doNotUseExistingInstanceWritableInnerInstance = doc.DoNotUseExistingInstanceWritableInnerInstance;

            var rehydrated = TestUsingTopLevelInstance<TCollection, TValue>(collectionFactory, valuesToBeDeserialized, expectExcessItemsToBeTrimmed, bsonDocument, doc);

            // we expect the original document instance that we pass into the deserializer to be the same as the one that gets returned from the deserializer
            Assert.Same(doc, rehydrated);

            // we expect all readonly properties that *are* marked with the UseExistingInstance attribute to be identical to the ones returned from the deserializer
            Assert.Same(useExistingInstanceReadOnlyCollection, rehydrated.UseExistingInstanceReadOnlyCollection);
            Assert.Same(useExistingInstanceReadOnlyInnerInstance, rehydrated.UseExistingInstanceReadOnlyInnerInstance);

            // we expect all writable properties that *are* marked with the UseExistingInstance attribute to be identical to the ones returned from the deserializer
            // unless the existing instance is null in which case we expect them to be different
            AssertSameIfNotNullOtherwiseAssertNotSame(useExistingInstanceWritableCollection, rehydrated.UseExistingInstanceWritableCollection);
            AssertSameIfNotNullOtherwiseAssertNotSame(useExistingInstanceWritableInnerInstance, rehydrated.UseExistingInstanceWritableInnerInstance);

            // we expect all writable properties that are *not* marked with the UseExistingInstance attribute to be different from the ones returned from the deserializer
            Assert.NotSame(doNotUseExistingInstanceWritableCollection, rehydrated.DoNotUseExistingInstanceWritableInnerInstance);
            Assert.NotSame(doNotUseExistingInstanceWritableInnerInstance, rehydrated.DoNotUseExistingInstanceWritableInnerInstance);

            // we expect all readonly properties that are *not* marked with the UseExistingInstance attribute to be identical to the the ones returned from the deserializer
            Assert.Same(doNotUseExistingInstanceReadOnlyCollection, rehydrated.DoNotUseExistingInstanceReadOnlyCollection);
            Assert.Same(doNotUseExistingInstanceReadOnlyInnerInstance, rehydrated.DoNotUseExistingInstanceReadOnlyInnerInstance);
        }

        private static void AssertSameIfNotNullOtherwiseAssertNotSame<TCollection>(TCollection originalCollection, TCollection rehydratedCollection)
        {
            if (originalCollection != null)
            {
                Assert.Same(originalCollection, rehydratedCollection);
            }
            else
            {
                Assert.NotSame(originalCollection, rehydratedCollection);
            }
        }

        private Test<TCollection> TestUsingTopLevelInstance<TCollection, TValue>(Func<TCollection> collectionFactory, TCollection valuesToBeDeserialized, bool expectExcessItemsToBeTrimmed, BsonDocument document, Test<TCollection> doc)
            where TCollection : IEnumerable<TValue>
        {
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = Deserialize(bsonReader, doc);
                IEnumerable<TValue> expectedResults = valuesToBeDeserialized;

                // we expect all writable properties that are *not* marked with the UseExistingInstance attribute to contain our new data
                // that's the default behaviour before the UseExistingInstance attribute even got created...
                Assert.True(expectedResults.SequenceEqual(rehydrated.DoNotUseExistingInstanceWritableCollection));
                Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceReadOnlyInnerInstance.DoNotUseExistingInstanceWritableCollection));
                Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceWritableInnerInstance.DoNotUseExistingInstanceWritableCollection));
                Assert.True(expectedResults.SequenceEqual(rehydrated.DoNotUseExistingInstanceWritableInnerInstance.DoNotUseExistingInstanceWritableCollection));

                TCollection collection = collectionFactory();

                if (!expectExcessItemsToBeTrimmed && !Equals(collection, default(TCollection)))
                {
                    // for all collections, other than list, the number of items in the collection will be equal to the number of deserialized items
                    // for arrays, however, the size will be the same as on the original instance since we reuse the existing array
                    expectedResults = expectedResults.Concat(Enumerable.Repeat(default(TValue), collection.Count() - valuesToBeDeserialized.Count())).ToList();
                }

                // we expect all writable properties that *are* marked with the UseExistingInstance attribute to contain our new data
                Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceWritableCollection));
                Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceReadOnlyInnerInstance.UseExistingInstanceWritableCollection));
                Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceWritableInnerInstance.UseExistingInstanceWritableCollection));
                Assert.True(expectedResults.SequenceEqual(rehydrated.DoNotUseExistingInstanceWritableInnerInstance.UseExistingInstanceWritableCollection));

                // we expect all readonly properties that *are* marked with the UseExistingInstance attribute to contain our new data unless the existing instance is null
                if (collection != null)
                {
                    Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceReadOnlyCollection));
                    Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceReadOnlyInnerInstance.UseExistingInstanceReadOnlyCollection));
                    Assert.True(expectedResults.SequenceEqual(rehydrated.UseExistingInstanceWritableInnerInstance.UseExistingInstanceReadOnlyCollection));
                    Assert.True(expectedResults.SequenceEqual(rehydrated.DoNotUseExistingInstanceWritableInnerInstance.UseExistingInstanceReadOnlyCollection));
                }
                else
                {
                    Assert.Null(rehydrated.UseExistingInstanceReadOnlyCollection);
                    Assert.Null(rehydrated.UseExistingInstanceReadOnlyInnerInstance.UseExistingInstanceReadOnlyCollection);
                    Assert.Null(rehydrated.UseExistingInstanceWritableInnerInstance.UseExistingInstanceReadOnlyCollection);
                    Assert.Null(rehydrated.DoNotUseExistingInstanceWritableInnerInstance.UseExistingInstanceReadOnlyCollection);
                }

                // we expect all readonly properties that are *not* marked with the UseExistingInstance attribute to be unchanged.
                Assert.Equal(collection, rehydrated.DoNotUseExistingInstanceReadOnlyCollection);
                Assert.Equal(collection, rehydrated.UseExistingInstanceReadOnlyInnerInstance.DoNotUseExistingInstanceReadOnlyCollection);
                Assert.Equal(collection, rehydrated.UseExistingInstanceWritableInnerInstance.DoNotUseExistingInstanceReadOnlyCollection);
                Assert.Equal(collection, rehydrated.DoNotUseExistingInstanceWritableInnerInstance.DoNotUseExistingInstanceReadOnlyCollection);

                // we expect all properties that sit inside a nested readonly type that's exposed by a readonly property that is *not* marked with the UseExistingInstance attribute to be unchanged.
                Assert.Equal(collection, rehydrated.DoNotUseExistingInstanceReadOnlyInnerInstance.DoNotUseExistingInstanceReadOnlyCollection);
                Assert.Equal(collection, rehydrated.DoNotUseExistingInstanceReadOnlyInnerInstance.DoNotUseExistingInstanceWritableCollection);
                Assert.Equal(collection, rehydrated.DoNotUseExistingInstanceReadOnlyInnerInstance.UseExistingInstanceWritableCollection);
                Assert.Equal(collection, rehydrated.DoNotUseExistingInstanceReadOnlyInnerInstance.UseExistingInstanceReadOnlyCollection);

                return rehydrated;
            }
        }

        private static T Deserialize<T>(IBsonReader bsonReader, T existingInstance = default(T))
        {
            return BsonSerializer.Deserialize(bsonReader, existingInstance: existingInstance);
        }
    }

    public class TestValueTypeWithAttribute
    {
        [BsonUseExistingInstance]
        public int ValueType { get; set; }
    }

    public class TestValueTypeWithoutAttribute
    {
        public int ValueType { get; set; }
    }

    public class Test<TCollection> where TCollection : IEnumerable
    {
        // readonly collection properties
        [BsonUseExistingInstance]
        public TCollection UseExistingInstanceReadOnlyCollection { get; }

        public TCollection DoNotUseExistingInstanceReadOnlyCollection { get; }

        // writable collection properties
        [BsonUseExistingInstance]
        public TCollection UseExistingInstanceWritableCollection { get; set; }

        public TCollection DoNotUseExistingInstanceWritableCollection { get; set; }

        // readonly nested classes
        [BsonUseExistingInstance]
        public Test<TCollection> UseExistingInstanceReadOnlyInnerInstance { get; }

        public Test<TCollection> DoNotUseExistingInstanceReadOnlyInnerInstance { get; }

        // writable nested classes
        [BsonUseExistingInstance]
        public Test<TCollection> UseExistingInstanceWritableInnerInstance { get; set; }

        public Test<TCollection> DoNotUseExistingInstanceWritableInnerInstance { get; set; }

        // hack so we can set the collection initializer from the outside in order to allow for a default constructor (called during deserialization) that still populates the collections
        internal static Func<TCollection> CollectionFactory;

        // this will be called by the deserializer
        public Test() : this(true)
        {
        }

        public Test(bool createInnerInstance)
        {
            UseExistingInstanceReadOnlyCollection = CollectionFactory();
            DoNotUseExistingInstanceReadOnlyCollection = CollectionFactory();
            UseExistingInstanceWritableCollection = CollectionFactory();
            DoNotUseExistingInstanceWritableCollection = CollectionFactory();
            if (createInnerInstance)
            {
                UseExistingInstanceReadOnlyInnerInstance = new Test<TCollection>(false);
                DoNotUseExistingInstanceReadOnlyInnerInstance = new Test<TCollection>(false);
                UseExistingInstanceWritableInnerInstance = new Test<TCollection>(false);
                DoNotUseExistingInstanceWritableInnerInstance = new Test<TCollection>(false);
            }
        }
    }
}