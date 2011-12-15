﻿/* Copyright 2010-2011 10gen Inc.
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp355
{
    [TestFixture]
    public class CSharp355Tests
    {
        public class C
        {
            public ObjectId Id { get; set; }
            public Image I { get; set; }
            public Bitmap B { get; set; }
            // public Metafile M { get; set; }
        }

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<C> collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true;slaveOk=true");
            database = server["onlinetests"];
            collection = database.GetCollection<C>("test");
            collection.Drop();
        }

        [Test]
        public void TestBitmap()
        {
            var bitmap = new Bitmap(1, 2);
            var c = new C { I = bitmap, B = bitmap };
            collection.RemoveAll();
            collection.Insert(c);
            var r = collection.FindOne();
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
            collection.RemoveAll();
            collection.Insert(c);
            var r = collection.FindOne();
            Assert.IsInstanceOf<C>(r);
            Assert.IsNull(r.I);
            Assert.IsNull(r.B);
            // Assert.IsNull(r.M);
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
