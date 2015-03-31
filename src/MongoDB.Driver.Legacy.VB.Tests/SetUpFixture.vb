' Copyright 2010-2014 MongoDB Inc.
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

Imports MongoDB.Driver
Imports NUnit.Framework

Namespace MongoDB.Driver.VB.Tests

    <SetUpFixture()>
    Public Class SetUpFixture

        <TearDown>
        Public Sub TearDown()
            Dim cluster = CoreTestConfiguration.Cluster ' force cluster to be created so database can be dropped
            CoreTestConfiguration.TearDown()
        End Sub

    End Class

End Namespace
