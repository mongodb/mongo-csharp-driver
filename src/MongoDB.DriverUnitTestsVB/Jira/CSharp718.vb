' Copyright 2010-2014 MongoDB Inc.
'
' Licensed under the Apache License, Version 2.0 (the "License");
' you may not use this file except in compliance with the License.
' You may obtain a copy of the License at
'
' http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.
'

Imports System.Linq
Imports MongoDB.Driver
Imports MongoDB.Driver.Linq
Imports NUnit.Framework

Namespace MongoDB.DriverUnitTests.Jira

    <TestFixture()>
    Public Class CSharp718

        Public Class C
            Public Id As Integer
            Public Foo() As Integer
        End Class

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection(Of C)

        <TestFixtureSetUp()>
        Public Sub Setup()
            _server = Configuration.TestServer
            _database = Configuration.TestDatabase
            _collection = Configuration.GetTestCollection(Of C)()
            TestSetup()
        End Sub

        <Test()>
        Public Sub TestLinqIsNothing()
            Dim postsWithFoo = (From d In _collection.AsQueryable(Of C)()
                Where d.Foo Is Nothing
                Select d).Count()
            Assert.AreEqual(2, postsWithFoo)
        End Sub

        <Test()>
        Public Sub TestLinqIsNotNothing()
            Dim postsWithFoo = (From d In _collection.AsQueryable(Of C)()
                Where d.Foo IsNot Nothing
                Select d).Count()
            Assert.AreEqual(3, postsWithFoo)
        End Sub

        Private Sub TestSetup()
            _collection.RemoveAll()
            _collection.Insert(New C() With { .Id = 1})
            _collection.Insert(New C() With {.Id = 2, .Foo = Nothing})
            _collection.Insert(New C() With {.Id = 3, .Foo = {1}})
            _collection.Insert(New C() With {.Id = 4, .Foo = {1, 2}})
            _collection.Insert(New C() With {.Id = 5, .Foo = {1, 2, 3}})
        End Sub
    End Class
End Namespace

