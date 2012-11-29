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
Imports System.Linq.Expressions
Imports System.Text
Imports NUnit.Framework

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Builders
Imports MongoDB.Driver.Linq

Namespace MongoDB.DriverUnitTests.Linq
    <TestFixture()> _
    Public Class MongoQueryableTests
        Private Class C
            Public Property Id() As ObjectId

            Public Property X() As Integer

            Public Property Y() As Integer
        End Class

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection

        <TestFixtureSetUp()> _
        Public Sub Setup()
            _server = Configuration.TestServer
            _server.Connect()
            _database = Configuration.TestDatabase
            _collection = Configuration.TestCollection
        End Sub

        <Test()> _
        Public Sub TestConstructorWithOneArgument()
            Dim provider = New MongoQueryProvider(_collection)
            Dim iqueryable = DirectCast(New MongoQueryable(Of C)(provider), IQueryable)
            Assert.AreSame(GetType(C), iqueryable.ElementType)
            Assert.AreSame(provider, iqueryable.Provider)
        End Sub

        <Test()> _
        Public Sub TestConstructorWithTwoArguments()
            Dim queryable = _collection.AsQueryable(Of C)()
            Dim iqueryable = DirectCast(New MongoQueryable(Of C)(DirectCast(queryable.Provider, MongoQueryProvider), queryable.Expression), IQueryable)
            Assert.AreSame(GetType(C), iqueryable.ElementType)
            Assert.AreSame(queryable.Provider, iqueryable.Provider)
            Assert.AreSame(queryable.Expression, iqueryable.Expression)
        End Sub
    End Class
End Namespace