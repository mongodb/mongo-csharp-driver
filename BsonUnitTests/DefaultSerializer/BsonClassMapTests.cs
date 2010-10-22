/* Copyright 2010 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer;

namespace MongoDB.BsonUnitTests.DefaultSerializer {
    [TestFixture]
    public class BsonClassMapTests {
        private class C {
            public short SD { get; set; }
            [BsonUseCompactRepresentation(false)]
            public short SF { get; set; }
            [BsonUseCompactRepresentation(true)]
            public short SC { get; set; }
        }

        [Test]
        public void TestInt16UseCompactRepresentation() {
            var classMap = BsonClassMap.RegisterClassMap<C>();
            var sdPropertyMap = classMap.GetPropertyMap("SD");
            var sfPropertyMap = classMap.GetPropertyMap("SF");
            var scPropertyMap = classMap.GetPropertyMap("SC");
            Assert.AreEqual(true, sdPropertyMap.UseCompactRepresentation);
            Assert.AreEqual(false, sfPropertyMap.UseCompactRepresentation);
            Assert.AreEqual(true, scPropertyMap.UseCompactRepresentation);
        }
    }
}
