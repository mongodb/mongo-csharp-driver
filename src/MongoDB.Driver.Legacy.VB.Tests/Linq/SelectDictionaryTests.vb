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


Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Xunit

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Bson.Serialization.Options
Imports MongoDB.Driver
Imports MongoDB.Driver.Linq

Namespace MongoDB.Driver.VB.Tests.Linq
    Public Class SelectDictionaryTests
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

            <BsonDictionaryOptions(DictionaryRepresentation.Document)>
            Public Property D() As IDictionary(Of String, Integer)
                Get
                    Return m_D
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_D = value
                End Set
            End Property
            Private m_D As IDictionary(Of String, Integer)
            ' serialized as { D : { x : 1, ... } }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)>
            Public Property E() As IDictionary(Of String, Integer)
                Get
                    Return m_E
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_E = value
                End Set
            End Property
            Private m_E As IDictionary(Of String, Integer)
            ' serialized as { E : [{ k : "x", v : 1 }, ...] }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)>
            Public Property F() As IDictionary(Of String, Integer)
                Get
                    Return m_F
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_F = value
                End Set
            End Property
            Private m_F As IDictionary(Of String, Integer)
            ' serialized as { F : [["x", 1], ... ] }
            <BsonDictionaryOptions(DictionaryRepresentation.Document)>
            Public Property H() As IDictionary
                Get
                    Return m_H
                End Get
                Set(ByVal value As IDictionary)
                    m_H = value
                End Set
            End Property
            Private m_H As IDictionary
            ' serialized as { H : { x : 1, ... } }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)>
            Public Property I() As IDictionary
                Get
                    Return m_I
                End Get
                Set(ByVal value As IDictionary)
                    m_I = value
                End Set
            End Property
            Private m_I As IDictionary
            ' serialized as { I : [{ k : "x", v : 1 }, ...] }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)>
            Public Property J() As IDictionary
                Get
                    Return m_J
                End Get
                Set(ByVal value As IDictionary)
                    m_J = value
                End Set
            End Property
            Private m_J As IDictionary
            ' serialized as { J : [["x", 1], ... ] }
        End Class

        Private Shared __server As MongoServer
        Private Shared __database As MongoDatabase
        Private Shared __collection As MongoCollection

        Private Shared __lazyOneTimeSetup As Lazy(Of Boolean) = New Lazy(Of Boolean)(OneTimeSetup)

        Public Sub New()
            Dim x = __lazyOneTimeSetup.Value
        End Sub

        Private Shared Function OneTimeSetup() As Boolean
            __server = LegacyTestConfiguration.Server
            __database = LegacyTestConfiguration.Database
            __collection = LegacyTestConfiguration.GetCollection(Of C)()

            Dim de = New Dictionary(Of String, Integer)()
            Dim dx = New Dictionary(Of String, Integer)() From {
             {"x", 1}
            }
            Dim dy = New Dictionary(Of String, Integer)() From {
             {"y", 1}
            }

            Dim he = New Hashtable()
            Dim hx = New Hashtable() From {
             {"x", 1}
            }
            Dim hy = New Hashtable() From {
             {"y", 1}
            }

            __collection.Drop()
            __collection.Insert(New C() With {
                .D = Nothing,
                .E = Nothing,
                .F = Nothing,
                .H = Nothing,
                .I = Nothing,
                .J = Nothing
            })
            __collection.Insert(New C() With {
                .D = de,
                .E = de,
                .F = de,
                .H = he,
                .I = he,
                .J = he
            })
            __collection.Insert(New C() With {
                .D = dx,
                .E = dx,
                .F = dx,
                .H = hx,
                .I = hx,
                .J = hx
            })
            __collection.Insert(New C() With {
                 .D = dy,
                 .E = dy,
                 .F = dy,
                 .H = hy,
                 .I = hy,
                 .J = hy
            })

            Return True
        End Function

        <Fact>
        Public Sub TestWhereDContainsKeyX()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.D.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.D.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""D.x"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereDContainsKeyZ()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.D.ContainsKey("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.D.ContainsKey(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""D.z"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereEContainsKeyX()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.E.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.E.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""E.k"" : ""x"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereEContainsKeyZ()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.E.ContainsKey("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.E.ContainsKey(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""E.k"" : ""z"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereFContainsKeyX()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.F.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.F.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Dim ex = Assert.Throws(Of NotSupportedException)(Sub()
                                                                 selectQuery.BuildQuery()
                                                             End Sub)
            Assert.Equal("ContainsKey in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message)
        End Sub

        <Fact>
        Public Sub TestWhereHContainsKeyX()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.H.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.H.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""H.x"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereHContainsKeyZ()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.H.Contains("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.H.Contains(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""H.z"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereIContainsKeyX()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.I.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.I.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""I.k"" : ""x"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereIContainsKeyZ()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.I.Contains("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.I.Contains(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""I.k"" : ""z"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, query.ToList().Count())
        End Sub

        <Fact>
        Public Sub TestWhereJContainsKeyX()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.J.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => c.J.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Dim ex = Assert.Throws(Of NotSupportedException)(Sub()
                                                                 selectQuery.BuildQuery()
                                                             End Sub)
            Assert.Equal("Contains in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message)
        End Sub
    End Class
End Namespace
