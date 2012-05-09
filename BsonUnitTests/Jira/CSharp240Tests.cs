/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Cryptography;
using MongoDB.Bson.Serialization.Serializers;
using System.Reflection;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.BsonUnitTests.Jira.CSharp240
{
    [TestFixture]
    public class CSharp240Tests
    {
        public class Person
        {
            public ObjectId Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            [BsonEncryption]
            public string SocialSecurityNumber { get; set; }
        }

        [Test]
        public void TestEncryptionDecryptionViaAttribute()
        {
            var person = new Person { FirstName = "Jack", LastName = "McJack", SocialSecurityNumber = "123-45-6789" };

            var serializedDoc = new BsonDocument();
            var writer = BsonWriter.Create(serializedDoc);

            BsonSerializer.Serialize<Person>(writer, person);

            Assert.AreEqual(person.FirstName, (string)serializedDoc["FirstName"]);
            Assert.AreEqual(person.LastName, (string)serializedDoc["LastName"]);
            Assert.IsTrue(serializedDoc["SocialSecurityNumber"].IsBsonBinaryData);
            Assert.AreNotEqual(person.SocialSecurityNumber, Encoding.UTF8.GetString((byte[])serializedDoc["SocialSecurityNumber"].AsBsonBinaryData));
            Assert.AreEqual(person.SocialSecurityNumber, EncryptingStringSerializer.Decrypt((byte[])serializedDoc["SocialSecurityNumber"].AsBsonBinaryData));

            var deserializedPerson = BsonSerializer.Deserialize<Person>(serializedDoc);

            Assert.AreEqual(person.FirstName, deserializedPerson.FirstName);
            Assert.AreEqual(person.LastName, deserializedPerson.LastName);
            Assert.AreEqual(person.SocialSecurityNumber, deserializedPerson.SocialSecurityNumber);
        }

        [Test]
        public void TestEncryptionDecryption()
        {
            var original = "Hello, Jack McJack";
            var encrypted = EncryptingStringSerializer.Encrypt(original);
            var decrypted = EncryptingStringSerializer.Decrypt(encrypted);

            Assert.AreEqual(original, decrypted);
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        private class BsonEncryptionAttribute : Attribute, IBsonMemberMapModifier
        {
            public void Apply(BsonMemberMap memberMap)
            {
                if (memberMap.MemberType != typeof(string))
                    throw new NotSupportedException("BsonEncryptionAttribute can only be applied to string members.");

                memberMap.SetSerializer(new EncryptingStringSerializer());
            }
        }

        private class EncryptingStringSerializer : BsonBaseSerializer
        {
            public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
            {
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(out bytes, out subType);

                return Decrypt(bytes);
            }

            public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
            {
                var bytes = Encrypt((string)value);
                bsonWriter.WriteBinaryData(bytes, BsonBinarySubType.Binary);
            }

            public static byte[] Encrypt(string toEncrypt)
            {
                byte[] keyArray = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 256-AES key
                byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);
                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.IV = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
                rDel.Padding = PaddingMode.PKCS7;
                ICryptoTransform cTransform = rDel.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                return resultArray;
            }

            public static string Decrypt(byte[] toDecrypt)
            {
                byte[] keyArray = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // AES-256 key
                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.IV = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
                rDel.Padding = PaddingMode.PKCS7;
                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toDecrypt, 0, toDecrypt.Length);
                return Encoding.UTF8.GetString(resultArray);
            }
        }
    }
}