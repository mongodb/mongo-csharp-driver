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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.UnitTests;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp355
{
    [TestFixture]
    public class CSharp355Tests
    {
        public class C
        {
            public ObjectId Id { get; set; }
            public Image I { get; set; }
            public Bitmap B { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();
            _collection.Drop();
        }

        [Test]
        public void TestBitmap()
        {
            if (TestEnvironment.IsMono)
            {
                // this test does not work in Mono. Skipping for the time being
                // CSHARP-389
                return;
            }
            var bitmap = new Bitmap(1, 2);
            var c = new C { I = bitmap, B = bitmap };
            _collection.RemoveAll();
            _collection.Insert(c);
            var r = _collection.FindOne();
            Assert.IsInstanceOf<C>(r);
            Assert.IsInstanceOf<Bitmap>(r.I);
            Assert.AreEqual(1, r.B.Width);
            Assert.AreEqual(2, r.B.Height);
            Assert.IsTrue(GetBytes(bitmap).SequenceEqual(GetBytes(r.B)));
        }

        [Test]
        public void TestImageNull()
        {
            var c = new C { I = null, B = null };
            _collection.RemoveAll();
            _collection.Insert(c);
            var r = _collection.FindOne();
            Assert.IsInstanceOf<C>(r);
            Assert.IsNull(r.I);
            Assert.IsNull(r.B);
        }

        private byte[] GetBytes(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);
                return stream.ToArray();
            }
        }
    }
}
