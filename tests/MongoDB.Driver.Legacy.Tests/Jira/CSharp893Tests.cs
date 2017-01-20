/* Copyright 2010-2015 MongoDB Inc.
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

using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp893CheckElementNameTests
    {
        [Fact]
        public void TestEmptyElementNameNotAllowed()
        {
            var collection = LegacyTestConfiguration.GetCollection<BsonDocument>();
            collection.Drop();
            var document = new BsonDocument { { "_id", 1 }, { "", 2 } };
            Assert.Throws<BsonSerializationException>(() => collection.Insert(document));
        }
    }

    public abstract class CSharp893DictionaryTestsBase<C>
    {
        public C TestEmptyKey(C c, string expected)
        {
            var json = c.ToJson();
            Assert.Equal(expected.Replace("'", "\""), json);

            var collection = LegacyTestConfiguration.GetCollection<C>();
            collection.Drop();
            collection.Insert(c);
            return collection.FindOne();
        }
    }

    public class CSharp893DictionaryObjectObjectArrayOfArraysTests : CSharp893DictionaryTestsBase<CSharp893DictionaryObjectObjectArrayOfArraysTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
            public Dictionary<object, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<object, object> { { "", 2 } } };
            var rehydrated = TestEmptyKey(c, "{ '_id' : 1, 'd' : [['', 2]] }");
            Assert.Equal(1, rehydrated._id);
            Assert.Equal(1, rehydrated.d.Count);
            Assert.Equal(2, rehydrated.d[""]);
        }
    }

    public class CSharp893DictionaryObjectObjectArrayOfDocumentsTests : CSharp893DictionaryTestsBase<CSharp893DictionaryObjectObjectArrayOfDocumentsTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfDocuments)]
            public Dictionary<object, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<object, object> { { "", 2 } } };
            var rehydrated = TestEmptyKey(c, "{ '_id' : 1, 'd' : [{ 'k' : '', 'v' : 2 }] }");
            Assert.Equal(1, rehydrated._id);
            Assert.Equal(1, rehydrated.d.Count);
            Assert.Equal(2, rehydrated.d[""]);
        }
    }

    public class CSharp893DictionaryObjectObjectDocumentTests : CSharp893DictionaryTestsBase<CSharp893DictionaryObjectObjectDocumentTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.Document)]
            public Dictionary<object, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<object, object> { { "", 2 } } };
            Assert.Throws<BsonSerializationException>(() => TestEmptyKey(c, "{ '_id' : 1, 'd' : { '' : 2 } }"));
        }
    }

    public class CSharp893DictionaryObjectObjectDynamicTests : CSharp893DictionaryTestsBase<CSharp893DictionaryObjectObjectDynamicTests.C>
    {
        public class C
        {
            public int _id;
            public Dictionary<object, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<object, object> { { "", 2 } } };
            Assert.Throws<BsonSerializationException>(() => TestEmptyKey(c, "{ '_id' : 1, 'd' : { '' : 2 } }"));
        }
    }

    public class CSharp893DictionaryStringObjectArrayOfArraysTests : CSharp893DictionaryTestsBase<CSharp893DictionaryStringObjectArrayOfArraysTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
            public Dictionary<string, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<string, object> { { "", 2 } } };
            var rehydrated = TestEmptyKey(c, "{ '_id' : 1, 'd' : [['', 2]] }");
            Assert.Equal(1, rehydrated._id);
            Assert.Equal(1, rehydrated.d.Count);
            Assert.Equal(2, rehydrated.d[""]);
        }
    }

    public class CSharp893DictionaryStringObjectArrayOfDocumentsTests : CSharp893DictionaryTestsBase<CSharp893DictionaryStringObjectArrayOfDocumentsTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfDocuments)]
            public Dictionary<string, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<string, object> { { "", 2 } } };
            var rehydrated = TestEmptyKey(c, "{ '_id' : 1, 'd' : [{ 'k' : '', 'v' : 2 }] }");
            Assert.Equal(1, rehydrated._id);
            Assert.Equal(1, rehydrated.d.Count);
            Assert.Equal(2, rehydrated.d[""]);
        }
    }

    public class CSharp893DictionaryStringObjectDocumentTests : CSharp893DictionaryTestsBase<CSharp893DictionaryStringObjectDocumentTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.Document)]
            public Dictionary<string, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<string, object> { { "", 2 } } };
            Assert.Throws<BsonSerializationException>(() => TestEmptyKey(c, "{ '_id' : 1, 'd' : { '' : 2 } }"));
        }
    }

    public class CSharp893DictionaryStringObjectDynamicTests : CSharp893DictionaryTestsBase<CSharp893DictionaryStringObjectDynamicTests.C>
    {
        public class C
        {
            public int _id;
            public Dictionary<string, object> d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Dictionary<string, object> { { "", 2 } } };
            Assert.Throws<BsonSerializationException>(() => TestEmptyKey(c, "{ '_id' : 1, 'd' : { '' : 2 } }"));
        }
    }

    public class CSharp893HashtableArrayOfArraysTests : CSharp893DictionaryTestsBase<CSharp893HashtableArrayOfArraysTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
            public Hashtable d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Hashtable { { "", 2 } } };
            var rehydrated = TestEmptyKey(c, "{ '_id' : 1, 'd' : [['', 2]] }");
            Assert.Equal(1, rehydrated._id);
            Assert.Equal(1, rehydrated.d.Count);
            Assert.Equal(2, rehydrated.d[""]);
        }
    }

    public class CSharp893HashtableArrayOfDocumentsTests : CSharp893DictionaryTestsBase<CSharp893HashtableArrayOfDocumentsTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfDocuments)]
            public Hashtable d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Hashtable { { "", 2 } } };
            var rehydrated = TestEmptyKey(c, "{ '_id' : 1, 'd' : [{ 'k' : '', 'v' : 2 }] }");
            Assert.Equal(1, rehydrated._id);
            Assert.Equal(1, rehydrated.d.Count);
            Assert.Equal(2, rehydrated.d[""]);
        }
    }

    public class CSharp893HashtableDocumentTests : CSharp893DictionaryTestsBase<CSharp893HashtableDocumentTests.C>
    {
        public class C
        {
            public int _id;
            [BsonDictionaryOptions(Representation = DictionaryRepresentation.Document)]
            public Hashtable d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Hashtable { { "", 2 } } };
            Assert.Throws<BsonSerializationException>(() => TestEmptyKey(c, "{ '_id' : 1, 'd' : { '' : 2 } }"));
        }
    }

    public class CSharp893HashtableDynamicTests : CSharp893DictionaryTestsBase<CSharp893HashtableDynamicTests.C>
    {
        public class C
        {
            public int _id;
            public Hashtable d;
        }

        [Fact]
        public void TestEmptyKey()
        {
            var c = new C { _id = 1, d = new Hashtable { { "", 2 } } };
            Assert.Throws<BsonSerializationException>(() => TestEmptyKey(c, "{ '_id' : 1, 'd' : { '' : 2 } }"));
        }
    }
}