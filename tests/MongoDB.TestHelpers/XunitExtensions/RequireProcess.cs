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

using System;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions
{
    public class RequireProcess
    {
        #region static
        public static RequireProcess Check()
        {
            return new RequireProcess();
        }
        #endregion

        public RequireProcess Bits(int bits)
        {
            var actualBits = IntPtr.Size < 8 ? 32 : 64;
            if (actualBits == bits)
            {
                return this;
            }
            throw new SkipException($"Test skipped because process is a {actualBits}-bit process and not a {bits}-bit process.");
        }
    }
}
