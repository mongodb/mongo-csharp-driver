/* Copyright 2015-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Runtime.InteropServices;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

[assembly: ComVisible(false)]

[assembly: CollectionBehavior(DisableTestParallelization = true)]

[assembly: TestFramework(XunitExtensionsConsts.TimeoutEnforcingXunitFramework, XunitExtensionsConsts.TimeoutEnforcingFrameworkAssembly)]
