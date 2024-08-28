﻿/* Copyright 2010-present MongoDB Inc.
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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    // This class is needed by compiler for supporting init only properties in net core frameworks prior to net5.
    // This class should not be used by developers in the source code.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [SuppressMessage("Performance", "CA1812")]
    internal class IsExternalInit { }
}
