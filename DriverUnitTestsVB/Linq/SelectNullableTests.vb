' Copyright 2010-2012 10gen Inc.
'*
'* Licensed under the Apache License, Version 2.0 (the "License");
'* you may not use this file except in compliance with the License.
'* You may obtain a copy of the License at
'*
'* http://www.apache.org/licenses/LICENSE-2.0
'*
'* Unless required by applicable law or agreed to in writing, software
'* distributed under the License is distributed on an "AS IS" BASIS,
'* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
'* See the License for the specific language governing permissions and
'* limitations under the License.
'


Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports NUnit.Framework

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Linq

Namespace MongoDB.DriverUnitTests.Linq
    <TestFixture()> _
    Public Class SelectNullableTests
        Private Enum E
            None
            A
            B
        End Enum

        Private Class C
            Public Property Id() As ObjectId
                Get
                    Return m_Id
                End Get
                Set(ByVal value As ObjectId)
                    m_Id = Value
                End Set
            End Property
            Private m_Id As ObjectId
            <BsonElement("e")> _
            <BsonRepresentation(BsonType.[String])> _
            Public Property E() As System.Nullable(Of E)
                Get
                    Return m_E
                End Get
                Set(ByVal value As System.Nullable(Of E))
                    m_E = Value
                End Set
            End Property
            Private m_E As System.Nullable(Of E)
            <BsonElement("x")> _
            Public Property X() As System.Nullable(Of Integer)
                Get
                    Return m_X
                End Get
                Set(ByVal value As System.Nullable(Of Integer))
                    m_X = Value
                End Set
            End Property
            Private m_X As System.Nullable(Of Integer)
        End Class

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection(Of C)

        <TestFixtureSetUp()> _
        Public Sub Setup()
            _server = Configuration.TestServer
            _database = Configuration.TestDatabase
            _collection = Configuration.GetTestCollection(Of C)()

            _collection.Drop()
            _collection.Insert(New C() With { _
                .E = Nothing _
            })
            _collection.Insert(New C() With { _
                .E = E.A _
            })
			_collection.Insert(New C() With { _
				.E = E.B _
			})
			_collection.Insert(New C() With { _
				.X = Nothing _
			})
			_collection.Insert(New C() With { _
				.X = 1 _
			})
            _collection.Insert(New C() With { _
                .X = 2 _
            })
        End Sub

        <Test()> _
        Public Sub TestWhereEEqualsA()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.E = E.A
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : ""A"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEEqualsNull()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.E Is Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEqualsNull()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X Is Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        Private Function Consume(Of T)(ByVal query As IQueryable(Of T)) As Integer
            Dim count = 0
            For Each c In query
                count += 1
            Next
            Return count
        End Function
    End Class
End Namespace
