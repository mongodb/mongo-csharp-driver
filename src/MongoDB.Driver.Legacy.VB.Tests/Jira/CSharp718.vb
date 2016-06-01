' Copyright 2010-2016 MongoDB Inc.
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
Imports Xunit

Namespace MongoDB.Driver.VB.Tests.Jira

    Public Class CSharp718

        Public Class C
            Public Id As Integer
            Public Foo() As Integer
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
            TestSetup()
            Return True
        End Function

        <Fact>
        Public Sub TestLinqIsNothing()
            Dim postsWithFoo = (From d In __collection.AsQueryable(Of C)()
                                Where d.Foo Is Nothing
                                Select d).Count()
            Assert.Equal(2, postsWithFoo)
        End Sub

        <Fact>
        Public Sub TestLinqIsNotNothing()
            Dim postsWithFoo = (From d In __collection.AsQueryable(Of C)()
                                Where d.Foo IsNot Nothing
                                Select d).Count()
            Assert.Equal(3, postsWithFoo)
        End Sub

        Private Shared Sub TestSetup()
            __collection.RemoveAll()
            __collection.Insert(New C() With {.Id = 1})
            __collection.Insert(New C() With {.Id = 2, .Foo = Nothing})
            __collection.Insert(New C() With {.Id = 3, .Foo = {1}})
            __collection.Insert(New C() With {.Id = 4, .Foo = {1, 2}})
            __collection.Insert(New C() With {.Id = 5, .Foo = {1, 2, 3}})
        End Sub
    End Class
End Namespace

