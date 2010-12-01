﻿/* Copyright 2010 10gen Inc.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver {
    [Flags]
    public enum QueryFlags {
        None = 0,
        TailableCursor = 2,
        SlaveOk = 4,
        NoCursorTimeout = 16,
        AwaitData = 132,
        Exhaust = 64
    }
}
