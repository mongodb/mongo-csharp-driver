/* Copyright 2010-2016 MongoDB Inc.
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
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp112
{
    public class CSharp112Tests
    {
#pragma warning disable 649 // never assigned to
        private class D
        {
            public int Id;
            public double N;
        }

        private class I
        {
            public int Id;
            public int N;
        }

        private class L
        {
            public int Id;
            public long N;
        }
#pragma warning restore

        private MongoCollection _collection;

        public CSharp112Tests()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            // test with valid values
            _collection.RemoveAll();
            var values = new object[]
            {
                0,
                0L,
                0.0,
                -1,
                -1L,
                -1.0,
                1,
                1L,
                1.0,
                Int32.MinValue,
                Int32.MaxValue
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                var document = _collection.FindOneAs<D>(query);
                Assert.Equal(BsonValue.Create(values[i]).ToDouble(), document.N);
            }

            // test with values that cause data loss
            _collection.RemoveAll();
            values = new object[]
            {
                0x7eeeeeeeeeeeeeee,
                0xfeeeeeeeeeeeeeee,
                Int64.MinValue + 1, // need some low order bits to see data loss
                Int64.MaxValue
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                try
                {
                    _collection.FindOneAs<D>(query);
                    Assert.True(false, "Expected an exception to be thrown.");
                }
                catch (Exception ex)
                {
                    var expectedMessage = "An error occurred while deserializing the N field of class MongoDB.Driver.Tests.Jira.CSharp112.CSharp112Tests+D: Truncation resulted in data loss.";
                    Assert.IsType<FormatException>(ex);
                    Assert.IsType<TruncationException>(ex.InnerException);
                    Assert.Equal(expectedMessage, ex.Message);
                }
            }
        }

        [Fact]
        public void TestDeserializeInt32()
        {
            // test with valid values
            _collection.RemoveAll();
            var values = new object[]
            {
                0L,
                0.0,
                -1L,
                -1.0,
                1L,
                1.0,
                (double)Int32.MinValue,
                (double)Int32.MaxValue,
                (long)Int32.MinValue,
                (long)Int32.MaxValue
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                var document = _collection.FindOneAs<I>(query);
                Assert.Equal(BsonValue.Create(values[i]).ToInt32(), document.N);
            }

            // test with values that cause overflow
            _collection.RemoveAll();
            values = new object[]
            {
                ((long)Int32.MinValue - 1),
                ((long)Int32.MaxValue + 1),
                Int64.MinValue,
                Int64.MaxValue,
                double.MaxValue,
                double.MinValue
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                try
                {
                    _collection.FindOneAs<I>(query);
                    Assert.True(false, "Expected an exception to be thrown.");
                }
                catch (Exception ex)
                {
                    var expectedMessage = "An error occurred while deserializing the N field of class MongoDB.Driver.Tests.Jira.CSharp112.CSharp112Tests+I";
                    Assert.IsType<FormatException>(ex);
                    Assert.IsType<OverflowException>(ex.InnerException);
                    Assert.Equal(expectedMessage, ex.Message.Substring(0, ex.Message.IndexOf(':')));
                }
            }

            // test with values that cause truncation
            _collection.RemoveAll();
            values = new object[]
            {
                -1.5,
                1.5
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                try
                {
                    _collection.FindOneAs<I>(query);
                    Assert.True(false, "Expected an exception to be thrown.");
                }
                catch (Exception ex)
                {
                    var expectedMessage = "An error occurred while deserializing the N field of class MongoDB.Driver.Tests.Jira.CSharp112.CSharp112Tests+I: Truncation resulted in data loss.";
                    Assert.IsType<FormatException>(ex);
                    Assert.IsType<TruncationException>(ex.InnerException);
                    Assert.Equal(expectedMessage, ex.Message);
                }
            }
        }

        [Fact]
        public void TestDeserializeInt64()
        {
            // test with valid values
            _collection.RemoveAll();
            var values = new object[]
            {
                0,
                0L,
                0.0,
                -1,
                -1L,
                -1.0,
                1,
                1L,
                1.0,
                (double)Int32.MinValue,
                (double)Int32.MaxValue,
                (long)Int32.MinValue,
                (long)Int32.MaxValue,
                Int64.MinValue,
                Int64.MaxValue
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                var document = _collection.FindOneAs<L>(query);
                Assert.Equal(BsonValue.Create(values[i]).ToInt64(), document.N);
            }

            // test with values that cause overflow
            _collection.RemoveAll();
            values = new object[]
            {
                double.MaxValue,
                double.MinValue
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                try
                {
                    _collection.FindOneAs<L>(query);
                    Assert.True(false, "Expected an exception to be thrown.");
                }
                catch (Exception ex)
                {
                    var expectedMessage = "An error occurred while deserializing the N field of class MongoDB.Driver.Tests.Jira.CSharp112.CSharp112Tests+L";
                    Assert.IsType<FormatException>(ex);
                    Assert.IsType<OverflowException>(ex.InnerException);
                    Assert.Equal(expectedMessage, ex.Message.Substring(0, ex.Message.IndexOf(':')));
                }
            }

            // test with values that cause data truncation
            _collection.RemoveAll();
            values = new object[]
            {
                -1.5,
                1.5
            };
            for (int i = 0; i < values.Length; i++)
            {
                var document = new BsonDocument
                {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                _collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var query = Query.EQ("_id", i + 1);
                try
                {
                    _collection.FindOneAs<L>(query);
                    Assert.True(false, "Expected an exception to be thrown.");
                }
                catch (Exception ex)
                {
                    var expectedMessage = "An error occurred while deserializing the N field of class MongoDB.Driver.Tests.Jira.CSharp112.CSharp112Tests+L: Truncation resulted in data loss.";
                    Assert.IsType<FormatException>(ex);
                    Assert.IsType<TruncationException>(ex.InnerException);
                    Assert.Equal(expectedMessage, ex.Message);
                }
            }
        }
    }
}
