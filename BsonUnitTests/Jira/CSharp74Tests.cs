﻿/* Copyright 2010-2012 10gen Inc.
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
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp74Tests
    {
        [DataContract]
        private class Employee
        {
            [DataMember]
            public ObjectId EmployeeId { get; set; }
            [DataMember]
            public string Name { get; set; }
        }

        [Test]
        public void TestObjectIdSerialization()
        {
            var employee = new Employee { EmployeeId = ObjectId.GenerateNewId(), Name = "John Smith" };
            var serializer = new DataContractSerializer(typeof(Employee));
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true };
            using (var xmlWriter = XmlWriter.Create(sb, settings))
            {
                serializer.WriteObject(xmlWriter, employee);
            }
            var xml = sb.ToString();

            Employee rehydrated;
            using (var xmlReader = XmlReader.Create(new StringReader(xml)))
            {
                rehydrated = (Employee)serializer.ReadObject(xmlReader);
            }

            Assert.AreEqual(employee.EmployeeId, rehydrated.EmployeeId);
            Assert.AreEqual(employee.Name, rehydrated.Name);
        }
    }
}
