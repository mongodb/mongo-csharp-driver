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

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Xunit

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Builders
Imports MongoDB.Driver.Linq
Imports MongoDB.Driver.Tests

Namespace MongoDB.Driver.VB.Tests.Linq
    Public Class SelectOfTypeTests
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

        Private Shared __server As MongoServer
        Private Shared __database As MongoDatabase
        Private Shared __collection As MongoCollection(Of B)
        Private Shared __lazyOneTimeSetup As Lazy(Of Boolean) = New Lazy(Of Boolean)(OneTimeSetup)

        Public Sub New()
            Dim x = __lazyOneTimeSetup.Value
        End Sub

        Private Shared Function OneTimeSetup() As Boolean
            __server = LegacyTestConfiguration.Server
            __database = LegacyTestConfiguration.Database
            __collection = LegacyTestConfiguration.GetCollection(Of B)()

            __collection.Drop()
            __collection.Insert(New B() With
            {
                 .Id = ObjectId.GenerateNewId(),
                 .b = 1
            })
            __collection.Insert(Of B)(New C() With
            {
                .Id = ObjectId.GenerateNewId(),
                .b = 2,
                .c = 2
            })
            __collection.Insert(Of B)(New D() With
            {
                 .Id = ObjectId.GenerateNewId(),
                 .b = 3,
                 .c = 3,
                 .d = 3
            })

            Return True
        End Function

        <Fact>
        Public Sub TestOfTypeB()
            Dim query = __collection.AsQueryable(Of B)().OfType(Of B)()

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(Nothing, selectQuery.OfType)
            ' OfType ignored because <T> was the same as <TDocument>
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestOfTypeC()
            Dim query = __collection.AsQueryable(Of B)().OfType(Of C)()

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B x) => LinqToMongo.Inject({ ""_t"" : ""C"" })", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(GetType(C), selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t"" : ""C"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
            ' should match 2 but for that you need to use the hierarchical discriminator
        End Sub

        <Fact>
        Public Sub TestOfTypeCWhereCGreaterThan0()
            Dim query = __collection.AsQueryable(Of B)().OfType(Of C)().Where(Function(c) c.c > 0)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (LinqToMongo.Inject({ ""_t"" : ""C"" }) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(GetType(C), selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t"" : ""C"", ""c"" : { ""$gt"" : 0 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
            ' should match 2 but for that you need to use the hierarchical discriminator
        End Sub

        <Fact>
        Public Sub TestOfTypeD()
            Dim query = __collection.AsQueryable(Of B)().OfType(Of D)()

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B x) => LinqToMongo.Inject({ ""_t"" : ""D"" })", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(GetType(D), selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t"" : ""D"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBGreaterThan0OfTypeCWhereCGreaterThan0()
            Dim query = __collection.AsQueryable(Of B)().Where(Function(b) b.b > 0).OfType(Of C)().Where(Function(c) c.c > 0)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (((c.b > 0) && LinqToMongo.Inject({ ""_t"" : ""C"" })) && (c.c > 0))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(GetType(C), selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""b"" : { ""$gt"" : 0 }, ""_t"" : ""C"", ""c"" : { ""$gt"" : 0 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
            ' should match 2 but for that you need to use the hierarchical discriminator
        End Sub

        <Fact>
        Public Sub TestWhereBIsB()
            Dim query = From b In __collection.AsQueryable(Of B)()
                        Where TypeOf b Is B
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B b) => (b is B)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(Nothing, selectQuery.OfType)
            ' OfType ignored because <T> was the same as <TDocument>
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBIsC()
            Dim query = From b In __collection.AsQueryable(Of B)()
                        Where TypeOf b Is C
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B b) => (b is C)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(Nothing, selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t"" : ""C"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
            ' should match 2 but for that you need to use the hierarchical discriminator
        End Sub

        <Fact>
        Public Sub TestWhereBIsD()
            Dim query = From b In __collection.AsQueryable(Of B)()
                        Where TypeOf b Is D
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B b) => (b is D)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(Nothing, selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t"" : ""D"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBTypeEqualsB()
            Dim query = From b In __collection.AsQueryable(Of B)()
                        Where b.GetType().Equals(GetType(B))
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B b) => b.GetType().Equals(typeof(B))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(Nothing, selectQuery.OfType)
            ' OfType ignored because <T> was the same as <TDocument>
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBTypeEqualsC()
            Dim query = From b In __collection.AsQueryable(Of B)()
                        Where b.GetType().Equals(GetType(C))
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B b) => b.GetType().Equals(typeof(C))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(Nothing, selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t.0"" : { ""$exists"" : false }, ""_t"" : ""C"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBTypeEqualsD()
            Dim query = From b In __collection.AsQueryable(Of B)()
                        Where b.GetType().Equals(GetType(D))
                        Select b

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(B), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(B b) => b.GetType().Equals(typeof(D))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Equal(Nothing, selectQuery.OfType)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_t.0"" : { ""$exists"" : false }, ""_t"" : ""D"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
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