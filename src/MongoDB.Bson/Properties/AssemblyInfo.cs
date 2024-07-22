/* Copyright 2010-present MongoDB Inc.
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MongoDB.Bson;

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

// Prevents the Xamarin static linker from stripping anything from this assembly.
// Required for most of the serialization classes on AOT compiled targets, such
// as Xamarin.iOS/Xamarin.Mac.
[assembly: Preserve(AllMembers = true)]

[assembly: InternalsVisibleTo("MongoDB.Bson.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010035287f0d3883c0a075c88e0cda3ce93b621003ecbd5e920d4a8c7238564f4d2f4f68116aca28c9b21341dc3a877679c14556192b2b2f5fe2c11d624e0894d308ff7b94bf6fd72aef1b41017ffe2572e99019d1c61963e68cd0ed67734a42cb333b808e3867cbe631937214e32e409fb1fa62fdb69d494c2530e64a40e417d6ee")]
[assembly: InternalsVisibleTo("MongoDB.Analyzer.MQLGenerator, PublicKey=002400000480000094000000060200000024000052534131000400000100010035287f0d3883c0a075c88e0cda3ce93b621003ecbd5e920d4a8c7238564f4d2f4f68116aca28c9b21341dc3a877679c14556192b2b2f5fe2c11d624e0894d308ff7b94bf6fd72aef1b41017ffe2572e99019d1c61963e68cd0ed67734a42cb333b808e3867cbe631937214e32e409fb1fa62fdb69d494c2530e64a40e417d6ee")]
