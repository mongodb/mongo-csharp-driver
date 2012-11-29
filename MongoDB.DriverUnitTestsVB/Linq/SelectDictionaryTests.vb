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


Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports NUnit.Framework

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Bson.Serialization.Options
Imports MongoDB.Driver
Imports MongoDB.Driver.Linq

Namespace MongoDB.DriverUnitTests.Linq
    <TestFixture()> _
    Public Class SelectDictionaryTests
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

            <BsonDictionaryOptions(DictionaryRepresentation.Document)> _
            Public Property D() As IDictionary(Of String, Integer)
                Get
                    Return m_D
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_D = Value
                End Set
            End Property
            Private m_D As IDictionary(Of String, Integer)
            ' serialized as { D : { x : 1, ... } }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)> _
            Public Property E() As IDictionary(Of String, Integer)
                Get
                    Return m_E
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_E = Value
                End Set
            End Property
            Private m_E As IDictionary(Of String, Integer)
            ' serialized as { E : [{ k : "x", v : 1 }, ...] }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)> _
            Public Property F() As IDictionary(Of String, Integer)
                Get
                    Return m_F
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_F = Value
                End Set
            End Property
            Private m_F As IDictionary(Of String, Integer)
            ' serialized as { F : [["x", 1], ... ] }
            <BsonDictionaryOptions(DictionaryRepresentation.Dynamic)> _
            Public Property G() As IDictionary(Of String, Integer)
                Get
                    Return m_G
                End Get
                Set(ByVal value As IDictionary(Of String, Integer))
                    m_G = Value
                End Set
            End Property
            Private m_G As IDictionary(Of String, Integer)
            ' serialized form depends on actual key values
            <BsonDictionaryOptions(DictionaryRepresentation.Document)> _
            Public Property H() As IDictionary
                Get
                    Return m_H
                End Get
                Set(ByVal value As IDictionary)
                    m_H = Value
                End Set
            End Property
            Private m_H As IDictionary
            ' serialized as { H : { x : 1, ... } }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)> _
            Public Property I() As IDictionary
                Get
                    Return m_I
                End Get
                Set(ByVal value As IDictionary)
                    m_I = Value
                End Set
            End Property
            Private m_I As IDictionary
            ' serialized as { I : [{ k : "x", v : 1 }, ...] }
            <BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)> _
            Public Property J() As IDictionary
                Get
                    Return m_J
                End Get
                Set(ByVal value As IDictionary)
                    m_J = Value
                End Set
            End Property
            Private m_J As IDictionary
            ' serialized as { J : [["x", 1], ... ] }
            <BsonDictionaryOptions(DictionaryRepresentation.Dynamic)> _
            Public Property K() As IDictionary
                Get
                    Return m_K
                End Get
                Set(ByVal value As IDictionary)
                    m_K = Value
                End Set
            End Property
            Private m_K As IDictionary
            ' serialized form depends on actual key values
        End Class

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection

        <TestFixtureSetUp()> _
        Public Sub Setup()
            _server = Configuration.TestServer
            _database = Configuration.TestDatabase
            _collection = Configuration.GetTestCollection(Of C)()

            Dim de = New Dictionary(Of String, Integer)()
            Dim dx = New Dictionary(Of String, Integer)() From { _
             {"x", 1} _
            }
            Dim dy = New Dictionary(Of String, Integer)() From { _
             {"y", 1} _
            }

            Dim he = New Hashtable()
            Dim hx = New Hashtable() From { _
             {"x", 1} _
            }
            Dim hy = New Hashtable() From { _
             {"y", 1} _
            }

            _collection.Drop()
			_collection.Insert(New C() With { _
				.D = Nothing, _
				.E = Nothing, _
				.F = Nothing, _
				.G = Nothing, _
				.H = Nothing, _
				.I = Nothing, _
				.J = Nothing, _
				.K = Nothing _
			})
			_collection.Insert(New C() With { _
				.D = de, _
				.E = de, _
				.F = de, _
				.G = de, _
				.H = he, _
				.I = he, _
				.J = he, _
				.K = he _
			})
			_collection.Insert(New C() With { _
				.D = dx, _
				.E = dx, _
				.F = dx, _
				.G = dx, _
				.H = hx, _
				.I = hx, _
				.J = hx, _
				.K = hx _
			})
            _collection.Insert(New C() With { _
                 .D = dy, _
                 .E = dy, _
                 .F = dy, _
                 .G = dy, _
                 .H = hy, _
                 .I = hy, _
                 .J = hy, _
                 .K = hy _
            })
        End Sub

        <Test()> _
        Public Sub TestWhereDContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.D.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.D.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""D.x"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereDContainsKeyZ()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.D.ContainsKey("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.D.ContainsKey(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""D.z"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereEContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.E.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.E.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""E.k"" : ""x"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereEContainsKeyZ()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.E.ContainsKey("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.E.ContainsKey(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""E.k"" : ""z"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereFContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.F.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.F.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Dim ex = Assert.Throws(Of NotSupportedException)(Sub()
                                                                 selectQuery.BuildQuery()
                                                             End Sub)
            Assert.AreEqual("ContainsKey in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message)
        End Sub

        <Test()> _
        Public Sub TestWhereGContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.G.ContainsKey("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.G.ContainsKey(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Dim ex = Assert.Throws(Of NotSupportedException)(Sub()
                                                                 selectQuery.BuildQuery()
                                                             End Sub)
            Assert.AreEqual("ContainsKey in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not Dynamic.", ex.Message)
        End Sub

        <Test()> _
        Public Sub TestWhereHContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.H.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.H.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""H.x"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereHContainsKeyZ()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.H.Contains("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.H.Contains(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""H.z"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereIContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.I.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.I.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""I.k"" : ""x"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereIContainsKeyZ()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.I.Contains("z")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.I.Contains(""z"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""I.k"" : ""z"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, query.ToList().Count())
        End Sub

        <Test()> _
        Public Sub TestWhereJContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.J.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.J.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Dim ex = Assert.Throws(Of NotSupportedException)(Sub()
                                                                 selectQuery.BuildQuery()
                                                             End Sub)
            Assert.AreEqual("Contains in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not ArrayOfArrays.", ex.Message)
        End Sub

        <Test()> _
        Public Sub TestWhereKContainsKeyX()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.K.Contains("x")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => c.K.Contains(""x"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Dim ex = Assert.Throws(Of NotSupportedException)(Sub()
                                                                 selectQuery.BuildQuery()
                                                             End Sub)
            Assert.AreEqual("Contains in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not Dynamic.", ex.Message)
        End Sub
    End Class
End Namespace
