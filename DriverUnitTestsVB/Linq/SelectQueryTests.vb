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
Imports System.Text.RegularExpressions
Imports NUnit.Framework

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports MongoDB.Driver
Imports MongoDB.Driver.Builders
Imports MongoDB.Driver.Linq

Namespace MongoDB.DriverUnitTests.Linq
    <TestFixture()> _
    Public Class SelectQueryTests
        Public Enum E
            None
            A
            B
            C
        End Enum

        Public Class C
            Public Property Id() As ObjectId

            <BsonElement("x")> _
            Public Property X() As Integer
            <BsonElement("lx")> _
            Public Property LX() As Long

            <BsonElement("y")> _
            Public Property Y() As Integer

            <BsonElement("d")> _
            Public Property D() As D
            <BsonElement("da")> _
            Public Property DA() As List(Of D)
            <BsonElement("s")> _
            <BsonIgnoreIfNull()> _
            Public Property S() As String

            <BsonElement("a")> _
            <BsonIgnoreIfNull()> _
            Public Property A() As Integer()

            <BsonElement("b")> _
            Public Property B() As Boolean

            <BsonElement("l")> _
            <BsonIgnoreIfNull()> _
            Public Property L() As List(Of Integer)

            <BsonElement("dbref")> _
            <BsonIgnoreIfNull()> _
            Public Property DBRef() As MongoDBRef

            <BsonElement("e")> _
            <BsonIgnoreIfDefault()> _
            <BsonRepresentation(BsonType.[String])> _
            Public Property E() As E

            <BsonElement("ea")> _
            <BsonIgnoreIfNull()> _
            Public Property EA() As E()

            <BsonElement("sa")> _
            <BsonIgnoreIfNull()> _
            Public Property SA() As String()

            <BsonElement("ba")> _
            <BsonIgnoreIfNull()> _
            Public Property BA() As Boolean()
        End Class

        Public Class D
            <BsonElement("z")> _
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

        Private _server As MongoServer
        Private _database As MongoDatabase
        Private _collection As MongoCollection(Of C)
        Private _systemProfileCollection As MongoCollection(Of SystemProfileInfo)

        Private _id1 As ObjectId = ObjectId.GenerateNewId()
        Private _id2 As ObjectId = ObjectId.GenerateNewId()
        Private _id3 As ObjectId = ObjectId.GenerateNewId()
        Private _id4 As ObjectId = ObjectId.GenerateNewId()
        Private _id5 As ObjectId = ObjectId.GenerateNewId()

        <TestFixtureSetUp()> _
        Public Sub Setup()
            _server = Configuration.TestServer
            _server.Connect()
            _database = Configuration.TestDatabase
            _collection = Configuration.GetTestCollection(Of C)()
            _systemProfileCollection = _database.GetCollection(Of SystemProfileInfo)("system.profile")

            ' documents inserted deliberately out of order to test sorting
            _collection.Drop()
            _collection.Insert(New C() With
            {
                 .Id = _id2,
                 .X = 2,
                 .LX = 2,
                 .Y = 11,
                 .D = New D() With {.Z = 22},
                 .A = {2, 3, 4},
                 .L = New List(Of Integer)({2, 3, 4})
            })
            _collection.Insert(New C() With
            {
                 .Id = _id1,
                 .X = 1,
                 .LX = 1,
                 .Y = 11,
                 .D = New D() With {.Z = 11},
                 .S = "abc",
                 .SA = {"Tom", "Dick", "Harry"}
            })
            _collection.Insert(New C() With
            {
                 .Id = _id3,
                 .X = 3,
                 .LX = 3,
                 .Y = 33,
                 .D = New D() With {.Z = 33},
                 .B = True,
                 .BA = {True},
                 .E = E.A,
                 .EA = New E() {E.A, E.B}
            })
            _collection.Insert(New C() With
            {
                 .Id = _id5,
                 .X = 5,
                 .LX = 5,
                 .Y = 44,
                 .D = New D() With {.Z = 55},
                .DBRef = New MongoDBRef("db", "c", 1)
            })
            _collection.Insert(New C() With
            {
                 .Id = _id4,
                 .X = 4,
                 .LX = 4,
                 .Y = 44,
                 .D = New D() With {.Z = 44},
                 .DA = {New D() With {.Z = 333}}.ToList,
                .S = "   xyz   "
            })
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Aggregate query operator is not supported.")> _
        Public Sub TestAggregate()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Aggregate(Function(a, b) Nothing)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Aggregate query operator is not supported.")> _
        Public Sub TestAggregateWithAccumulator()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Aggregate(0, Function(a, c) 0)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Aggregate query operator is not supported.")> _
        Public Sub TestAggregateWithAccumulatorAndSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Aggregate(0, Function(a, c) 0, Function(a) a)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The All query operator is not supported.")> _
        Public Sub TestAll()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).All(Function(c) True)
        End Sub

        <Test()> _
        Public Sub TestAny()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Any()
            Assert.IsTrue(result)
        End Sub

        <Test()> _
        Public Sub TestAnyWhereXEquals1()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).Any()
            Assert.IsTrue(result)
        End Sub

        <Test()> _
        Public Sub TestAnyWhereXEquals9()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).Any()
            Assert.IsFalse(result)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="Any with predicate after a projection is not supported.")> _
        Public Sub TestAnyWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).Any(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestAnyWithPredicateAfterWhere()
            Dim result = _collection.AsQueryable(Of C)().Where(Function(c) c.X = 1).Any(Function(c) c.Y = 11)
            Assert.IsTrue(result)
        End Sub

        <Test()> _
        Public Sub TestAnyWithPredicateFalse()
            Dim result = _collection.AsQueryable(Of C)().Any(Function(c) c.X = 9)
            Assert.IsFalse(result)
        End Sub

        <Test()> _
        Public Sub TestAnyWithPredicateTrue()
            Dim result = _collection.AsQueryable(Of C)().Any(Function(c) c.X = 1)
            Assert.IsTrue(result)
        End Sub

        <Test()> _
        Public Sub TestAsQueryableWithNothingElse()
            Dim query = _collection.AsQueryable(Of C)()
            Dim result = query.ToList()
            Assert.AreEqual(5, result.Count)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Average query operator is not supported.")> _
        Public Sub TestAverage()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select 1.0).Average()
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Average query operator is not supported.")> _
        Public Sub TestAverageNullable()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select New Nullable(Of Decimal)(1.0)).Average()
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Average query operator is not supported.")> _
        Public Sub TestAverageWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Average(Function(c) 1.0)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Average query operator is not supported.")> _
        Public Sub TestAverageWithSelectorNullable()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Average(Function(c) New Nullable(Of Decimal)(1.0))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Cast query operator is not supported.")> _
        Public Sub TestCast()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select C).Cast(Of C)()
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Concat query operator is not supported.")> _
        Public Sub TestConcat()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Concat(source2)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Contains query operator is not supported.")> _
        Public Sub TestContains()
            Dim item = New C()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Contains(item)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Contains query operator is not supported.")> _
        Public Sub TestContainsWithEqualityComparer()
            Dim item = New C()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Contains(item, New CEqualityComparer())
        End Sub

        <Test()> _
        Public Sub TestCountEquals2()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).Count()

            Assert.AreEqual(2, result)
        End Sub

        <Test()> _
        Public Sub TestCountEquals5()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Count()

            Assert.AreEqual(5, result)
        End Sub

        <Test()> _
        Public Sub TestCountWithPredicate()
            Dim result = _collection.AsQueryable(Of C)().Count(Function(c) c.Y = 11)

            Assert.AreEqual(2, result)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="Count with predicate after a projection is not supported.")> _
        Public Sub TestCountWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).Count(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestCountWithPredicateAfterWhere()
            Dim result = _collection.AsQueryable(Of C)().Where(Function(c) c.X = 1).Count(Function(c) c.Y = 11)

            Assert.AreEqual(1, result)
        End Sub

        <Test()> _
        Public Sub TestCountWithSkipAndTake()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Skip(2).Take(2).Count()

            Assert.AreEqual(2, result)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The DefaultIfEmpty query operator is not supported.")> _
        Public Sub TestDefaultIfEmpty()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).DefaultIfEmpty()
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The DefaultIfEmpty query operator is not supported.")> _
        Public Sub TestDefaultIfEmptyWithDefaultValue()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).DefaultIfEmpty(Nothing)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestDistinctASub0()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.A(0)).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains(2))
        End Sub

        <Test()> _
        Public Sub TestDistinctB()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.B).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(2, results.Count)
            Assert.IsTrue(results.Contains(False))
            Assert.IsTrue(results.Contains(True))
        End Sub

        <Test()> _
        Public Sub TestDistinctBASub0()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.BA(0)).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains(True))
        End Sub

        <Test()> _
        Public Sub TestDistinctD()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.D).Distinct()
            Dim results = query.ToList()
            ' execute query
            Assert.AreEqual(5, results.Count)
            Assert.IsTrue(results.Contains(New D() With { _
             .Z = 11 _
            }))
            Assert.IsTrue(results.Contains(New D() With { _
             .Z = 22 _
            }))
            Assert.IsTrue(results.Contains(New D() With { _
             .Z = 33 _
            }))
            Assert.IsTrue(results.Contains(New D() With { _
             .Z = 44 _
            }))
            Assert.IsTrue(results.Contains(New D() With { _
             .Z = 55 _
            }))
        End Sub

        <Test()> _
        Public Sub TestDistinctDBRef()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.DBRef).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains(New MongoDBRef("db", "c", 1)))
        End Sub

        <Test()> _
        Public Sub TestDistinctDBRefDatabase()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.DBRef.DatabaseName).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains("db"))
        End Sub

        <Test()> _
        Public Sub TestDistinctDZ()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.D.Z).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.IsTrue(results.Contains(11))
            Assert.IsTrue(results.Contains(22))
            Assert.IsTrue(results.Contains(33))
            Assert.IsTrue(results.Contains(44))
            Assert.IsTrue(results.Contains(55))
        End Sub

        <Test()> _
        Public Sub TestDistinctE()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.E).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains(E.A))
        End Sub

        <Test()> _
        Public Sub TestDistinctEASub0()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.EA(0)).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains(E.A))
        End Sub

        <Test()> _
        Public Sub TestDistinctId()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.Id).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.IsTrue(results.Contains(_id1))
            Assert.IsTrue(results.Contains(_id2))
            Assert.IsTrue(results.Contains(_id3))
            Assert.IsTrue(results.Contains(_id4))
            Assert.IsTrue(results.Contains(_id5))
        End Sub

        <Test()> _
        Public Sub TestDistinctLSub0()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.L(0)).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains(2))
        End Sub

        <Test()> _
        Public Sub TestDistinctS()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.S).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(2, results.Count)
            Assert.IsTrue(results.Contains("abc"))
            Assert.IsTrue(results.Contains("   xyz   "))
        End Sub

        <Test()> _
        Public Sub TestDistinctSASub0()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.SA(0)).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(1, results.Count)
            Assert.IsTrue(results.Contains("Tom"))
        End Sub

        <Test()> _
        Public Sub TestDistinctX()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.X).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.IsTrue(results.Contains(1))
            Assert.IsTrue(results.Contains(2))
            Assert.IsTrue(results.Contains(3))
            Assert.IsTrue(results.Contains(4))
            Assert.IsTrue(results.Contains(5))
        End Sub

        <Test()> _
        Public Sub TestDistinctXWithQuery()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Where c.X > 3
                         Select c.X).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(2, results.Count)
            Assert.IsTrue(results.Contains(4))
            Assert.IsTrue(results.Contains(5))
        End Sub

        <Test()> _
        Public Sub TestDistinctY()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c.Y).Distinct()
            Dim results = query.ToList()
            Assert.AreEqual(3, results.Count)
            Assert.IsTrue(results.Contains(11))
            Assert.IsTrue(results.Contains(33))
            Assert.IsTrue(results.Contains(44))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The version of the Distinct query operator with an equality comparer is not supported.")> _
        Public Sub TestDistinctWithEqualityComparer()
            Dim query = _collection.AsQueryable(Of C)().Distinct(New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestElementAtOrDefaultWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).ElementAtOrDefault(2)

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestElementAtOrDefaultWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).ElementAtOrDefault(0)
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestElementAtOrDefaultWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).ElementAtOrDefault(0)

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestElementAtOrDefaultWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).ElementAtOrDefault(1)

            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestElementAtWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).ElementAt(2)

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestElementAtWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).ElementAt(0)
        End Sub

        <Test()> _
        Public Sub TestElementAtWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).ElementAt(0)

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestElementAtWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).ElementAt(1)

            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Except query operator is not supported.")> _
        Public Sub TestExcept()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Except(source2)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Except query operator is not supported.")> _
        Public Sub TestExceptWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Except(source2, New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault()

            Assert.AreEqual(2, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).FirstOrDefault()
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).FirstOrDefault()

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="FirstOrDefault with predicate after a projection is not supported.")> _
        Public Sub TestFirstOrDefaultWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).FirstOrDefault(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithPredicateAfterWhere()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).FirstOrDefault(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithPredicateNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault(Function(c) c.X = 9)
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithPredicateOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault(Function(c) c.X = 3)
            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithPredicateTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).FirstOrDefault(Function(c) c.Y = 11)
            Assert.AreEqual(2, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstOrDefaultWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).FirstOrDefault()

            Assert.AreEqual(2, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).First()

            Assert.AreEqual(2, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestFirstWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).First()
        End Sub

        <Test()> _
        Public Sub TestFirstWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).First()

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="First with predicate after a projection is not supported.")> _
        Public Sub TestFirstWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).First(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestFirstWithPredicateAfterWhere()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).First(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstWithPredicateNoMatch()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In _collection.AsQueryable(Of C)()
                                                                                   Select c).First(Function(c) c.X = 9)
                                                                 End Sub)
            Assert.AreEqual(ExpectedErrorMessage.FirstEmptySequence, ex.Message)
        End Sub

        <Test()> _
        Public Sub TestFirstWithPredicateOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).First(Function(c) c.X = 3)
            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstWithPredicateTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).First(Function(c) c.Y = 11)
            Assert.AreEqual(2, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestFirstWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).First()

            Assert.AreEqual(2, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelector()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndElementSelector()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndElementSelectorAndEqualityComparer()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c, New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndElementSelectorAndResultSelector()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c, Function(c, e) 1.0)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndElementSelectorAndResultSelectorAndEqualityComparer()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(c) c, Function(c, e) e.First(), New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndEqualityComparer()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndResultSelector()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(k, e) 1.0)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupBy query operator is not supported.")> _
        Public Sub TestGroupByWithKeySelectorAndResultSelectorAndEqualityComparer()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).GroupBy(Function(c) c, Function(k, e) e.First(), New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupJoin query operator is not supported.")> _
        Public Sub TestGroupJoin()
            Dim inner = New C(-1) {}
            Dim query = _collection.AsQueryable(Of C)().GroupJoin(inner, Function(c) c, Function(c) c, Function(c, e) c)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The GroupJoin query operator is not supported.")> _
        Public Sub TestGroupJoinWithEqualityComparer()
            Dim inner = New C(-1) {}
            Dim query = _collection.AsQueryable(Of C)().GroupJoin(inner, Function(c) c, Function(c) c, Function(c, e) c, New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Intersect query operator is not supported.")> _
        Public Sub TestIntersect()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Intersect(source2)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Intersect query operator is not supported.")> _
        Public Sub TestIntersectWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Intersect(source2, New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Join query operator is not supported.")> _
        Public Sub TestJoin()
            Dim query = _collection.AsQueryable(Of C)().Join(_collection.AsQueryable(Of C)(), Function(c) c.X, Function(c) c.X, Function(x, y) x)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Join query operator is not supported.")> _
        Public Sub TestJoinWithEqualityComparer()
            Dim query = _collection.AsQueryable(Of C)().Join(_collection.AsQueryable(Of C)(), Function(c) c.X, Function(c) c.X, Function(x, y) x, New Int32EqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).LastOrDefault()

            Assert.AreEqual(4, result.X)
            Assert.AreEqual(44, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).LastOrDefault()
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).LastOrDefault()

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithOrderBy()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                       Order By c.X
                       Select c).LastOrDefault()

            Assert.AreEqual(5, result.X)
            Assert.AreEqual(44, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="LastOrDefault with predicate after a projection is not supported.")> _
        Public Sub TestLastOrDefaultWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).LastOrDefault(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithPredicateAfterWhere()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).LastOrDefault(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithPredicateNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).LastOrDefault(Function(c) c.X = 9)
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithPredicateOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).LastOrDefault(Function(c) c.X = 3)
            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithPredicateTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).LastOrDefault(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastOrDefaultWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).LastOrDefault()

            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Last()

            Assert.AreEqual(4, result.X)
            Assert.AreEqual(44, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestLastWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).Last()
        End Sub

        <Test()> _
        Public Sub TestLastWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).Last()

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="Last with predicate after a projection is not supported.")> _
        Public Sub TestLastWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().Select(Function(c) c.Y).Last(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestLastWithPredicateAfterWhere()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).Last(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastWithPredicateNoMatch()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In _collection.AsQueryable(Of C)()
                                                                                   Select c).Last(Function(c) c.X = 9)
                                                                 End Sub)
            Assert.AreEqual(ExpectedErrorMessage.LastEmptySequence, ex.Message)
        End Sub

        <Test()> _
        Public Sub TestLastWithPredicateOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Last(Function(c) c.X = 3)
            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastWithPredicateTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Last(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastWithOrderBy()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Order By c.X
                          Select c).Last()

            Assert.AreEqual(5, result.X)
            Assert.AreEqual(44, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLastWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).Last()

            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestLongCountEquals2()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).LongCount()

            Assert.AreEqual(2L, result)
        End Sub

        <Test()> _
        Public Sub TestLongCountEquals5()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).LongCount()

            Assert.AreEqual(5L, result)
        End Sub

        <Test()> _
        Public Sub TestLongCountWithSkipAndTake()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Skip(2).Take(2).LongCount()

            Assert.AreEqual(2L, result)
        End Sub

        <Test()> _
        Public Sub TestMaxDZWithProjection()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c.D.Z).Max()
            Assert.AreEqual(55, result)
        End Sub

        <Test()> _
        Public Sub TestMaxDZWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Max(Function(c) c.D.Z)
            Assert.AreEqual(55, result)
        End Sub

        <Test()> _
        Public Sub TestMaxXWithProjection()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c.X).Max()
            Assert.AreEqual(5, result)
        End Sub

        <Test()> _
        Public Sub TestMaxXWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                                   Select c).Max(Function(c) c.X)
            Assert.AreEqual(5, result)
        End Sub

        <Test()> _
        Public Sub TestMaxXYWithProjection()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select New With {c.X, c.Y}).Max()
            Assert.AreEqual(5, result.X)
            Assert.AreEqual(44, result.Y)
        End Sub

        <Test()> _
        Public Sub TestMaxXYWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Max(Function(c) New With
                          {
                             c.X,
                             c.Y
                          })
            Assert.AreEqual(5, result.X)
            Assert.AreEqual(44, result.Y)
        End Sub

        <Test()> _
        Public Sub TestMinDZWithProjection()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c.D.Z).Min()
            Assert.AreEqual(11, result)
        End Sub

        <Test()> _
        Public Sub TestMinDZWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Min(Function(c) c.D.Z)
            Assert.AreEqual(11, result)
        End Sub

        <Test()> _
        Public Sub TestMinXWithProjection()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c.X).Min()
            Assert.AreEqual(1, result)
        End Sub

        <Test()> _
        Public Sub TestMinXWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Min(Function(c) c.X)
            Assert.AreEqual(1, result)
        End Sub

        <Test()> _
        Public Sub TestMinXYWithProjection()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select New With {c.X, c.Y}).Min()
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestMinXYWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Min(Function(c) New With
                          {
                             c.X,
                             c.Y
                          })
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestOrderByAscending()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.AreEqual(1, selectQuery.OrderBy.Count)

            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy(0).Direction)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(1, results.First().X)
            Assert.AreEqual(5, results.Last().X)
        End Sub

        <Test()> _
        Public Sub TestOrderByAscendingThenByAscending()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.Y, c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.AreEqual(2, selectQuery.OrderBy.Count)

            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy(0).Direction)

            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy(1).Direction)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(1, results.First().X)
            Assert.AreEqual(5, results.Last().X)
        End Sub

        <Test()> _
        Public Sub TestOrderByAscendingThenByDescending()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.Y, c.X Descending
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.AreEqual(2, selectQuery.OrderBy.Count)

            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy(0).Direction)

            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy(1).Direction)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(2, results.First().X)
            Assert.AreEqual(4, results.Last().X)
        End Sub

        <Test()> _
        Public Sub TestOrderByDescending()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.X Descending
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.AreEqual(1, selectQuery.OrderBy.Count)

            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy(0).Direction)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(5, results.First().X)
            Assert.AreEqual(1, results.Last().X)
        End Sub

        <Test()> _
        Public Sub TestOrderByDescendingThenByAscending()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.Y Descending, c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.AreEqual(2, selectQuery.OrderBy.Count)

            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy(0).Direction)

            Assert.AreEqual(OrderByDirection.Ascending, selectQuery.OrderBy(1).Direction)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(4, results.First().X)
            Assert.AreEqual(2, results.Last().X)
        End Sub

        <Test()> _
        Public Sub TestOrderByDescendingThenByDescending()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.Y Descending, c.X Descending
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.AreEqual(2, selectQuery.OrderBy.Count)

            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy(0).Direction)

            Assert.AreEqual(OrderByDirection.Descending, selectQuery.OrderBy(1).Direction)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(5, results.First().X)
            Assert.AreEqual(1, results.Last().X)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).")> _
        Public Sub TestOrderByDuplicate()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Order By c.X _
                        Order By c.Y
                        Select c

            MongoQueryTranslator.Translate(query)
        End Sub

        <Test()> _
        Public Sub TestProjection()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Select c.X

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.IsNull(selectQuery.OrderBy)

            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())

            Dim results = query.ToList()
            Assert.AreEqual(5, results.Count)
            Assert.AreEqual(2, results.First())
            Assert.AreEqual(4, results.Last())
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Reverse query operator is not supported.")> _
        Public Sub TestReverse()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Reverse()
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestSelect()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SelectMany query operator is not supported.")> _
        Public Sub TestSelectMany()
            Dim query = _collection.AsQueryable(Of C)().SelectMany(Function(c) New C() {c})
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SelectMany query operator is not supported.")> _
        Public Sub TestSelectManyWithIndex()
            Dim query = _collection.AsQueryable(Of C)().SelectMany(Function(c, index) New C() {c})
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SelectMany query operator is not supported.")> _
        Public Sub TestSelectManyWithIntermediateResults()
            Dim query = _collection.AsQueryable(Of C)().SelectMany(Function(c) New C() {c}, Function(c, i) i)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SelectMany query operator is not supported.")> _
        Public Sub TestSelectManyWithIndexAndIntermediateResults()
            Dim query = _collection.AsQueryable(Of C)().SelectMany(Function(c, index) New C() {c}, Function(c, i) i)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The indexed version of the Select query operator is not supported.")> _
        Public Sub TestSelectWithIndex()
            Dim query = _collection.AsQueryable(Of C)().[Select](Function(c, index) c)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestSelectWithNothingElse()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Select c
            Dim result = query.ToList()
            Assert.AreEqual(5, result.Count)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SequenceEqual query operator is not supported.")> _
        Public Sub TestSequenceEqual()
            Dim source2 = New C(-1) {}
            Dim result = (From c In _collection.AsQueryable(Of C)()
                                   Select c).SequenceEqual(source2)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SequenceEqual query operator is not supported.")> _
        Public Sub TestSequenceEqualtWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim result = (From c In _collection.AsQueryable(Of C)()
                                   Select c).SequenceEqual(source2, New CEqualityComparer())
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestSingleOrDefaultWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                                   Select c).SingleOrDefault()
        End Sub

        <Test()> _
        Public Sub TestSingleOrDefaultWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).SingleOrDefault()
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestSingleOrDefaultWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).SingleOrDefault()

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="SingleOrDefault with predicate after a projection is not supported.")> _
        Public Sub TestSingleOrDefaultWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).SingleOrDefault(Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestSingleOrDefaultWithPredicateAfterWhere()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).SingleOrDefault(Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestSingleOrDefaultWithPredicateNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).SingleOrDefault(Function(c) c.X = 9)
            Assert.IsNull(result)
        End Sub

        <Test()> _
        Public Sub TestSingleOrDefaultWithPredicateOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).SingleOrDefault(Function(c) c.X = 3)
            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestSingleOrDefaultWithPredicateTwoMatches()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In _collection.AsQueryable(Of C)()
                                                                                   Select c).SingleOrDefault(Function(c) c.Y = 11)
                                                                 End Sub)
            Assert.AreEqual(ExpectedErrorMessage.SingleLongSequence, ex.Message)
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestSingleOrDefaultWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.Y = 11
                          Select c).SingleOrDefault()
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestSingleWithManyMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).[Single]()
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestSingleWithNoMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 9
                          Select c).[Single]()
        End Sub

        <Test()> _
        Public Sub TestSingleWithOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 3
                          Select c).[Single]()

            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="Single with predicate after a projection is not supported.")> _
        Public Sub TestSingleWithPredicateAfterProjection()
            Dim result = _collection.AsQueryable(Of C)().[Select](Function(c) c.Y).[Single](Function(y) y = 11)
        End Sub

        <Test()> _
        Public Sub TestSingleWithPredicateAfterWhere()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Where c.X = 1
                          Select c).[Single](Function(c) c.Y = 11)
            Assert.AreEqual(1, result.X)
            Assert.AreEqual(11, result.Y)
        End Sub

        <Test()> _
        Public Sub TestSingleWithPredicateNoMatch()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In _collection.AsQueryable(Of C)()
                                                                                   Select c).[Single](Function(c) c.X = 9)
                                                                 End Sub)
            Assert.AreEqual(ExpectedErrorMessage.SingleEmptySequence, ex.Message)
        End Sub

        <Test()> _
        Public Sub TestSingleWithPredicateOneMatch()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).[Single](Function(c) c.X = 3)
            Assert.AreEqual(3, result.X)
            Assert.AreEqual(33, result.Y)
        End Sub

        <Test()> _
        Public Sub TestSingleWithPredicateTwoMatches()
            Dim ex = Assert.Throws(Of InvalidOperationException)(Sub()
                                                                     Dim result = (From c In _collection.AsQueryable(Of C)()
                                                                                   Select c).[Single](Function(c) c.Y = 11)
                                                                 End Sub)
            Assert.AreEqual(ExpectedErrorMessage.SingleLongSequence, ex.Message)
        End Sub

        <Test()> _
        <ExpectedException(GetType(InvalidOperationException))> _
        Public Sub TestSingleWithTwoMatches()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                       Where c.Y = 11
                       Select c).[Single]()
        End Sub

        <Test()> _
        Public Sub TestSkip2()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Skip(2)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.AreEqual(2, selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The SkipWhile query operator is not supported.")> _
        Public Sub TestSkipWhile()
            Dim query = _collection.AsQueryable(Of C)().SkipWhile(Function(c) True)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Sum query operator is not supported.")> _
        Public Sub TestSum()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select 1.0).Sum()
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Sum query operator is not supported.")> _
        Public Sub TestSumNullable()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select New Nullable(Of Integer)(1)).Sum()
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Sum query operator is not supported.")> _
        Public Sub TestSumWithSelector()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Sum(Function(c) 1.0)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Sum query operator is not supported.")> _
        Public Sub TestSumWithSelectorNullable()
            Dim result = (From c In _collection.AsQueryable(Of C)()
                          Select c).Sum(Function(c) New Nullable(Of Decimal)(1.0))
        End Sub

        <Test()> _
        Public Sub TestTake2()
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Take(2)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.Where)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.AreEqual(2, selectQuery.Take)

            Assert.IsNull(selectQuery.BuildQuery())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The TakeWhile query operator is not supported.")> _
        Public Sub TestTakeWhile()
            Dim query = _collection.AsQueryable(Of C)().TakeWhile(Function(c) True)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.")> _
        Public Sub TestThenByWithMissingOrderBy()
            ' not sure this could ever happen in real life without deliberate sabotaging like with this cast
            Dim query = DirectCast(_collection.AsQueryable(Of C)(), IOrderedQueryable(Of C)).ThenBy(Function(c) c.X)

            MongoQueryTranslator.Translate(query)
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Union query operator is not supported.")> _
        Public Sub TestUnion()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Union(source2)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The Union query operator is not supported.")> _
        Public Sub TestUnionWithEqualityComparer()
            Dim source2 = New C(-1) {}
            Dim query = (From c In _collection.AsQueryable(Of C)()
                         Select c).Union(source2, New CEqualityComparer())
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestWhereAAny()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A.Any()
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$ne"" : null, ""$not"" : { ""$size"" : 0 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="Any is only support for items that serialize into documents. The current serializer is Int32Serializer and must implement IBsonDocumentSerializer for participation in Any queries.")> _
        Public Sub TestWhereAAnyWithPredicate()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A.Any(Function(a) a > 3)
                        Select c
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestWhereLocalListContainsX()
            Dim local = New List(Of Integer)() From { _
             1, _
             2, _
             3 _
            }

            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where local.Contains(c.X)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => System.Collections.Generic.List`1[System.Int32].Contains(c.X)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$in"" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLocalArrayContainsX()
            Dim local = {1, 2, 3}

            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where local.Contains(c.X)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => Enumerable.Contains<Int32>(Int32[]:{ 1, 2, 3 }, c.X)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$in"" : [1, 2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub


        <Test()> _
        Public Sub TestWhereAContains2()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A.Contains(2)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : 2 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereAContains2Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.A.Contains(2)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$ne"" : 2 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereAContainsAll()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.A.ContainsAll({2, 3})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$all"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(Query))
        End Sub

        <Test()> _
        Public Sub TestWhereAContainsAllNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where Not c.A.ContainsAll({2, 3})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$not"" : { ""$all"" : [2, 3] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereAContainsAny()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.A.ContainsAny({2, 3})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$in"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereAContainsAnyNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where Not c.A.ContainsAny({1, 2})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$nin"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereAExistsFalse()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                           Where Query.NotExists("a").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereAExistsTrue()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                           Where Query.Exists("a").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereAExistsTrueNot()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                           Where Not Query.Exists("a").Inject()
                           Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereALengthEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A.Length = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereALengthEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.A.Length = 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereALengthEquals3Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 3 = c.A.Length
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereALengthNotEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A.Length <> 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereALengthNotEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.A.Length <> 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1Equals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A(1) = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1Equals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.A(1) = 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1ModTwoEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A(1) Mod 2 = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1ModTwoEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.A(1) Mod 2 = 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1ModTwoNotEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A(1) Mod 2 <> 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1ModTwoNotEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.A(1) Mod 2 <> 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1NotEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.A(1) <> 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereASub1NotEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.A(1) <> 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""a.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereB()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.B
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBASub0()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.BA(0)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ba.0"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBASub0EqualsFalse()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.BA(0) = False
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ba.0"" : false }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBASub0EqualsFalseNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.BA(0) = False)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ba.0"" : { ""$ne"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBASub0EqualsTrue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.BA(0) = True
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ba.0"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBASub0EqualsTrueNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.BA(0) = True)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ba.0"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBASub0Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.BA(0)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ba.0"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBEqualsFalse()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.B = False
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : false }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBEqualsFalseNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.B = False)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : { ""$ne"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBEqualsTrue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.B = True
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : true }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBEqualsTrueNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.B = True)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereBNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.B
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""b"" : { ""$ne"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDAAnyWithPredicate()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.DA.Any(Function(x) x.Z = 333)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(Query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => Enumerable.Any<D>(c.DA, (D x) => (x.Z == 333))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""da"" : { ""$elemMatch"" : { ""z"" : 333 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(Query))
        End Sub


        <Test()> _
        Public Sub TestWhereDBRefCollectionNameEqualsC()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.DBRef.CollectionName = "c"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref.$ref"" : ""c"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDBRefDatabaseNameEqualsDb()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.DBRef.DatabaseName = "db"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref.$db"" : ""db"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDBRefEquals()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.DBRef = New MongoDBRef("db", "c", 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDBRefEqualsNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.DBRef = New MongoDBRef("db", "c", 1))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref"" : { ""$ne"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDBRefNotEquals()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.DBRef <> New MongoDBRef("db", "c", 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref"" : { ""$ne"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDBRefNotEqualsNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.DBRef <> New MongoDBRef("db", "c", 1))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(Query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref"" : { ""$ref"" : ""c"", ""$id"" : 1, ""$db"" : ""db"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(Query))
        End Sub

        <Test()> _
        Public Sub TestWhereDBRefIdEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.DBRef.Id = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""dbref.$id"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDEquals11()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.D.Equals(New D() With { _
                         .Z = 11 _
                        })
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""d"" : { ""z"" : 11 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDEquals11Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.D.Equals(New D() With { _
                         .Z = 11 _
                        }))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""d"" : { ""$ne"" : { ""z"" : 11 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDNotEquals11()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.D.Equals(New D() With { _
                         .Z = 11 _
                        })
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""d"" : { ""$ne"" : { ""z"" : 11 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereDNotEquals11Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (Not c.D.Equals(New D() With { _
                         .Z = 11 _
                        }))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""d"" : { ""z"" : 11 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEAContainsAll()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.EA.ContainsAll(New E() {E.A, E.B})
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea"" : { ""$all"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEAContainsAllNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.EA.ContainsAll(New E() {E.A, E.B})
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea"" : { ""$not"" : { ""$all"" : [1, 2] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEAContainsAny()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.EA.ContainsAny({E.A, E.B})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea"" : { ""$in"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEAContainsAnyNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where Not c.EA.ContainsAny({E.A, E.B})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(Query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea"" : { ""$nin"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(Query))
        End Sub

        <Test()> _
        Public Sub TestWhereEAContainsB()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.EA.Contains(E.B)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea"" : 2 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEAContainsBNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.EA.Contains(E.B)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea"" : { ""$ne"" : 2 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEASub0EqualsA()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.EA(0) = E.A
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea.0"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEASub0EqualsANot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.EA(0) = E.A)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea.0"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEASub0NotEqualsA()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.EA(0) <> E.A
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea.0"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEASub0NotEqualsANot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.EA(0) <> E.A)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ea.0"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEEqualsA()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.E = E.A
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : ""A"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEEqualsANot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.E = E.A)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : { ""$ne"" : ""A"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEEqualsAReversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where E.A = c.E
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : ""A"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEInAOrB()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.E.In({E.A, E.B})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : { ""$in"" : [""A"", ""B""] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereEInAOrBNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where Not c.E.In({E.A, E.B})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : { ""$nin"" : [""A"", ""B""] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereENotEqualsA()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.E <> E.A
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : { ""$ne"" : ""A"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereENotEqualsANot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.E <> E.A)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""e"" : ""A"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLContains2()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L.Contains(2)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : 2 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLContains2Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.L.Contains(2)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$ne"" : 2 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLContainsAll()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.L.ContainsAll({2, 3})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$all"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLContainsAllNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.L.ContainsAll({2, 3})
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$not"" : { ""$all"" : [2, 3] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLContainsAny()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.L.ContainsAny({2, 3})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$in"" : [2, 3] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLContainsAnyNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where Not c.L.ContainsAny({1, 2})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$nin"" : [1, 2] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLExistsFalse()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                            Where Query.NotExists("l").Inject()
                            Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereLExistsTrue()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                            Where Query.Exists("l").Inject()
                            Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$exists"" : true } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereLExistsTrueNot()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                            Where Not Query.Exists("l").Inject()
                            Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$exists"" : false } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountMethodEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L.Count() = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountMethodEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L.Count() = 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountMethodEquals3Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 3 = c.L.Count()
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountPropertyEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L.Count = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountPropertyEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L.Count = 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountPropertyEquals3Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 3 = c.L.Count
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountPropertyNotEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L.Count <> 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$not"" : { ""$size"" : 3 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLCountPropertyNotEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L.Count <> 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l"" : { ""$size"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1Equals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L(1) = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1Equals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L(1) = 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1ModTwoEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L(1) Mod 2 = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1ModTwoEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L(1) Mod 2 = 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1ModTwoNotEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L(1) Mod 2 <> 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1ModTwoNotEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L(1) Mod 2 <> 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1NotEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.L(1) <> 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : { ""$ne"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLSub1NotEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.L(1) <> 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""l.1"" : 3 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLXModTwoEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.LX Mod 2 = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""lx"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLXModTwoEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.LX Mod 2 = 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""lx"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLXModTwoEquals1Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 1 = c.LX Mod 2
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""lx"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLXModTwoNotEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.LX Mod 2 <> 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""lx"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereLXModTwoNotEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.LX Mod 2 <> 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""lx"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0ContainsO()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.SA(0).Contains("o")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : /o/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0ContainsONot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.SA(0).Contains("o")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : { ""$not"" : /o/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0EndsWithM()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.SA(0).EndsWith("m")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : /m$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0EndsWithMNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.SA(0).EndsWith("m")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : { ""$not"" : /m$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0IsMatch()
            Dim regex = New Regex("^T")
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where regex.IsMatch(c.SA(0))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : /^T/ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0IsMatchNot()
            Dim regex = New Regex("^T")
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not regex.IsMatch(c.SA(0))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : { ""$not"" : /^T/ } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0IsMatchStatic()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.SA(0), "^T")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : /^T/ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0IsMatchStaticNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not Regex.IsMatch(c.SA(0), "^T")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : { ""$not"" : /^T/ } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0IsMatchStaticWithOptions()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.SA(0), "^t", RegexOptions.IgnoreCase)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : /^t/i }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0StartsWithT()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.SA(0).StartsWith("T")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : /^T/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSASub0StartsWithTNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.SA(0).StartsWith("T")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""sa.0"" : { ""$not"" : /^T/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSContainsAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Contains("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /abc/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSContainsAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.S.Contains("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /abc/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSContainsDot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Contains(".")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /\./s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSCountEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Count() = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEqualsAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S = "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEqualsAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.S = "abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEqualsMethodAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Equals("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEqualsMethodAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.S.Equals("abc"))
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEqualsStaticMethodAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where String.Equals(c.S, "abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEqualsStaticMethodAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not String.Equals(c.S, "abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEndsWithAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.EndsWith("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /abc$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSEndsWithAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.S.EndsWith("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /abc$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfAnyBC()
            Dim tempCollection = _database.GetCollection("temp")
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
            Assert.AreEqual(2, Consume(query1))

            Dim query2 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "c"c}, 1) = 2
                        Select c
            Assert.AreEqual(3, Consume(query2))

            Dim query3 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "c"c}, 1, 1) = 2
                        Select c
            Assert.AreEqual(0, Consume(query3))

            Dim query4 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "c"c}, 1, 2) = 2
                        Select c
            Assert.AreEqual(3, Consume(query4))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfAnyBDashCEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "-"c, "c"c}) = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^[^b\-c]{1}[b\-c]/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfAnyBCStartIndex1Equals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "-"c, "c"c}, 1) = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}[^b\-c]{0}[b\-c]/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfAnyBCStartIndex1Count2Equals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOfAny(New Char() {"b"c, "-"c, "c"c}, 1, 2) = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}(?=.{2})[^b\-c]{0}[b\-c]/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfB()
            Dim tempCollection = _database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C() With { _
             .S = "bxxx" _
            })
            tempCollection.Insert(New C() With { _
             .S = "xbxx" _
            })
            tempCollection.Insert(New C() With { _
             .S = "xxbx" _
            })
            tempCollection.Insert(New C() With { _
             .S = "xxxb" _
            })
            tempCollection.Insert(New C() With { _
             .S = "bxbx" _
            })
            tempCollection.Insert(New C() With { _
             .S = "xbbx" _
            })
            tempCollection.Insert(New C() With { _
             .S = "xxbb" _
            })

            Dim query1 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c) = 2
                        Select c
            Assert.AreEqual(2, Consume(query1))

            Dim query2 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1) = 2
                        Select c
            Assert.AreEqual(3, Consume(query2))

            Dim query3 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1, 1) = 2
                        Select c
            Assert.AreEqual(0, Consume(query3))

            Dim query4 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1, 2) = 2
                        Select c
            Assert.AreEqual(3, Consume(query4))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfBEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c) = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^[^b]{1}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfBStartIndex1Equals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1) = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}[^b]{0}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfBStartIndex1Count2Equals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("b"c, 1, 2) = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}(?=.{2})[^b]{0}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfXyz()
            Dim tempCollection = _database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C() With { _
             .S = "xyzaaa" _
            })
            tempCollection.Insert(New C() With { _
             .S = "axyzaa" _
            })
            tempCollection.Insert(New C() With { _
             .S = "aaxyza" _
            })
            tempCollection.Insert(New C() With { _
             .S = "aaaxyz" _
            })
            tempCollection.Insert(New C() With { _
             .S = "aaaaxy" _
            })
            tempCollection.Insert(New C() With { _
             .S = "xyzxyz" _
            })

            Dim query1 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz") = 3
                        Select c
            Assert.AreEqual(1, Consume(query1))

            Dim query2 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1) = 3
                        Select c
            Assert.AreEqual(2, Consume(query2))

            Dim query3 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1, 4) = 3
                        Select c
            Assert.AreEqual(0, Consume(query3))
            ' substring isn't long enough to match
            Dim query4 = From c In tempCollection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1, 5) = 3
                        Select c
            Assert.AreEqual(2, Consume(query4))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfXyzEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz") = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^(?!.{0,2}xyz).{3}xyz/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfXyzStartIndex1Equals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1) = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIndexOfXyzStartIndex1Count5Equals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.IndexOf("xyz", 1, 5) = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}(?=.{5})(?!.{0,1}xyz).{2}xyz/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIsMatch()
            Dim regex = New Regex("^abc")
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where regex.IsMatch(c.S)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^abc/ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIsMatchNot()
            Dim regex = New Regex("^abc")
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not regex.IsMatch(c.S)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^abc/ } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIsMatchStatic()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.S, "^abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^abc/ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIsMatchStaticNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not Regex.IsMatch(c.S, "^abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^abc/ } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIsMatchStaticWithOptions()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Regex.IsMatch(c.S, "^abc", RegexOptions.IgnoreCase)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^abc/i }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSIsNullOrEmpty()
            Dim tempCollection = _database.GetCollection("temp")
            tempCollection.Drop()
            tempCollection.Insert(New C())
            ' serialized document will have no "s" field
            tempCollection.Insert(New BsonDocument("s", BsonNull.Value))
            ' work around [BsonIgnoreIfNull] on S
            tempCollection.Insert(New C() With { _
             .S = "" _
            })
            tempCollection.Insert(New C() With { _
             .S = "x" _
            })

            Dim query = From c In tempCollection.AsQueryable(Of C)()
                        Where String.IsNullOrEmpty(c.S)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(tempCollection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""$or"" : [{ ""s"" : { ""$type"" : 10 } }, { ""s"" : """" }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Length = 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(Query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(Query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.S.Length = 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthGreaterThan3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Length > 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{4,}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthGreaterThanOrEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Length >= 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{3,}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthLessThan3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Length < 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{0,2}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthLessThanOrEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Length <= 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{0,3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthNotEquals3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Length <> 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^.{3}$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSLengthNotEquals3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.S.Length <> 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{3}$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSNotEqualsAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S <> "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$ne"" : ""abc"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSNotEqualsAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.S <> "abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : ""abc"" }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSStartsWithAbc()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.StartsWith("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^abc/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSStartsWithAbcNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.S.StartsWith("abc")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^abc/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSSub1EqualsB()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S(1) = "b"c
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}b/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSSub1NotEqualsB()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S(1) <> "b"c
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^.{1}[^b]/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimContainsXyz()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Trim().Contains("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^\s*.*xyz.*\s*$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimContainsXyzNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.S.Trim().Contains("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^\s*.*xyz.*\s*$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimEndsWithXyz()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Trim().EndsWith("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^\s*.*xyz\s*$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimEndsWithXyzNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.S.Trim().EndsWith("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^\s*.*xyz\s*$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimStartsWithXyz()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.Trim().StartsWith("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^\s*xyz.*\s*$/s }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimStartsWithXyzNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not c.S.Trim().StartsWith("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^\s*xyz.*\s*$/s } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSTrimStartTrimEndToLowerContainsXyz()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.TrimStart(" "c, "."c, "-"c, ControlChars.Tab).TrimEnd().ToLower().Contains("xyz")
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^[\ \.\-\t]*.*xyz.*\s*$/is }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToLowerEqualsConstantLowerCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToLower() = "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToLower() == ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : /^abc$/i }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToLowerDoesNotEqualConstantLowerCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToLower() <> "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToLower() != ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$not"" : /^abc$/i } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToLowerEqualsConstantMixedCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToLower() = "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToLower() == ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_id"" : { ""$type"" : -1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToLowerDoesNotEqualConstantMixedCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToLower() <> "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToLower() != ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToLowerEqualsNullValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToLower() = Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToLowerDoesNotEqualNullValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToLower() <> Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$ne"" : null } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToUpperEqualsConstantLowerCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() = "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToUpper() == ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_id"" : { ""$type"" : -1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToUpperDoesNotEqualConstantLowerCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() <> "abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToUpper() != ""abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToUpperEqualsConstantMixedCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() = "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToUpper() == ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""_id"" : { ""$type"" : -1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToUpperDoesNotEqualConstantMixedCaseValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() <> "Abc"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(C c) => (c.S.ToUpper() != ""Abc"")", ExpressionFormatter.ToString(selectQuery.Where))

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToUpperEqualsNullValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() = Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : null }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereSToUpperDoesNotEqualNullValue()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.S.ToUpper() <> Nothing
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""s"" : { ""$ne"" : null } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub


        <Test()> _
        Public Sub TestWhereSystemProfileInfoDurationGreatherThan10Seconds()
            Dim query = From pi In _systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.Duration > TimeSpan.FromSeconds(10)
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection)
            Assert.AreSame(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.Duration > TimeSpan:(00:00:10))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""millis"" : { ""$gt"" : 10000.0 } }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Test()> _
        Public Sub TestWhereSystemProfileInfoNamespaceEqualsNs()
            Dim query = From pi In _systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.[Namespace] = "ns"
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection)
            Assert.AreSame(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.Namespace == ""ns"")", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ns"" : ""ns"" }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Test()> _
        Public Sub TestWhereSystemProfileInfoNumberScannedGreaterThan1000()
            Dim query = From pi In _systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.NumberScanned > 1000
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection)
            Assert.AreSame(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.NumberScanned > 1000)", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""nscanned"" : { ""$gt"" : 1000 } }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Test()> _
        Public Sub TestWhereSystemProfileInfoTimeStampGreatherThanJan12012()
            Dim query = From pi In _systemProfileCollection.AsQueryable(Of SystemProfileInfo)()
                        Where pi.Timestamp > New DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        Select pi

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_systemProfileCollection, translatedQuery.Collection)
            Assert.AreSame(GetType(SystemProfileInfo), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)
            Assert.AreEqual("(SystemProfileInfo pi) => (pi.Timestamp > DateTime:(2012-01-01T00:00:00Z))", ExpressionFormatter.ToString(selectQuery.Where))
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""ts"" : { ""$gt"" : ISODate(""2012-01-01T00:00:00Z"") } }", selectQuery.BuildQuery().ToJson())
        End Sub

        <Test()> _
        Public Sub TestWhereTripleAnd()
            If _server.BuildInfo.Version >= New Version(2, 0) Then
                ' the query is a bit odd in order to force the built query to be promoted to $and form
                Dim query = From c In _collection.AsQueryable(Of C)()
                            Where c.X >= 0 AndAlso c.X >= 1 AndAlso c.Y = 11
                            Select c

                Dim translatedQuery = MongoQueryTranslator.Translate(query)
                Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
                Assert.AreSame(_collection, translatedQuery.Collection)
                Assert.AreSame(GetType(C), translatedQuery.DocumentType)

                Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

                Assert.IsNull(selectQuery.OrderBy)
                Assert.IsNull(selectQuery.Projection)
                Assert.IsNull(selectQuery.Skip)
                Assert.IsNull(selectQuery.Take)

                Assert.AreEqual("{ ""$and"" : [{ ""x"" : { ""$gte"" : 0 } }, { ""x"" : { ""$gte"" : 1 } }, { ""y"" : 11 }] }", selectQuery.BuildQuery().ToJson())
                Assert.AreEqual(2, Consume(query))
            End If
        End Sub

        <Test()> _
        Public Sub TestWhereTripleOr()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X = 1 OrElse c.Y = 33 OrElse c.S = "x is 1"
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""$or"" : [{ ""x"" : 1 }, { ""y"" : 33 }, { ""s"" : ""x is 1"" }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        <ExpectedException(GetType(NotSupportedException), ExpectedMessage:="The indexed version of the Where query operator is not supported.")> _
        Public Sub TestWhereWithIndex()
            Dim query = _collection.AsQueryable(Of C)().Where(Function(c, i) True)
            query.ToList()
            ' execute query
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1AndYEquals11()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X = 1 AndAlso c.Y = 11
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1, ""y"" : 11 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1AndYEquals11UsingTwoWhereClauses()
            ' note: using different variable names in the two where clauses to test parameter replacement when combining predicates
            Dim query = _collection.AsQueryable(Of C)().Where(Function(c) c.X = 1).Where(Function(d) d.Y = 11)

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            ' note parameter replacement from c to d in second clause
            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1, ""y"" : 11 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1AndYEquals11Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X = 1 AndAlso c.Y = 11)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""$or"" : [{ ""x"" : { ""$ne"" : 1 } }, { ""y"" : { ""$ne"" : 11 } }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1AndYEquals11AndZEquals11()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X = 1 AndAlso c.Y = 11 AndAlso c.D.Z = 11
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1, ""y"" : 11, ""d.z"" : 11 }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X = 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1OrYEquals33()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X = 1 OrElse c.Y = 33
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""$or"" : [{ ""x"" : 1 }, { ""y"" : 33 }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1OrYEquals33Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X = 1 OrElse c.Y = 33)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            'Assert.AreEqual("{ ""$nor"" : [{ ""x"" : 1 }, { ""y"" : 33 }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual("{ ""x"" : { ""$ne"" : 1 }, ""y"" : { ""$ne"" : 33 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1OrYEquals33NotNot()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not Not (c.X = 1 OrElse c.Y = 33)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""$or"" : [{ ""x"" : 1 }, { ""y"" : 33 }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXEquals1UsingJavaScript()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                            Where c.X = 1 AndAlso Query.Where("this.x < 9").Inject()
                            Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1, ""$where"" : { ""$code"" : ""this.x < 9"" } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThan1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X > 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThan1AndLessThan3()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X > 1 AndAlso c.X < 3
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gt"" : 1, ""$lt"" : 3 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThan1AndLessThan3Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X > 1 AndAlso c.X < 3)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""$or"" : [{ ""x"" : { ""$lte"" : 1 } }, { ""x"" : { ""$gte"" : 3 } }] }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThan1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X > 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$lte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThan1Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 1 < c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThanOrEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X >= 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThanOrEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X >= 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$lt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXGreaterThanOrEquals1Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 1 <= c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXIn1Or9()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where c.X.[In]({1, 9})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$in"" : [1, 9] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXIn1Or9Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                     Where Not c.X.[In]({1, 9})
                     Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$nin"" : [1, 9] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXIsTypeInt32()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                            Where Query.Type("x", BsonType.Int32).Inject()
                            Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$type"" : 16 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereXIsTypeInt32Not()
            Dim query__1 = From c In _collection.AsQueryable(Of C)()
                            Where Not Query.Type("x", BsonType.Int32).Inject()
                            Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query__1)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$not"" : { ""$type"" : 16 } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query__1))
        End Sub

        <Test()> _
        Public Sub TestWhereXLessThan1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X < 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$lt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXLessThan1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X < 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(5, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXLessThan1Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 1 > c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$lt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(0, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXLessThanOrEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X <= 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$lte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXLessThanOrEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X <= 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$gt"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXLessThanOrEquals1Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 1 >= c.X
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$lte"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(1, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXModOneEquals0AndXModTwoEquals0()
            If _server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From c In _collection.AsQueryable(Of C)()
                            Where (c.X Mod 1 = 0) AndAlso (c.X Mod 2 = 0)
                            Select c

                Dim translatedQuery = MongoQueryTranslator.Translate(query)
                Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
                Assert.AreSame(_collection, translatedQuery.Collection)
                Assert.AreSame(GetType(C), translatedQuery.DocumentType)

                Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

                Assert.IsNull(selectQuery.OrderBy)
                Assert.IsNull(selectQuery.Projection)
                Assert.IsNull(selectQuery.Skip)
                Assert.IsNull(selectQuery.Take)

                Assert.AreEqual("{ ""$and"" : [{ ""x"" : { ""$mod"" : [1, 0] } }, { ""x"" : { ""$mod"" : [2, 0] } }] }", selectQuery.BuildQuery().ToJson())
                Assert.AreEqual(2, Consume(query))
            End If
        End Sub

        <Test()> _
        Public Sub TestWhereXModOneEquals0AndXModTwoEquals0Not()
            If _server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From c In _collection.AsQueryable(Of C)()
                            Where Not ((c.X Mod 1 = 0) AndAlso (c.X Mod 2 = 0))
                            Select c

                Dim translatedQuery = MongoQueryTranslator.Translate(query)
                Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
                Assert.AreSame(_collection, translatedQuery.Collection)
                Assert.AreSame(GetType(C), translatedQuery.DocumentType)

                Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

                Assert.IsNull(selectQuery.OrderBy)
                Assert.IsNull(selectQuery.Projection)
                Assert.IsNull(selectQuery.Skip)
                Assert.IsNull(selectQuery.Take)

                Dim json = selectQuery.BuildQuery().ToJson()

                Assert.AreEqual("{ ""$or"" : [{ ""x"" : { ""$not"" : { ""$mod"" : [1, 0] } } }, { ""x"" : { ""$not"" : { ""$mod"" : [2, 0] } } }] }", selectQuery.BuildQuery().ToJson())
                Assert.AreEqual(3, Consume(query))
            End If
        End Sub

        <Test()> _
        Public Sub TestWhereXModOneEquals0AndXModTwoEquals0NotNot()
            If _server.BuildInfo.Version >= New Version(2, 0) Then
                Dim query = From c In _collection.AsQueryable(Of C)()
                            Where Not Not ((c.X Mod 1 = 0) AndAlso (c.X Mod 2 = 0))
                            Select c

                Dim translatedQuery = MongoQueryTranslator.Translate(query)
                Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
                Assert.AreSame(_collection, translatedQuery.Collection)
                Assert.AreSame(GetType(C), translatedQuery.DocumentType)

                Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

                Assert.IsNull(selectQuery.OrderBy)
                Assert.IsNull(selectQuery.Projection)
                Assert.IsNull(selectQuery.Skip)
                Assert.IsNull(selectQuery.Take)

                Assert.AreEqual("{ ""$and"" : [{ ""x"" : { ""$mod"" : [1, 0] } }, { ""x"" : { ""$mod"" : [2, 0] } }] }", selectQuery.BuildQuery().ToJson())
                Assert.AreEqual(2, Consume(query))
            End If
        End Sub

        <Test()> _
        Public Sub TestWhereXModTwoEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X Mod 2 = 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXModTwoEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X Mod 2 = 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXModTwoEquals1Reversed()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where 1 = c.X Mod 2
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXModTwoNotEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X Mod 2 <> 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$not"" : { ""$mod"" : [2, 1] } } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(2, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXModTwoNotEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X Mod 2 <> 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$mod"" : [2, 1] } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(3, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXNotEquals1()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where c.X <> 1
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : { ""$ne"" : 1 } }", selectQuery.BuildQuery().ToJson())
            Assert.AreEqual(4, Consume(query))
        End Sub

        <Test()> _
        Public Sub TestWhereXNotEquals1Not()
            Dim query = From c In _collection.AsQueryable(Of C)()
                        Where Not (c.X <> 1)
                        Select c

            Dim translatedQuery = MongoQueryTranslator.Translate(query)
            Assert.IsInstanceOf(Of SelectQuery)(translatedQuery)
            Assert.AreSame(_collection, translatedQuery.Collection)
            Assert.AreSame(GetType(C), translatedQuery.DocumentType)

            Dim selectQuery = DirectCast(translatedQuery, SelectQuery)

            Assert.IsNull(selectQuery.OrderBy)
            Assert.IsNull(selectQuery.Projection)
            Assert.IsNull(selectQuery.Skip)
            Assert.IsNull(selectQuery.Take)

            Assert.AreEqual("{ ""x"" : 1 }", selectQuery.BuildQuery().ToJson())
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