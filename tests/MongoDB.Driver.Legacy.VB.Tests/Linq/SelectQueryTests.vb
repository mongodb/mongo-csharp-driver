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
Imports System.Text.RegularExpressions
Imports Xunit

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Builders
Imports MongoDB.Driver.Linq
Imports MongoDB.Driver.Tests

Namespace MongoDB.Driver.VB.Tests.Linq
    Public Class SelectQueryTests
        Public Enum E
            None
            A
            B
            C
        End Enum

        Public Class C
            Public Property Id() As ObjectId

            <BsonElement("x")>
            Public Property X() As Integer
            <BsonElement("lx")>
            Public Property LX() As Long

            <BsonElement("y")>
            Public Property Y() As Integer

            <BsonElement("d")>
            Public Property D() As D
            <BsonElement("da")>
            Public Property DA() As List(Of D)
            <BsonElement("s")>
            <BsonIgnoreIfNull()>
            Public Property S() As String

            <BsonElement("a")>
            <BsonIgnoreIfNull()>
            Public Property A() As Integer()

            <BsonElement("b")>
            Public Property B() As Boolean

            <BsonElement("l")>
            <BsonIgnoreIfNull()>
            Public Property L() As List(Of Integer)

            <BsonElement("dbref")>
            <BsonIgnoreIfNull()>
            Public Property DBRef() As MongoDBRef

            <BsonElement("e")>
            <BsonIgnoreIfDefault()>
            <BsonRepresentation(BsonType.[String])>
            Public Property E() As E

            <BsonElement("ea")>
            <BsonIgnoreIfNull()>
            Public Property EA() As E()

            <BsonElement("sa")>
            <BsonIgnoreIfNull()>
            Public Property SA() As String()

            <BsonElement("ba")>
            <BsonIgnoreIfNull()>
            Public Property BA() As Boolean()
        End Class

        Public Class D
            <BsonElement("z")>
            Public Z As Integer
            ' use field instead of property to test fields also
            Public Overrides Function Equals(ByVal obj As Object) As Boolean
                If obj Is Nothing OrElse Not obj.GetType().Equals(GetType(D)) Then
                    Return False
                End If
                Return Z = DirectCast(obj, D).Z
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Z.GetHashCode()
            End Function

            Public Overrides Function ToString() As String
                Return String.Format("new D {{ Z = {0} }}", Z)
            End Function
        End Class

        ' used to test some query operators that have an IEqualityComparer parameter
        Private Class CEqualityComparer
            Implements IEqualityComparer(Of C)

            Public Function Equals1(ByVal x As C, ByVal y As C) As Boolean Implements IEqualityComparer(Of C).Equals
                Return x.Id.Equals(y.Id) AndAlso x.X.Equals(y.X) AndAlso x.Y.Equals(y.Y)
            End Function

            Public Function GetHashCode1(ByVal obj As C) As Integer Implements IEqualityComparer(Of C).GetHashCode
                Return obj.GetHashCode()
            End Function
        End Class

        ' used to test some query operators that have an IEqualityComparer parameter
        Private Class Int32EqualityComparer
            Implements IEqualityComparer(Of Integer)
            Public Function Equals1(ByVal x As Integer, ByVal y As Integer) As Boolean Implements IEqualityComparer(Of Integer).Equals
                Return x = y
            End Function

            Public Function GetHashCode1(ByVal obj As Integer) As Integer Implements IEqualityComparer(Of Integer).GetHashCode
                Return obj.GetHashCode()
            End Function
        End Class

        Private Shared __server As MongoServer
        Private Shared __database As MongoDatabase
        Private Shared __collection As MongoCollection(Of C)
        Private Shared __systemProfileCollection As MongoCollection(Of SystemProfileInfo)
        Private Shared __lazyOneTimeSetup As Lazy(Of Boolean) = New Lazy(Of Boolean)(OneTimeSetup)

        Private Shared __id1 As ObjectId
        Private Shared __id2 As ObjectId
        Private Shared __id3 As ObjectId
        Private Shared __id4 As ObjectId
        Private Shared __id5 As ObjectId

        Public Sub New()
            Dim x = __lazyOneTimeSetup.Value
        End Sub

        Private Shared Function OneTimeSetup() As Boolean
            __server = LegacyTestConfiguration.Server
            __server.Connect()
            __database = LegacyTestConfiguration.Database
            __collection = LegacyTestConfiguration.GetCollection(Of C)()
            __systemProfileCollection = __database.GetCollection(Of SystemProfileInfo)("system.profile")

            __id1 = ObjectId.GenerateNewId()
            __id2 = ObjectId.GenerateNewId()
            __id3 = ObjectId.GenerateNewId()
            __id4 = ObjectId.GenerateNewId()
            __id5 = ObjectId.GenerateNewId()

            ' documents inserted deliberately out of order to test sorting
            __collection.Drop()
            __collection.Insert(New C() With
            {
                 .Id = __id2,
                 .X = 2,
                 .LX = 2,
                 .Y = 11,
                 .D = New D() With {.Z = 22},
                 .A = {2, 3, 4},
                 .L = New List(Of Integer)({2, 3, 4})
            })
            __collection.Insert(New C() With
            {
                 .Id = __id1,
                 .X = 1,
                 .LX = 1,
                 .Y = 11,
                 .D = New D() With {.Z = 11},
                 .S = "abc",
                 .SA = {"Tom", "Dick", "Harry"}
            })
            __collection.Insert(New C() With
            {
                 .Id = __id3,
                 .X = 3,
                 .LX = 3,
                 .Y = 33,
                 .D = New D() With {.Z = 33},
                 .B = True,
                 .BA = {True},
                 .E = E.A,
                 .EA = New E() {E.A, E.B}
            })
            __collection.Insert(New C() With
            {
                 .Id = __id5,
                 .X = 5,
                 .LX = 5,
                 .Y = 44,
                 .D = New D() With {.Z = 55},
                .DBRef = New MongoDBRef("db", "c", 1)
            })
            __collection.Insert(New C() With
            {
                 .Id = __id4,
                 .X = 4,
                 .LX = 4,
                 .Y = 44,
                 .D = New D() With {.Z = 44},
                 .DA = {New D() With {.Z = 333}}.ToList,
                .S = "   xyz   "
            })

            Return True
        End Function

        <Fact>
        Public Sub TestAggregate()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Aggregate(Function(a, b) Nothing)
                End Sub)

            Dim expectedMessage = "The Aggregate query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAggregateWithAccumulator()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Aggregate(0, Function(a, c) 0)
                End Sub)

            Dim expectedMessage = "The Aggregate query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAggregateWithAccumulatorAndSelector()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Aggregate(0, Function(a, c) 0, Function(a) a)
                End Sub)

            Dim expectedMessage = "The Aggregate query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAll()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).All(Function(c) True)
                End Sub)

            Dim expectedMessage = "The All query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAny()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Any()
            Assert.True(result)
        End Sub

        <Fact>
        Public Sub TestAnyWhereXEquals1()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).Any()
            Assert.True(result)
        End Sub

        <Fact>
        Public Sub TestAnyWhereXEquals9()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).Any()
            Assert.False(result)
        End Sub

        <Fact>
        Public Sub TestAnyWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).Any(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "Any with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAnyWithPredicateAfterWhere()
            Dim result = __collection.AsQueryable(Of C)().Where(Function(c) c.X = 1).Any(Function(c) c.Y = 11)
            Assert.True(result)
        End Sub

        <Fact>
        Public Sub TestAnyWithPredicateFalse()
            Dim result = __collection.AsQueryable(Of C)().Any(Function(c) c.X = 9)
            Assert.False(result)
        End Sub

        <Fact>
        Public Sub TestAnyWithPredicateTrue()
            Dim result = __collection.AsQueryable(Of C)().Any(Function(c) c.X = 1)
            Assert.True(result)
        End Sub

        <Fact>
        Public Sub TestAsQueryableWithNothingElse()
            Dim query = __collection.AsQueryable(Of C)()
            Dim result = query.ToList()
            Assert.Equal(5, result.Count)
        End Sub

        <Fact>
        Public Sub TestAverage()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select 1.0).Average()
                End Sub)

            Dim expectedMessage = "The Average query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAverageNullable()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select New Nullable(Of Decimal)(1.0)).Average()
                End Sub)

            Dim expectedMessage = "The Average query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAverageWithSelector()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Average(Function(c) 1.0)
                End Sub)

            Dim expectedMessage = "The Average query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestAverageWithSelectorNullable()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Average(Function(c) New Nullable(Of Decimal)(1.0))
                End Sub)

            Dim expectedMessage = "The Average query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestCast()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Cast(Of C)()
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Cast query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestConcat()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Concat(source2)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Concat query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestContains()
            Dim item = New C()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Contains(item)
                End Sub)

            Dim expectedMessage = "The Contains query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestContainsWithEqualityComparer()
            Dim item = New C()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Contains(item, New CEqualityComparer())
                End Sub)

            Dim expectedMessage = "The Contains query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestCountEquals2()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).Count()

            Assert.Equal(2, result)
        End Sub

        <Fact>
        Public Sub TestCountEquals5()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Count()

            Assert.Equal(5, result)
        End Sub

        <Fact>
        Public Sub TestCountWithPredicate()
            Dim result = __collection.AsQueryable(Of C)().Count(Function(c) c.Y = 11)

            Assert.Equal(2, result)
        End Sub

        <Fact>
        Public Sub TestCountWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).Count(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "Count with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestCountWithPredicateAfterWhere()
            Dim result = __collection.AsQueryable(Of C)().Where(Function(c) c.X = 1).Count(Function(c) c.Y = 11)

            Assert.Equal(1, result)
        End Sub

        <Fact>
        Public Sub TestCountWithSkipAndTake()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Skip(2).Take(2).Count()

            Assert.Equal(2, result)
        End Sub

        <Fact>
        Public Sub TestDefaultIfEmpty()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).DefaultIfEmpty()
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The DefaultIfEmpty query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestDefaultIfEmptyWithDefaultValue()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).DefaultIfEmpty(Nothing)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The DefaultIfEmpty query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestDistinctASub0()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.A(0)).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains(2))
        End Sub

        <Fact>
        Public Sub TestDistinctB()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.B).Distinct()
            Dim results = query.ToList()
            Assert.Equal(2, results.Count)
            Assert.True(results.Contains(False))
            Assert.True(results.Contains(True))
        End Sub

        <Fact>
        Public Sub TestDistinctBASub0()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.BA(0)).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains(True))
        End Sub

        <Fact>
        Public Sub TestDistinctD()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.D).Distinct()
            Dim results = query.ToList()
            ' execute query
            Assert.Equal(5, results.Count)
            Assert.True(results.Contains(New D() With {
             .Z = 11
            }))
            Assert.True(results.Contains(New D() With {
             .Z = 22
            }))
            Assert.True(results.Contains(New D() With {
             .Z = 33
            }))
            Assert.True(results.Contains(New D() With {
             .Z = 44
            }))
            Assert.True(results.Contains(New D() With {
             .Z = 55
            }))
        End Sub

        <Fact>
        Public Sub TestDistinctDBRef()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.DBRef).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains(New MongoDBRef("db", "c", 1)))
        End Sub

        <Fact>
        Public Sub TestDistinctDBRefDatabase()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.DBRef.DatabaseName).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains("db"))
        End Sub

        <Fact>
        Public Sub TestDistinctDZ()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.D.Z).Distinct()
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.True(results.Contains(11))
            Assert.True(results.Contains(22))
            Assert.True(results.Contains(33))
            Assert.True(results.Contains(44))
            Assert.True(results.Contains(55))
        End Sub

        <Fact>
        Public Sub TestDistinctE()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.E).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains(E.A))
        End Sub

        <Fact>
        Public Sub TestDistinctEASub0()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.EA(0)).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains(E.A))
        End Sub

        <Fact>
        Public Sub TestDistinctId()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.Id).Distinct()
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.True(results.Contains(__id1))
            Assert.True(results.Contains(__id2))
            Assert.True(results.Contains(__id3))
            Assert.True(results.Contains(__id4))
            Assert.True(results.Contains(__id5))
        End Sub

        <Fact>
        Public Sub TestDistinctLSub0()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.L(0)).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains(2))
        End Sub

        <Fact>
        Public Sub TestDistinctS()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.S).Distinct()
            Dim results = query.ToList()
            Assert.Equal(2, results.Count)
            Assert.True(results.Contains("abc"))
            Assert.True(results.Contains("   xyz   "))
        End Sub

        <Fact>
        Public Sub TestDistinctSASub0()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.SA(0)).Distinct()
            Dim results = query.ToList()
            Assert.Equal(1, results.Count)
            Assert.True(results.Contains("Tom"))
        End Sub

        <Fact>
        Public Sub TestDistinctX()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.X).Distinct()
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.True(results.Contains(1))
            Assert.True(results.Contains(2))
            Assert.True(results.Contains(3))
            Assert.True(results.Contains(4))
            Assert.True(results.Contains(5))
        End Sub

        <Fact>
        Public Sub TestDistinctXWithQuery()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Where c.X > 3
                         Select c.X).Distinct()
            Dim results = query.ToList()
            Assert.Equal(2, results.Count)
            Assert.True(results.Contains(4))
            Assert.True(results.Contains(5))
        End Sub

        <Fact>
        Public Sub TestDistinctY()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c.Y).Distinct()
            Dim results = query.ToList()
            Assert.Equal(3, results.Count)
            Assert.True(results.Contains(11))
            Assert.True(results.Contains(33))
            Assert.True(results.Contains(44))
        End Sub

        <Fact>
        Public Sub TestDistinctWithEqualityComparer()
            Dim query = __collection.AsQueryable(Of C)().Distinct(New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The version of the Distinct query operator with an equality comparer is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestElementAtOrDefaultWithManyMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).ElementAtOrDefault(2)

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestElementAtOrDefaultWithNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).ElementAtOrDefault(0)
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestElementAtOrDefaultWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).ElementAtOrDefault(0)

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestElementAtOrDefaultWithTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).ElementAtOrDefault(1)

            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestElementAtWithManyMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).ElementAt(2)

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestElementAtWithNoMatch()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Where c.X = 9
                                  Select c).ElementAt(0)
                End Sub)

            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestElementAtWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).ElementAt(0)

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestElementAtWithTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).ElementAt(1)

            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestExcept()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Except(source2)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Except query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestExceptWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Except(source2, New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Except query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithManyMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault()

            Assert.Equal(2, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).FirstOrDefault()
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).FirstOrDefault()

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).FirstOrDefault(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "FirstOrDefault with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithPredicateAfterWhere()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).FirstOrDefault(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithPredicateNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault(Function(c) c.X = 9)
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithPredicateOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault(Function(c) c.X = 3)
            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithPredicateTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault(Function(c) c.Y = 11)
            Assert.Equal(2, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstOrDefaultWithTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).FirstOrDefault()

            Assert.Equal(2, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstWithManyMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).First()

            Assert.Equal(2, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstWithNoMatch()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Where c.X = 9
                                  Select c).First()
                End Sub)

            Dim expectedMessage = ""
            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestFirstWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).First()

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).First(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "First with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestFirstWithPredicateAfterWhere()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).First(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstWithPredicateNoMatch()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In __collection.AsQueryable(Of C)()
                                                                                   Select c).First(Function(c) c.X = 9)
                                                                 End Sub)
            Assert.Equal(ExpectedErrorMessage.FirstEmptySequence, ex.Message)
        End Sub

        <Fact>
        Public Sub TestFirstWithPredicateOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).First(Function(c) c.X = 3)
            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstWithPredicateTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).First(Function(c) c.Y = 11)
            Assert.Equal(2, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestFirstWithTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).First()

            Assert.Equal(2, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelector()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndElementSelector()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndElementSelectorAndEqualityComparer()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c, New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndElementSelectorAndResultSelector()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c, Function(c, e) 1.0)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndElementSelectorAndResultSelectorAndEqualityComparer()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c, Function(c, e) e.First(), New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndEqualityComparer()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndResultSelector()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(k, e) 1.0)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupByWithKeySelectorAndResultSelectorAndEqualityComparer()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(k, e) e.First(), New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupBy query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupJoin()
            Dim inner = New C(-1) {}
            Dim query = __collection.AsQueryable(Of C)().GroupJoin(inner, Function(c) c, Function(c) c, Function(c, e) c)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupJoin query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestGroupJoinWithEqualityComparer()
            Dim inner = New C(-1) {}
            Dim query = __collection.AsQueryable(Of C)().GroupJoin(inner, Function(c) c, Function(c) c, Function(c, e) c, New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The GroupJoin query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestIntersect()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Intersect(source2)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Intersect query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestIntersectWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Intersect(source2, New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Intersect query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestJoin()
            Dim query = __collection.AsQueryable(Of C)().Join(__collection.AsQueryable(Of C)(), Function(c) c.X, Function(c) c.X, Function(x, y) x)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Join query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestJoinWithEqualityComparer()
            Dim query = __collection.AsQueryable(Of C)().Join(__collection.AsQueryable(Of C)(), Function(c) c.X, Function(c) c.X, Function(x, y) x, New Int32EqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Join query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithManyMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).LastOrDefault()

            Assert.Equal(4, result.X)
            Assert.Equal(44, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).LastOrDefault()
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).LastOrDefault()

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithOrderBy()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Order By c.X
                          Select c).LastOrDefault()

            Assert.Equal(5, result.X)
            Assert.Equal(44, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).LastOrDefault(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "LastOrDefault with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithPredicateAfterWhere()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).LastOrDefault(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithPredicateNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).LastOrDefault(Function(c) c.X = 9)
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithPredicateOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).LastOrDefault(Function(c) c.X = 3)
            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithPredicateTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).LastOrDefault(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastOrDefaultWithTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).LastOrDefault()

            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithManyMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Last()

            Assert.Equal(4, result.X)
            Assert.Equal(44, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithNoMatch()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Where c.X = 9
                                  Select c).Last()
                End Sub)

            Dim expectedMessage = ""
            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestLastWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).Last()

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().Select(Function(c) c.Y).Last(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "Last with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestLastWithPredicateAfterWhere()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).Last(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithPredicateNoMatch()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In __collection.AsQueryable(Of C)()
                                                                                   Select c).Last(Function(c) c.X = 9)
                                                                 End Sub)
            Assert.Equal(ExpectedErrorMessage.LastEmptySequence, ex.Message)
        End Sub

        <Fact>
        Public Sub TestLastWithPredicateOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Last(Function(c) c.X = 3)
            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithPredicateTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Last(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithOrderBy()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Order By c.X
                          Select c).Last()

            Assert.Equal(5, result.X)
            Assert.Equal(44, result.Y)
        End Sub

        <Fact>
        Public Sub TestLastWithTwoMatches()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).Last()

            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestLongCountEquals2()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).LongCount()

            Assert.Equal(2L, result)
        End Sub

        <Fact>
        Public Sub TestLongCountEquals5()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).LongCount()

            Assert.Equal(5L, result)
        End Sub

        <Fact>
        Public Sub TestLongCountWithSkipAndTake()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Skip(2).Take(2).LongCount()

            Assert.Equal(2L, result)
        End Sub

        <Fact>
        Public Sub TestMaxDZWithProjection()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c.D.Z).Max()
            Assert.Equal(55, result)
        End Sub

        <Fact>
        Public Sub TestMaxDZWithSelector()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Max(Function(c) c.D.Z)
            Assert.Equal(55, result)
        End Sub

        <Fact>
        Public Sub TestMaxXWithProjection()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c.X).Max()
            Assert.Equal(5, result)
        End Sub

        <Fact>
        Public Sub TestMaxXWithSelector()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Max(Function(c) c.X)
            Assert.Equal(5, result)
        End Sub

        <Fact>
        Public Sub TestMaxXYWithProjection()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select New With {c.X, c.Y}).Max()
            Assert.Equal(5, result.X)
            Assert.Equal(44, result.Y)
        End Sub

        <Fact>
        Public Sub TestMaxXYWithSelector()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Max(Function(c) New With
                          {
                             c.X,
                             c.Y
                          })
            Assert.Equal(5, result.X)
            Assert.Equal(44, result.Y)
        End Sub

        <Fact>
        Public Sub TestMinDZWithProjection()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c.D.Z).Min()
            Assert.Equal(11, result)
        End Sub

        <Fact>
        Public Sub TestMinDZWithSelector()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Min(Function(c) c.D.Z)
            Assert.Equal(11, result)
        End Sub

        <Fact>
        Public Sub TestMinXWithProjection()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c.X).Min()
            Assert.Equal(1, result)
        End Sub

        <Fact>
        Public Sub TestMinXWithSelector()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Min(Function(c) c.X)
            Assert.Equal(1, result)
        End Sub

        <Fact>
        Public Sub TestMinXYWithProjection()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select New With {c.X, c.Y}).Min()
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestMinXYWithSelector()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).Min(Function(c) New With
                          {
                             c.X,
                             c.Y
                          })
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestOrderByValueTypeWithObjectReturnType()
            Dim orderByClause As Expression(Of Func(Of C, Object)) = Function(c) c.LX
            Dim query = __collection.AsQueryable(Of C)().OrderBy(orderByClause)

            RunTestOrderByValueTypeWithMismatchingType(query, "(C c) => (Object)c.LX")
        End Sub

        <Fact>
        Public Sub TestOrderByValueTypeWithIComparableReturnType()
            Dim orderByClause As Expression(Of Func(Of C, IComparable)) = Function(c) c.LX
            Dim query = __collection.AsQueryable(Of C)().OrderBy(orderByClause)

            RunTestOrderByValueTypeWithMismatchingType(query, "(C c) => (IComparable)c.LX")
        End Sub

        Sub RunTestOrderByValueTypeWithMismatchingType(ByVal query As IOrderedQueryable, ByVal orderByString As String)
            Dim mongoQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(mongoQuery)
            Dim selectQuery As SelectQuery = mongoQuery
            Assert.Equal(orderByString, ExpressionFormatter.ToString(selectQuery.OrderBy(0).Key))
        End Sub

        <Fact>
        Public Sub TestOrderByAscending()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(1, selectQuery.OrderBy.Count)

            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy(0).Direction)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(1, results.First().X)
            Assert.Equal(5, results.Last().X)
        End Sub

        <Fact>
        Public Sub TestOrderByAscendingThenByAscending()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.Y, c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(2, selectQuery.OrderBy.Count)

            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy(0).Direction)

            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy(1).Direction)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(1, results.First().X)
            Assert.Equal(5, results.Last().X)
        End Sub

        <Fact>
        Public Sub TestOrderByAscendingThenByDescending()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.Y, c.X Descending
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(2, selectQuery.OrderBy.Count)

            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy(0).Direction)

            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy(1).Direction)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(2, results.First().X)
            Assert.Equal(4, results.Last().X)
        End Sub

        <Fact>
        Public Sub TestOrderByDescending()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.X Descending
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(1, selectQuery.OrderBy.Count)

            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy(0).Direction)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(5, results.First().X)
            Assert.Equal(1, results.Last().X)
        End Sub

        <Fact>
        Public Sub TestOrderByDescendingThenByAscending()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.Y Descending, c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(2, selectQuery.OrderBy.Count)

            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy(0).Direction)

            Assert.Equal(OrderByDirection.Ascending, selectQuery.OrderBy(1).Direction)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(4, results.First().X)
            Assert.Equal(2, results.Last().X)
        End Sub

        <Fact>
        Public Sub TestOrderByDescendingThenByDescending()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.Y Descending, c.X Descending
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Equal(2, selectQuery.OrderBy.Count)

            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy(0).Direction)

            Assert.Equal(OrderByDirection.Descending, selectQuery.OrderBy(1).Direction)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(5, results.First().X)
            Assert.Equal(1, results.Last().X)
        End Sub

        <Fact>
        Public Sub TestOrderByDuplicate()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Order By c.X
                        Order By c.Y
                        Select c

            Dim exception = Record.Exception(
                Sub()
                    MongoQueryTranslator.Translate(query)
                End Sub)

            Dim expectedMessage = "Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses)."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestProjection()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Select c.X

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Null(selectQuery.OrderBy)

            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())

            Dim results = query.ToList()
            Assert.Equal(5, results.Count)
            Assert.Equal(2, results.First())
            Assert.Equal(4, results.Last())
        End Sub

        <Fact>
        Public Sub TestReverse()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Reverse()
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Reverse query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSelect()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestSelectMany()
            Dim query = __collection.AsQueryable(Of C)().SelectMany(Function(c) New C() {c})
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The SelectMany query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSelectManyWithIndex()
            Dim query = __collection.AsQueryable(Of C)().SelectMany(Function(c, index) New C() {c})
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The SelectMany query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSelectManyWithIntermediateResults()
            Dim query = __collection.AsQueryable(Of C)().SelectMany(Function(c) New C() {c}, Function(c, i) i)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The SelectMany query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSelectManyWithIndexAndIntermediateResults()
            Dim query = __collection.AsQueryable(Of C)().SelectMany(Function(c, index) New C() {c}, Function(c, i) i)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The SelectMany query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSelectWithIndex()
            Dim query = __collection.AsQueryable(Of C)().[Select](Function(c, index) c)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The indexed version of the Select query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSelectWithNothingElse()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Select c
            Dim result = query.ToList()
            Assert.Equal(5, result.Count)
        End Sub

        <Fact>
        Public Sub TestSequenceEqual()
            Dim source2 = New C(-1) {}
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).SequenceEqual(source2)
                End Sub)

            Dim expectedMessage = "The SequenceEqual query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSequenceEqualtWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).SequenceEqual(source2, New CEqualityComparer())
                End Sub)

            Dim expectedMessage = "The SequenceEqual query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithManyMatches()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).SingleOrDefault()
                End Sub)

            Dim expectedMessage = ""
            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).SingleOrDefault()
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).SingleOrDefault()

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).SingleOrDefault(Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "SingleOrDefault with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithPredicateAfterWhere()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).SingleOrDefault(Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithPredicateNoMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).SingleOrDefault(Function(c) c.X = 9)
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithPredicateOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).SingleOrDefault(Function(c) c.X = 3)
            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithPredicateTwoMatches()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In __collection.AsQueryable(Of C)()
                                                                                   Select c).SingleOrDefault(Function(c) c.Y = 11)
                                                                 End Sub)
            Assert.Equal(ExpectedErrorMessage.SingleLongSequence, ex.Message)
        End Sub

        <Fact>
        Public Sub TestSingleOrDefaultWithTwoMatches()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Where c.Y = 11
                                  Select c).SingleOrDefault()
                End Sub)

            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestSingleWithManyMatches()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).[Single]()
                End Sub)

            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestSingleWithNoMatch()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Where c.X = 9
                                  Select c).[Single]()
                End Sub)

            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestSingleWithOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).[Single]()

            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestSingleWithPredicateAfterProjection()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = __collection.AsQueryable(Of C)().[Select](Function(c) c.Y).[Single](Function(y) y = 11)
                End Sub)

            Dim expectedMessage = "Single with predicate after a projection is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSingleWithPredicateAfterWhere()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).[Single](Function(c) c.Y = 11)
            Assert.Equal(1, result.X)
            Assert.Equal(11, result.Y)
        End Sub

        <Fact>
        Public Sub TestSingleWithPredicateNoMatch()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In __collection.AsQueryable(Of C)()
                                                                                   Select c).[Single](Function(c) c.X = 9)
                                                                 End Sub)
            Assert.Equal(ExpectedErrorMessage.SingleEmptySequence, ex.Message)
        End Sub

        <Fact>
        Public Sub TestSingleWithPredicateOneMatch()
            Dim result = (From c In __collection.AsQueryable(Of C)()
                          Select c).[Single](Function(c) c.X = 3)
            Assert.Equal(3, result.X)
            Assert.Equal(33, result.Y)
        End Sub

        <Fact>
        Public Sub TestSingleWithPredicateTwoMatches()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In __collection.AsQueryable(Of C)()
                                                                                   Select c).[Single](Function(c) c.Y = 11)
                                                                 End Sub)
            Assert.Equal(ExpectedErrorMessage.SingleLongSequence, ex.Message)
        End Sub

        <Fact>
        Public Sub TestSingleWithTwoMatches()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Where c.Y = 11
                                  Select c).[Single]()
                End Sub)

            Assert.IsType(Of InvalidOperationException)(exception)
        End Sub

        <Fact>
        Public Sub TestSkip2()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Skip(2)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Equal(2, selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestSkipWhile()
            Dim query = __collection.AsQueryable(Of C)().SkipWhile(Function(c) True)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The SkipWhile query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSum()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select 1.0).Sum()
                End Sub)

            Dim expectedMessage = "The Sum query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSumNullable()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select New Nullable(Of Integer)(1)).Sum()
                End Sub)

            Dim expectedMessage = "The Sum query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSumWithSelector()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Sum(Function(c) 1.0)
                End Sub)

            Dim expectedMessage = "The Sum query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestSumWithSelectorNullable()
            Dim exception = Record.Exception(
                Sub()
                    Dim result = (From c In __collection.AsQueryable(Of C)()
                                  Select c).Sum(Function(c) New Nullable(Of Decimal)(1.0))
                End Sub)

            Dim expectedMessage = "The Sum query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestTake2()
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Take(2)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Null(selectQuery.Where)
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Equal(2, selectQuery.Take)

            Assert.Null(selectQuery.BuildQuery())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestTakeWhile()
            Dim query = __collection.AsQueryable(Of C)().TakeWhile(Function(c) True)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The TakeWhile query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestThenByWithMissingOrderBy()
            ' not sure this could ever happen in real life without deliberate sabotaging like with this cast
            Dim query = DirectCast(__collection.AsQueryable(Of C)(), IOrderedQueryable(Of C)).ThenBy(Function(c) c.X)

            Dim exception = Record.Exception(
                Sub()
                    MongoQueryTranslator.Translate(query)
                End Sub)

            Dim expectedMessage = "ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestUnion()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Union(source2)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Union query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestUnionWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim query = (From c In __collection.AsQueryable(Of C)()
                         Select c).Union(source2, New CEqualityComparer())
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The Union query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestWhereAAny()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.Any()
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

            Assert.Equal("{ ""a"" : { ""$ne"" : null, ""$not"" : { ""$size"" : 0 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAAnyWithPredicate()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.Any(Function(a) a > 3)
                        Select c
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "Any is only support for items that serialize into documents. The current serializer is Int32Serializer and must implement IBsonDocumentSerializer for participation in Any queries."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
        End Sub

        <Fact>
        Public Sub TestWhereLocalListContainsX()
            Dim local = New List(Of Integer)() From {
             1,
             2,
             3
            }

            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where local.Contains(c.X)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => System.Collections.Generic.List`1[System.Int32].Contains(c.X)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : { ""$in"" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLocalArrayContainsX()
            Dim local = {1, 2, 3}

            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where local.Contains(c.X)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => Enumerable.Contains<Int32>(Int32[]:{ 1, 2, 3 }, c.X)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : { ""$in"" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub


        <Fact>
        Public Sub TestWhereAContains2()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.Contains(2)
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

            Assert.Equal("{ ""a"" : 2 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAContains2Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.A.Contains(2)
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

            Assert.Equal("{ ""a"" : { ""$ne"" : 2 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAContainsAll()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.ContainsAll({2, 3})
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

            Assert.Equal("{ ""a"" : { ""$all"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAContainsAllNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.A.ContainsAll({2, 3})
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

            Assert.Equal("{ ""a"" : { ""$not"" : { ""$all"" : [2, 3] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAContainsAny()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.ContainsAny({2, 3})
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

            Assert.Equal("{ ""a"" : { ""$in"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAContainsAnyNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.A.ContainsAny({1, 2})
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

            Assert.Equal("{ ""a"" : { ""$nin"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereAExistsFalse()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Query.NotExists("a").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""a"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereAExistsTrue()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Query.Exists("a").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""a"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereAExistsTrueNot()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Not Query.Exists("a").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""a"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereALengthEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.Length = 3
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

            Assert.Equal("{ ""a"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereALengthEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.A.Length = 3)
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

            Assert.Equal("{ ""a"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereALengthEquals3Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 3 = c.A.Length
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

            Assert.Equal("{ ""a"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereALengthNotEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A.Length <> 3
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

            Assert.Equal("{ ""a"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereALengthNotEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.A.Length <> 3)
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

            Assert.Equal("{ ""a"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1Equals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A(1) = 3
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

            Assert.Equal("{ ""a.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1Equals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.A(1) = 3)
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

            Assert.Equal("{ ""a.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1ModTwoEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A(1) Mod 2 = 1
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

            Assert.Equal("{ ""a.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1ModTwoEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.A(1) Mod 2 = 1)
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

            Assert.Equal("{ ""a.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1ModTwoNotEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A(1) Mod 2 <> 1
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

            Assert.Equal("{ ""a.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1ModTwoNotEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.A(1) Mod 2 <> 1)
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

            Assert.Equal("{ ""a.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1NotEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.A(1) <> 3
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

            Assert.Equal("{ ""a.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereASub1NotEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.A(1) <> 3)
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

            Assert.Equal("{ ""a.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereB()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.B
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

            Assert.Equal("{ ""b"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBASub0()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.BA(0)
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

            Assert.Equal("{ ""ba.0"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBASub0EqualsFalse()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.BA(0) = False
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

            Assert.Equal("{ ""ba.0"" : false }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBASub0EqualsFalseNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.BA(0) = False)
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

            Assert.Equal("{ ""ba.0"" : { ""$ne"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBASub0EqualsTrue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.BA(0) = True
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

            Assert.Equal("{ ""ba.0"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBASub0EqualsTrueNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.BA(0) = True)
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

            Assert.Equal("{ ""ba.0"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBASub0Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.BA(0)
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

            Assert.Equal("{ ""ba.0"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBEqualsFalse()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.B = False
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

            Assert.Equal("{ ""b"" : false }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBEqualsFalseNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.B = False)
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

            Assert.Equal("{ ""b"" : { ""$ne"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBEqualsTrue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.B = True
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

            Assert.Equal("{ ""b"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBEqualsTrueNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.B = True)
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

            Assert.Equal("{ ""b"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereBNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.B
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

            Assert.Equal("{ ""b"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDAAnyWithPredicate()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.DA.Any(Function(x) x.Z = 333)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => Enumerable.Any<D>((IEnumerable<D>)c.DA, (D x) => (x.Z == 333))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""da"" : { ""$elemMatch"" : { ""z"" : 333 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub


        <Fact>
        Public Sub TestWhereDBRefCollectionNameEqualsC()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.DBRef.CollectionName = "c"
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

            Assert.Equal("{ ""dbref.$ref"" : ""c"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDBRefDatabaseNameEqualsDb()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.DBRef.DatabaseName = "db"
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

            Assert.Equal("{ ""dbref.$db"" : ""db"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDBRefEquals()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.DBRef = New MongoDBRef("db", "c", 1)
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

            Assert.Equal("{ ""dbref"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDBRefEqualsNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.DBRef = New MongoDBRef("db", "c", 1))
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

            Assert.Equal("{ ""dbref"" : { ""$ne"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDBRefNotEquals()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.DBRef <> New MongoDBRef("db", "c", 1)
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

            Assert.Equal("{ ""dbref"" : { ""$ne"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDBRefNotEqualsNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.DBRef <> New MongoDBRef("db", "c", 1))
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

            Assert.Equal("{ ""dbref"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDBRefIdEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.DBRef.Id = 1
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

            Assert.Equal("{ ""dbref.$id"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDEquals11()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.D.Equals(New D() With {
                         .Z = 11
                        })
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

            Assert.Equal("{ ""d"" : { ""z"" : 11 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDEquals11Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.D.Equals(New D() With {
                         .Z = 11
                        }))
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

            Assert.Equal("{ ""d"" : { ""$ne"" : { ""z"" : 11 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDNotEquals11()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.D.Equals(New D() With {
                         .Z = 11
                        })
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

            Assert.Equal("{ ""d"" : { ""$ne"" : { ""z"" : 11 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereDNotEquals11Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (Not c.D.Equals(New D() With {
                         .Z = 11
                        }))
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

            Assert.Equal("{ ""d"" : { ""z"" : 11 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEAContainsAll()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.EA.ContainsAll(New E() {E.A, E.B})
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

            Assert.Equal("{ ""ea"" : { ""$all"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEAContainsAllNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.EA.ContainsAll(New E() {E.A, E.B})
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

            Assert.Equal("{ ""ea"" : { ""$not"" : { ""$all"" : [1, 2] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEAContainsAny()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.EA.ContainsAny({E.A, E.B})
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

            Assert.Equal("{ ""ea"" : { ""$in"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEAContainsAnyNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.EA.ContainsAny({E.A, E.B})
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

            Assert.Equal("{ ""ea"" : { ""$nin"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEAContainsB()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.EA.Contains(E.B)
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

            Assert.Equal("{ ""ea"" : 2 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEAContainsBNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.EA.Contains(E.B)
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

            Assert.Equal("{ ""ea"" : { ""$ne"" : 2 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEASub0EqualsA()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.EA(0) = E.A
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

            Assert.Equal("{ ""ea.0"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEASub0EqualsANot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.EA(0) = E.A)
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

            Assert.Equal("{ ""ea.0"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEASub0NotEqualsA()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.EA(0) <> E.A
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

            Assert.Equal("{ ""ea.0"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEASub0NotEqualsANot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.EA(0) <> E.A)
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

            Assert.Equal("{ ""ea.0"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

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
        Public Sub TestWhereEEqualsANot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.E = E.A)
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

            Assert.Equal("{ ""e"" : { ""$ne"" : ""A"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEEqualsAReversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where E.A = c.E
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
        Public Sub TestWhereEInAOrB()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.E.In({E.A, E.B})
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

            Assert.Equal("{ ""e"" : { ""$in"" : [""A"", ""B""] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereEInAOrBNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.E.In({E.A, E.B})
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

            Assert.Equal("{ ""e"" : { ""$nin"" : [""A"", ""B""] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereENotEqualsA()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.E <> E.A
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

            Assert.Equal("{ ""e"" : { ""$ne"" : ""A"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereENotEqualsANot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.E <> E.A)
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
        Public Sub TestWhereLContains2()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L.Contains(2)
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

            Assert.Equal("{ ""l"" : 2 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLContains2Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.L.Contains(2)
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

            Assert.Equal("{ ""l"" : { ""$ne"" : 2 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLContainsAll()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L.ContainsAll({2, 3})
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

            Assert.Equal("{ ""l"" : { ""$all"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLContainsAllNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.L.ContainsAll({2, 3})
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

            Assert.Equal("{ ""l"" : { ""$not"" : { ""$all"" : [2, 3] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLContainsAny()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L.ContainsAny({2, 3})
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

            Assert.Equal("{ ""l"" : { ""$in"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLContainsAnyNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.L.ContainsAny({1, 2})
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

            Assert.Equal("{ ""l"" : { ""$nin"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLExistsFalse()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Query.NotExists("l").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""l"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereLExistsTrue()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Query.Exists("l").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""l"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereLExistsTrueNot()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Not Query.Exists("l").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""l"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereLCountMethodEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L.Count() = 3
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

            Assert.Equal("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountMethodEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L.Count() = 3)
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

            Assert.Equal("{ ""l"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountMethodEquals3Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 3 = c.L.Count()
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

            Assert.Equal("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountPropertyEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L.Count = 3
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

            Assert.Equal("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountPropertyEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L.Count = 3)
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

            Assert.Equal("{ ""l"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountPropertyEquals3Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 3 = c.L.Count
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

            Assert.Equal("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountPropertyNotEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L.Count <> 3
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

            Assert.Equal("{ ""l"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLCountPropertyNotEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L.Count <> 3)
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

            Assert.Equal("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1Equals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L(1) = 3
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

            Assert.Equal("{ ""l.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1Equals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L(1) = 3)
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

            Assert.Equal("{ ""l.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1ModTwoEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L(1) Mod 2 = 1
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

            Assert.Equal("{ ""l.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1ModTwoEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L(1) Mod 2 = 1)
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

            Assert.Equal("{ ""l.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1ModTwoNotEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L(1) Mod 2 <> 1
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

            Assert.Equal("{ ""l.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1ModTwoNotEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L(1) Mod 2 <> 1)
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

            Assert.Equal("{ ""l.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1NotEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.L(1) <> 3
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

            Assert.Equal("{ ""l.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLSub1NotEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.L(1) <> 3)
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

            Assert.Equal("{ ""l.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLXModTwoEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.LX Mod 2 = 1
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

            Assert.Equal("{ ""lx"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLXModTwoEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.LX Mod 2 = 1)
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

            Assert.Equal("{ ""lx"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLXModTwoEquals1Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 1 = c.LX Mod 2
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

            Assert.Equal("{ ""lx"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLXModTwoNotEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.LX Mod 2 <> 1
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

            Assert.Equal("{ ""lx"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereLXModTwoNotEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.LX Mod 2 <> 1)
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

            Assert.Equal("{ ""lx"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0ContainsO()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.SA(0).Contains("o")
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

            Assert.Equal("{ ""sa.0"" : /o/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0ContainsONot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.SA(0).Contains("o")
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

            Assert.Equal("{ ""sa.0"" : { ""$not"" : /o/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0EndsWithM()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.SA(0).EndsWith("m")
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

            Assert.Equal("{ ""sa.0"" : /m$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0EndsWithMNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.SA(0).EndsWith("m")
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

            Assert.Equal("{ ""sa.0"" : { ""$not"" : /m$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0IsMatch()
            Dim regex = New Regex("^T")
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where regex.IsMatch(c.SA(0))
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

            Assert.Equal("{ ""sa.0"" : /^T/ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0IsMatchNot()
            Dim regex = New Regex("^T")
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not regex.IsMatch(c.SA(0))
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

            Assert.Equal("{ ""sa.0"" : { ""$not"" : /^T/ } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0IsMatchStatic()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.SA(0), "^T")
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

            Assert.Equal("{ ""sa.0"" : /^T/ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0IsMatchStaticNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not Regex.IsMatch(c.SA(0), "^T")
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

            Assert.Equal("{ ""sa.0"" : { ""$not"" : /^T/ } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0IsMatchStaticWithOptions()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.SA(0), "^t", RegexOptions.IgnoreCase)
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

            Assert.Equal("{ ""sa.0"" : /^t/i }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0StartsWithT()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.SA(0).StartsWith("T")
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

            Assert.Equal("{ ""sa.0"" : /^T/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSASub0StartsWithTNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.SA(0).StartsWith("T")
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

            Assert.Equal("{ ""sa.0"" : { ""$not"" : /^T/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSContainsAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Contains("abc")
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

            Assert.Equal("{ ""s"" : /abc/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSContainsAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.S.Contains("abc")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /abc/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSContainsDot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Contains(".")
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

            Assert.Equal("{ ""s"" : /\./s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSCountEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Count() = 3
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

            Assert.Equal("{ ""s"" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEqualsAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S = "abc"
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

            Assert.Equal("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEqualsAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.S = "abc")
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

            Assert.Equal("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEqualsMethodAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Equals("abc")
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

            Assert.Equal("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEqualsMethodAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.S.Equals("abc"))
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

            Assert.Equal("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEqualsStaticMethodAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where String.Equals(c.S, "abc")
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

            Assert.Equal("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEqualsStaticMethodAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not String.Equals(c.S, "abc")
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

            Assert.Equal("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEndsWithAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.EndsWith("abc")
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

            Assert.Equal("{ ""s"" : /abc$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSEndsWithAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.S.EndsWith("abc")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /abc$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfAnyBC()
            Dim tempCollection = __database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C() With {.S = "bxxx"})
            tempCollection.Insert(New C() With {.S = "xbxx"})
            tempCollection.Insert(New C() With {.S = "xxbx"})
            tempCollection.Insert(New C() With {.S = "xxxb"})
            tempCollection.Insert(New C() With {.S = "bxbx"})
            tempCollection.Insert(New C() With {.S = "xbbx"})
            tempCollection.Insert(New C() With {.S = "xxbb"})

            Dim query1 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOfAny(New Char() {"b"c, "c"c}) = 2
                         Select c
            Assert.Equal(2, Consume(query1))

            Dim query2 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOfAny(New Char() {"b"c, "c"c}, 1) = 2
                         Select c
            Assert.Equal(3, Consume(query2))

            Dim query3 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOfAny(New Char() {"b"c, "c"c}, 1, 1) = 2
                         Select c
            Assert.Equal(0, Consume(query3))

            Dim query4 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOfAny(New Char() {"b"c, "c"c}, 1, 2) = 2
                         Select c
            Assert.Equal(3, Consume(query4))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfAnyBDashCEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "-"c, "c"c}) = 1
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

            Assert.Equal("{ ""s"" : /^[^b\-c]{1}[b\-c]/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfAnyBCStartIndex1Equals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "-"c, "c"c}, 1) = 1
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

            Assert.Equal("{ ""s"" : /^.{1}[^b\-c]{0}[b\-c]/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfAnyBCStartIndex1Count2Equals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "-"c, "c"c}, 1, 2) = 1
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

            Assert.Equal("{ ""s"" : /^.{1}(?=.{2})[^b\-c]{0}[b\-c]/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfB()
            Dim tempCollection = __database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C() With {
             .S = "bxxx"
            })
            tempCollection.Insert(New C() With {
             .S = "xbxx"
            })
            tempCollection.Insert(New C() With {
             .S = "xxbx"
            })
            tempCollection.Insert(New C() With {
             .S = "xxxb"
            })
            tempCollection.Insert(New C() With {
             .S = "bxbx"
            })
            tempCollection.Insert(New C() With {
             .S = "xbbx"
            })
            tempCollection.Insert(New C() With {
             .S = "xxbb"
            })

            Dim query1 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("b"c) = 2
                         Select c
            Assert.Equal(2, Consume(query1))

            Dim query2 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("b"c, 1) = 2
                         Select c
            Assert.Equal(3, Consume(query2))

            Dim query3 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("b"c, 1, 1) = 2
                         Select c
            Assert.Equal(0, Consume(query3))

            Dim query4 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("b"c, 1, 2) = 2
                         Select c
            Assert.Equal(3, Consume(query4))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfBEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c) = 1
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

            Assert.Equal("{ ""s"" : /^[^b]{1}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfBStartIndex1Equals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1) = 1
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

            Assert.Equal("{ ""s"" : /^.{1}[^b]{0}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfBStartIndex1Count2Equals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1, 2) = 1
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

            Assert.Equal("{ ""s"" : /^.{1}(?=.{2})[^b]{0}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfXyz()
            Dim tempCollection = __database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C() With {
             .S = "xyzaaa"
            })
            tempCollection.Insert(New C() With {
             .S = "axyzaa"
            })
            tempCollection.Insert(New C() With {
             .S = "aaxyza"
            })
            tempCollection.Insert(New C() With {
             .S = "aaaxyz"
            })
            tempCollection.Insert(New C() With {
             .S = "aaaaxy"
            })
            tempCollection.Insert(New C() With {
             .S = "xyzxyz"
            })

            Dim query1 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("xyz") = 3
                         Select c
            Assert.Equal(1, Consume(query1))

            Dim query2 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("xyz", 1) = 3
                         Select c
            Assert.Equal(2, Consume(query2))

            Dim query3 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("xyz", 1, 4) = 3
                         Select c
            Assert.Equal(0, Consume(query3))
            ' substring isn't long enough to match
            Dim query4 = From c In tempCollection.AsQueryable(Of C)()
                         Where c.S.IndexOf("xyz", 1, 5) = 3
                         Select c
            Assert.Equal(2, Consume(query4))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfXyzEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz") = 3
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

            Assert.Equal("{ ""s"" : /^(?!.{0,2}xyz).{3}xyz/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfXyzStartIndex1Equals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1) = 3
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

            Assert.Equal("{ ""s"" : /^.{1}(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIndexOfXyzStartIndex1Count5Equals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1, 5) = 3
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

            Assert.Equal("{ ""s"" : /^.{1}(?=.{5})(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIsMatch()
            Dim regex = New Regex("^abc")
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where regex.IsMatch(c.S)
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

            Assert.Equal("{ ""s"" : /^abc/ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIsMatchNot()
            Dim regex = New Regex("^abc")
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not regex.IsMatch(c.S)
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^abc/ } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIsMatchStatic()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.S, "^abc")
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

            Assert.Equal("{ ""s"" : /^abc/ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIsMatchStaticNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not Regex.IsMatch(c.S, "^abc")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^abc/ } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIsMatchStaticWithOptions()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.S, "^abc", RegexOptions.IgnoreCase)
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

            Assert.Equal("{ ""s"" : /^abc/i }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSIsNullOrEmpty()
            Dim tempCollection = __database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C())
            ' serialized document will have no "s" field
            tempCollection.Insert(New BsonDocument("s", BsonNull.Value))
            ' work around [BsonIgnoreIfNull] on S
            tempCollection.Insert(New C() With {
             .S = ""
            })
            tempCollection.Insert(New C() With {
             .S = "x"
            })

            Dim query = From c In tempCollection.AsQueryable(Of C)()
                        Where String.IsNullOrEmpty(c.S)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(tempCollection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""$or"" : [{ ""s"" : { ""$type"" : 10 } }, { ""s"" : """" }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Length = 3
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

            Assert.Equal("{ ""s"" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.S.Length = 3)
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthGreaterThan3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Length > 3
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

            Assert.Equal("{ ""s"" : /^.{4,}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthGreaterThanOrEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Length >= 3
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

            Assert.Equal("{ ""s"" : /^.{3,}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthLessThan3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Length < 3
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

            Assert.Equal("{ ""s"" : /^.{0,2}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthLessThanOrEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Length <= 3
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

            Assert.Equal("{ ""s"" : /^.{0,3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthNotEquals3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Length <> 3
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSLengthNotEquals3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.S.Length <> 3)
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

            Assert.Equal("{ ""s"" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSNotEqualsAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S <> "abc"
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

            Assert.Equal("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSNotEqualsAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.S <> "abc")
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

            Assert.Equal("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSStartsWithAbc()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.StartsWith("abc")
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

            Assert.Equal("{ ""s"" : /^abc/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSStartsWithAbcNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.S.StartsWith("abc")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^abc/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSSub1EqualsB()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S(1) = "b"c
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

            Assert.Equal("{ ""s"" : /^.{1}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSSub1NotEqualsB()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S(1) <> "b"c
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

            Assert.Equal("{ ""s"" : /^.{1}[^b]/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimContainsXyz()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Trim().Contains("xyz")
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

            Assert.Equal("{ ""s"" : /^\s*.*xyz.*\s*$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimContainsXyzNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.S.Trim().Contains("xyz")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^\s*.*xyz.*\s*$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimEndsWithXyz()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Trim().EndsWith("xyz")
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

            Assert.Equal("{ ""s"" : /^\s*.*xyz\s*$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimEndsWithXyzNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.S.Trim().EndsWith("xyz")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^\s*.*xyz\s*$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimStartsWithXyz()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.Trim().StartsWith("xyz")
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

            Assert.Equal("{ ""s"" : /^\s*xyz.*\s*$/s }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimStartsWithXyzNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.S.Trim().StartsWith("xyz")
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

            Assert.Equal("{ ""s"" : { ""$not"" : /^\s*xyz.*\s*$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSTrimStartTrimEndToLowerContainsXyz()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.TrimStart(" "c, "."c, "-"c, ControlChars.Tab).TrimEnd().ToLower().Contains("xyz")
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

            Assert.Equal("{ ""s"" : /^[\ \.\-\t]*.*xyz.*\s*$/is }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToLowerEqualsConstantLowerCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToLower() = "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToLower() == ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""s"" : /^abc$/i }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToLowerDoesNotEqualConstantLowerCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToLower() <> "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToLower() != ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""s"" : { ""$not"" : /^abc$/i } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToLowerEqualsConstantMixedCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToLower() = "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToLower() == ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_id"" : { ""$type"" : -1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToLowerDoesNotEqualConstantMixedCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToLower() <> "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToLower() != ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToLowerEqualsNullValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToLower() = Nothing
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

            Assert.Equal("{ ""s"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToLowerDoesNotEqualNullValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToLower() <> Nothing
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

            Assert.Equal("{ ""s"" : { ""$ne"" : null } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToUpperEqualsConstantLowerCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() = "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToUpper() == ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_id"" : { ""$type"" : -1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToUpperDoesNotEqualConstantLowerCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() <> "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToUpper() != ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToUpperEqualsConstantMixedCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() = "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToUpper() == ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""_id"" : { ""$type"" : -1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToUpperDoesNotEqualConstantMixedCaseValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() <> "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(C c) => (c.S.ToUpper() != ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToUpperEqualsNullValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() = Nothing
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

            Assert.Equal("{ ""s"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereSToUpperDoesNotEqualNullValue()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() <> Nothing
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

            Assert.Equal("{ ""s"" : { ""$ne"" : null } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub


        <Fact>
        Public Sub TestWhereSystemProfileInfoDurationGreatherThan10Seconds()
            Dim query = From pi In __systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.Duration > TimeSpan.FromSeconds(10)
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__systemProfileCollection, translatedQuery.Collection)
            Assert.Same(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(SystemProfileInfo pi) => (pi.Duration > TimeSpan:(00:00:10))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""millis"" : { ""$gt"" : 10000.0 } }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Fact>
        Public Sub TestWhereSystemProfileInfoNamespaceEqualsNs()
            Dim query = From pi In __systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.[Namespace] = "ns"
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__systemProfileCollection, translatedQuery.Collection)
            Assert.Same(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(SystemProfileInfo pi) => (pi.Namespace == ""ns"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""ns"" : ""ns"" }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Fact>
        Public Sub TestWhereSystemProfileInfoNumberScannedGreaterThan1000()
            Dim query = From pi In __systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.NumberScanned > 1000
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__systemProfileCollection, translatedQuery.Collection)
            Assert.Same(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(SystemProfileInfo pi) => (pi.NumberScanned > 1000)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""nscanned"" : { ""$gt"" : 1000 } }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Fact>
        Public Sub TestWhereSystemProfileInfoTimeStampGreatherThanJan12012()
            Dim query = From pi In __systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.Timestamp > New DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__systemProfileCollection, translatedQuery.Collection)
            Assert.Same(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.Equal("(SystemProfileInfo pi) => (pi.Timestamp > DateTime:(2012-01-01T00:00:00Z))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""ts"" : { ""$gt"" : ISODate(""2012-01-01T00:00:00Z"") } }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Fact>
        Public Sub TestWhereTripleAnd()
            If __server.BuildInfo.Version >= New Version(2, 0) Then
                ' the query is a bit odd in order to force the built query to be promoted to $and form
                Dim query = From c In __collection.AsQueryable(Of C)()
                            Where c.X >= 0 AndAlso c.X >= 1 AndAlso c.Y = 11
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

                Assert.Equal("{ ""$and"" : [{ ""x"" : { ""$gte"" : 0 } }, { ""x"" : { ""$gte"" : 1 } }, { ""y"" : 11 }] }", selectQuery.BuildQuery().ToJson())
                Assert.Equal(2, Consume(query))
            End If
        End Sub

        <Fact>
        Public Sub TestWhereTripleOr()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X = 1 OrElse c.Y = 33 OrElse c.S = "x is 1"
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

            Assert.Equal("{ ""$or"" : [{ ""x"" : 1 }, { ""y"" : 33 }, { ""s"" : ""x is 1"" }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereWithIndex()
            Dim query = __collection.AsQueryable(Of C)().Where(Function(c, i) True)
            Dim exception = Record.Exception(
                Sub()
                    query.ToList()
                    ' execute query
                End Sub)

            Dim expectedMessage = "The indexed version of the Where query operator is not supported."
            Assert.IsType(Of NotSupportedException)(exception)
            Assert.Equal(expectedMessage, exception.Message)
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
        Public Sub TestWhereXEquals1AndYEquals11()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X = 1 AndAlso c.Y = 11
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

            Assert.Equal("{ ""x"" : 1, ""y"" : 11 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1AndYEquals11UsingTwoWhereClauses()
            ' note: using different variable names in the two where clauses to test parameter replacement when combining predicates
            Dim query = __collection.AsQueryable(Of C)().Where(Function(c) c.X = 1).Where(Function(d) d.Y = 11)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            ' note parameter replacement from c to d in second clause
            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : 1, ""y"" : 11 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1AndYEquals11Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X = 1 AndAlso c.Y = 11)
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

            Assert.Equal("{ ""$nor"" : [{ ""x"" : 1, ""y"" : 11 }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1AndYEquals11AndZEquals11()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X = 1 AndAlso c.Y = 11 AndAlso c.D.Z = 11
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

            Assert.Equal("{ ""x"" : 1, ""y"" : 11, ""d.z"" : 11 }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X = 1)
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

            Assert.Equal("{ ""x"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1OrYEquals33()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X = 1 OrElse c.Y = 33
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

            Assert.Equal("{ ""$or"" : [{ ""x"" : 1 }, { ""y"" : 33 }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1OrYEquals33Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X = 1 OrElse c.Y = 33)
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

            Assert.Equal("{ ""$nor"" : [{ ""x"" : 1 }, { ""y"" : 33 }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1OrYEquals33NotNot()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not Not (c.X = 1 OrElse c.Y = 33)
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

            Assert.Equal("{ ""$or"" : [{ ""x"" : 1 }, { ""y"" : 33 }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXEquals1UsingJavaScript()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where c.X = 1 AndAlso Query.Where("this.x < 9").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : 1, ""$where"" : { ""$code"" : ""this.x < 9"" } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThan1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X > 1
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

            Assert.Equal("{ ""x"" : { ""$gt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThan1AndLessThan3()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X > 1 AndAlso c.X < 3
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

            Assert.Equal("{ ""x"" : { ""$gt"" : 1, ""$lt"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThan1AndLessThan3Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X > 1 AndAlso c.X < 3)
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

            Assert.Equal("{ ""$nor"" : [{ ""x"" : { ""$gt"" : 1, ""$lt"" : 3 } }] }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThan1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X > 1)
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

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$gt"" : 1 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThan1Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 1 < c.X
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

            Assert.Equal("{ ""x"" : { ""$gt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThanOrEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X >= 1
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

            Assert.Equal("{ ""x"" : { ""$gte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThanOrEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X >= 1)
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

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$gte"" : 1 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXGreaterThanOrEquals1Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 1 <= c.X
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

            Assert.Equal("{ ""x"" : { ""$gte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXIn1Or9()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X.[In]({1, 9})
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

            Assert.Equal("{ ""x"" : { ""$in"" : [1, 9] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXIn1Or9Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not c.X.[In]({1, 9})
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

            Assert.Equal("{ ""x"" : { ""$nin"" : [1, 9] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXIsTypeInt32()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Query.Type("x", BsonType.Int32).Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : { ""$type"" : 16 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereXIsTypeInt32Not()
            Dim query__1 = From c In __collection.AsQueryable(Of C)()
                           Where Not Query.Type("x", BsonType.Int32).Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsType(Of SelectQuery)(translatedQuery)
            Assert.Same(__collection, translatedQuery.Collection)
            Assert.Same(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.Null(selectQuery.OrderBy)
            Assert.Null(selectQuery.Projection)
            Assert.Null(selectQuery.Skip)
            Assert.Null(selectQuery.Take)

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$type"" : 16 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query__1))
        End Sub

        <Fact>
        Public Sub TestWhereXLessThan1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X < 1
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

            Assert.Equal("{ ""x"" : { ""$lt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXLessThan1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X < 1)
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

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$lt"" : 1 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(5, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXLessThan1Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 1 > c.X
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

            Assert.Equal("{ ""x"" : { ""$lt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(0, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXLessThanOrEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X <= 1
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

            Assert.Equal("{ ""x"" : { ""$lte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXLessThanOrEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X <= 1)
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

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$lte"" : 1 } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXLessThanOrEquals1Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 1 >= c.X
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

            Assert.Equal("{ ""x"" : { ""$lte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(1, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXModOneEquals0AndXModTwoEquals0()
            If __server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From c In __collection.AsQueryable(Of C)()
                            Where (c.X Mod 1 = 0) AndAlso (c.X Mod 2 = 0)
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

                Assert.Equal("{ ""$and"" : [{ ""x"" : { ""$mod"" : [1, 0] } }, { ""x"" : { ""$mod"" : [2, 0] } }] }", selectQuery.BuildQuery().ToJson())
                Assert.Equal(2, Consume(query))
            End If
        End Sub

        <Fact>
        Public Sub TestWhereXModOneEquals0AndXModTwoEquals0Not()
            If __server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From c In __collection.AsQueryable(Of C)()
                            Where Not ((c.X Mod 1 = 0) AndAlso (c.X Mod 2 = 0))
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

                Dim json = selectQuery.BuildQuery().ToJson()

                Assert.Equal("{ ""$nor"" : [{ ""$and"" : [{ ""x"" : { ""$mod"" : [1, 0] } }, { ""x"" : { ""$mod"" : [2, 0] } }] }] }", selectQuery.BuildQuery().ToJson())
                Assert.Equal(3, Consume(query))
            End If
        End Sub

        <Fact>
        Public Sub TestWhereXModOneEquals0AndXModTwoEquals0NotNot()
            If __server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From c In __collection.AsQueryable(Of C)()
                            Where Not Not ((c.X Mod 1 = 0) AndAlso (c.X Mod 2 = 0))
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

                Assert.Equal("{ ""$or"" : [{ ""$and"" : [{ ""x"" : { ""$mod"" : [1, 0] } }, { ""x"" : { ""$mod"" : [2, 0] } }] }] }", selectQuery.BuildQuery().ToJson())
                Assert.Equal(2, Consume(query))
            End If
        End Sub

        <Fact>
        Public Sub TestWhereXModTwoEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X Mod 2 = 1
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

            Assert.Equal("{ ""x"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXModTwoEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X Mod 2 = 1)
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

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXModTwoEquals1Reversed()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where 1 = c.X Mod 2
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

            Assert.Equal("{ ""x"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXModTwoNotEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X Mod 2 <> 1
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

            Assert.Equal("{ ""x"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(2, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXModTwoNotEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X Mod 2 <> 1)
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

            Assert.Equal("{ ""x"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(3, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXNotEquals1()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where c.X <> 1
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

            Assert.Equal("{ ""x"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.Equal(4, Consume(query))
        End Sub

        <Fact>
        Public Sub TestWhereXNotEquals1Not()
            Dim query = From c In __collection.AsQueryable(Of C)()
                        Where Not (c.X <> 1)
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

        Private Function Consume(Of T)(ByVal query As IQueryable(Of T)) As Integer
            Dim count = 0
            For Each c In query
                count += 1
            Next
            Return count
        End Function
    End Class
End Namespace