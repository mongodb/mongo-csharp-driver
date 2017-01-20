' Copyright 2010-2016 MongoDB Inc.
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
Imports Xunit

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Linq

Namespace MongoDB.Driver.VB.Tests.Linq
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
                    m_Id = value
                End Set
            End Property
            Private m_Id As ObjectId
            <BsonElement("e")>
            <BsonRepresentation(BsonType.[String])>
            Public Property E() As System.Nullable(Of E)
                Get
                    Return m_E
                End Get
                Set(ByVal value As System.Nullable(Of E))
                    m_E = value
                End Set
            End Property
            Private m_E As System.Nullable(Of E)
            <BsonElement("x")>
            Public Property X() As System.Nullable(Of Integer)
                Get
                    Return m_X
                End Get
                Set(ByVal value As System.Nullable(Of Integer))
                    m_X = value
                End Set
            End Property
            Private m_X As System.Nullable(Of Integer)
        End Class

        Private Shared __server As MongoServer
        Private Shared __database As MongoDatabase
        Private Shared __collection As MongoCollection(Of C)
        Private Shared __lazyOneTimeSetup As Lazy(Of Boolean) = New Lazy(Of Boolean)(OneTimeSetup)

        Public Sub New()
            Dim x = __lazyOneTimeSetup.Value
        End Sub

        Private Shared Function OneTimeSetup() As Boolean
            __server = LegacyTestConfiguration.Server
            __database = LegacyTestConfiguration.Database
            __collection = LegacyTestConfiguration.GetCollection(Of C)()

            __collection.Drop()
            __collection.Insert(New C() With {
                .E = Nothing
            })
            __collection.Insert(New C() With {
                .E = E.A
            })
            __collection.Insert(New C() With {
                .E = E.B
            })
            __collection.Insert(New C() With {
                .X = Nothing
            })
            __collection.Insert(New C() With {
                .X = 1
            })
            __collection.Insert(New C() With {
                .X = 2
            })

            Return True
        End Function

        <Fact>
        Public Sub TestWhereEEqualsA()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.E = E.A
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""e"" : ""A"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEEqualsNull()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.E Is Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""e"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEqualsNull()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X Is Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
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
