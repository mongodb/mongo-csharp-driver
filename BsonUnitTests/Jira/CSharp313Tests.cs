/* Copyright 2010-2011 10gen Inc.
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
using System.Globalization;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp313Tests
    {
        private static readonly ArrayList scalarVals = new ArrayList {
          'a', 
          "a",
          (byte) 2,
          (sbyte) -2,
          (ushort) 512,
          (short) -512,
          (uint) 66000,
          (int) -66000,
          (ulong) 4294967296,
          (long) -4294967296,
          (float) 3.14159,
          (double) 1024.3453,
          (decimal) 1.23767,
          DateTime.Now.TimeOfDay,
          DateTime.Now,
          new DateTimeOffset(DateTime.Now),
          Guid.NewGuid(),
          ObjectId.GenerateNewId(),
          CultureInfo.CurrentCulture,
          new Uri("http://www.mongodb.org"),
          new Version(1,0,1337)
        };

        [Test]
        public void ScalarToBsonDocumentTest() {
            foreach (var scalar in scalarVals) {
                var msg = string.Format("Cannot serialize object of type {0} to BsonDocument.", scalar.GetType());
                var failureMsg =
                    string.Format(
                        "scalar.ToBsonDocument({0}) should have thrown an InvalidOperationException with message {0}",
                        msg);
                try {
                    scalar.ToBsonDocument(scalar.GetType());
                    Assert.Fail(failureMsg);
                }
                catch (InvalidOperationException ex) {
                    Assert.AreEqual(msg, ex.Message);
                }
            }
        }
    }
}