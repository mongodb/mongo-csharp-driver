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
Imports System.Linq.Expressions
Imports System.Text
Imports Xunit

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Builders
Imports MongoDB.Driver.Linq
Imports MongoDB.Driver.Tests

Namespace MongoDB.Driver.VB.Tests.Linq
    Public Class MongoQueryProviderTests
        Private Class C
            Public Property Id() As ObjectId

            Public Property X() As Integer

            Public Property Y() As Integer
        End Class

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection

        Public Sub New()
            _server = LegacyTestConfiguration.Server
            _server.Connect()
            _database = LegacyTestConfiguration.Database
            _collection = LegacyTestConfiguration.Collection
        End Sub

        <Fact>
        Public Sub TestConstructor()
            Dim provider = New MongoQueryProvider(_collection)
        End Sub

        <Fact>
        Public Sub TestCreateQuery()
            Dim expression = _collection.AsQueryable(Of C)().Expression
            Dim provider = New MongoQueryProvider(_collection)
            Dim query = provider.CreateQuery(Of C)(expression)
            Assert.Same(GetType(C), query.ElementType)
            Assert.Same(provider, query.Provider)
            Assert.Same(expression, query.Expression)
        End Sub

        <Fact>
        Public Sub TestCreateQueryNonGeneric()
            Dim expression = _collection.AsQueryable(Of C)().Expression
            Dim provider = New MongoQueryProvider(_collection)
            Dim query = provider.CreateQuery(expression)
            Assert.Same(GetType(C), query.ElementType)
            Assert.Same(provider, query.Provider)
            Assert.Same(expression, query.Expression)
        End Sub
    End Class
End Namespace