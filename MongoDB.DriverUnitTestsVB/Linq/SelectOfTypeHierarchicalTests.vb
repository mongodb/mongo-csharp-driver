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

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports NUnit.Framework

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Builders
Imports MongoDB.Driver.Linq

Namespace MongoDB.DriverUnitTests.Linq
    <TestFixture()> _
    Public Class SelectOfTypeHierarchicalTests
        <BsonDiscriminator(RootClass:=True)> _
        Private Class B
            Public Id As ObjectId
            Public b As Integer
        End Class

        Private Class C
            Inherits B
            Public c As Integer
        End Class

        Private Class D
            Inherits C
            Public d As Integer
        End Class

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection(Of B)

        <TestFixtureSetUp()> _
        Public Sub Setup()
            _server = Configuration.TestServer
            _database = Configuration.TestDatabase
            _collection = Configuration.GetTestCollection(Of B)()

            _collection.Drop()
            _collection.Insert(New B() With
            {
                 .Id = ObjectId.GenerateNewId(),
                 .b = 1
            })
            _collection.Insert(New C() With
            {
                .Id = ObjectId.GenerateNewId(),
                .b = 2,
                .c = 2
            })
            _collection.Insert(New D() With
            {
                 .Id = ObjectId.GenerateNewId(),
                 .b = 3,
                 .c = 3,
                 .d = 3
            })
        End Sub

        <Test()> _
        Public Sub TestOfTypeB()
            Dim query = _collection.AsQueryable(Of B)().OfType(Of B)()

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B x) => LinqToMongo.Inject({ ""_t"" : ""B"" })", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(GetType(B), selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""B"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestOfTypeC()
            Dim query = _collection.AsQueryable(Of B)().OfType(Of C)()

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B x) => LinqToMongo.Inject({ ""_t"" : ""C"" })", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(GetType(C), selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""C"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestOfTypeCWhereCGreaterThan0()
            Dim query = _collection.AsQueryable(Of B)().OfType(Of C)().Where(Function(c) c.c > 0)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (LinqToMongo.Inject({ ""_t"" : ""C"" }) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(GetType(C), selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""C"", ""c"" : { ""$gt"" : 0 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestOfTypeD()
            Dim query = _collection.AsQueryable(Of B)().OfType(Of D)()

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B x) => LinqToMongo.Inject({ ""_t"" : ""D"" })", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(GetType(D), selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""D"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBGreaterThan0OfTypeCWhereCGreaterThan0()
            Dim query = _collection.AsQueryable(Of B)().Where(Function(b) B.b > 0).OfType(Of C)().Where(Function(c) c.c > 0)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (((c.b > 0) && LinqToMongo.Inject({ ""_t"" : ""C"" })) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(GetType(C), selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : { ""$gt"" : 0 }, ""_t"" : ""C"", ""c"" : { ""$gt"" : 0 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBIsB()
            Dim query = From b In _collection.AsQueryable(Of B)()
                        Where TypeOf b Is B
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B b) => (b is B)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(Nothing, selectQuery.OfType)
            ' OfType ignored because <T> was the same as <TDocument>
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""B"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBIsC()
            Dim query = From b In _collection.AsQueryable(Of B)()
                        Where TypeOf b Is C
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B b) => (b is C)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(Nothing, selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""C"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBIsD()
            Dim query = From b In _collection.AsQueryable(Of B)()
                        Where TypeOf b Is D
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B b) => (b is D)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(Nothing, selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : ""D"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBTypeEqualsB()
            If _server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From b In _collection.AsQueryable(Of B)()
                            Where b.GetType().Equals(GetType(B))
                            Select b

                Dim translatedQuery = MongoQueryTranslator.Translate(query)
                Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
                Assert.AreSame(_collection, translatedQuery.Collection)
                Assert.AreSame(GetType(B), translatedQuery.DocumentType)

                Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
                Assert.AreEqual("(B b) => b.GetType().Equals(typeof(B))", ExpressionFormatter.ToString(selectQuery.Where))
                Assert.AreEqual(Nothing, selectQuery.OfType)
                ' OfType ignored because <T> was the same as <TDocument>
                Assert.IsNull(selectQuery.OrderBy)
                Assert.IsNull(selectQuery.Projection)
                Assert.IsNull(selectQuery.Skip)
                Assert.IsNull(selectQuery.Take)

                Assert.AreEqual("{ ""_t.0"" : { ""$exists"" : false }, ""_t"" : ""B"" }", selectQuery.BuildQuery().ToJson())
                Assert.AreEqual(1, Consume(query))
            End If
        End Sub

        <Test()> _
        Public Sub TestWhereBTypeEqualsC()
            Dim query = From b In _collection.AsQueryable(Of B)()
                        Where b.GetType().Equals(GetType(C))
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B b) => b.GetType().Equals(typeof(C))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(Nothing, selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : { ""$size"" : 2 }, ""_t.0"" : ""B"", ""_t.1"" : ""C"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBTypeEqualsD()
            Dim query = From b In _collection.AsQueryable(Of B)()
                        Where b.GetType().Equals(GetType(D))
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(B b) => b.GetType().Equals(typeof(D))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.AreEqual(Nothing, selectQuery.OfType)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_t"" : { ""$size"" : 3 }, ""_t.0"" : ""B"", ""_t.1"" : ""C"", ""_t.2"" : ""D"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
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
