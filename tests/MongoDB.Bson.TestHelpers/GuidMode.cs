/* Copyright 2019-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace MongoDB.Bson.TestHelpers
{
    public class GuidMode : IXunitSerializable
    {
        #region static
        // private static fields
        private static IReadOnlyList<GuidMode> __all = new[]
        {
            new GuidMode(GuidRepresentationMode.V2, GuidRepresentation.CSharpLegacy),
            new GuidMode(GuidRepresentationMode.V2, GuidRepresentation.JavaLegacy),
            new GuidMode(GuidRepresentationMode.V2, GuidRepresentation.PythonLegacy),
            new GuidMode(GuidRepresentationMode.V2, GuidRepresentation.Standard),
            new GuidMode(GuidRepresentationMode.V2, GuidRepresentation.Unspecified),
            new GuidMode(GuidRepresentationMode.V3)
        };

        // public static properties
        public static IReadOnlyList<GuidMode> All => __all;

        public static GuidMode Current
        {
            get
            {
#pragma warning disable 618
                var currentMode = BsonDefaults.GuidRepresentationMode;
                if (currentMode == GuidRepresentationMode.V2)
                {
                    return new GuidMode(currentMode, BsonDefaults.GuidRepresentation);
                }
                else
                {
                    return new GuidMode(currentMode);
                }
#pragma warning restore 618
            }
        }

        // public static methods
        public static void Set(GuidRepresentationMode guidRepresentationMode, GuidRepresentation guidRepresentation = GuidRepresentation.Unspecified)
        {
            var mode = new GuidMode(guidRepresentationMode, guidRepresentation);
            mode.Set();
        }
        #endregion

        private GuidRepresentationMode _guidRepresentationMode;
        private GuidRepresentation _guidRepresentation;

        public GuidMode()
        {
            _guidRepresentationMode = GuidRepresentationMode.V2;
            _guidRepresentation = GuidRepresentation.CSharpLegacy;
        }

        public GuidMode(GuidRepresentationMode guidRepresentationMode, GuidRepresentation guidRepresentation = GuidRepresentation.Unspecified)
        {
            _guidRepresentationMode = guidRepresentationMode;
            _guidRepresentation = guidRepresentation;
        }

        public GuidRepresentationMode GuidRepresentationMode => _guidRepresentationMode;

        public GuidRepresentation GuidRepresentation => _guidRepresentation;

        public void Deserialize(IXunitSerializationInfo info)
        {
            _guidRepresentationMode = info.GetValue<GuidRepresentationMode>(nameof(_guidRepresentationMode));
            if (_guidRepresentationMode == GuidRepresentationMode.V2)
            {
                _guidRepresentation = info.GetValue<GuidRepresentation>(nameof(_guidRepresentation));
            }
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(_guidRepresentationMode), _guidRepresentationMode);
            if (_guidRepresentationMode == GuidRepresentationMode.V2)
            {
                info.AddValue(nameof(_guidRepresentation), _guidRepresentation);
            }
        }

        public void Set()
        {
#pragma warning disable 618
            BsonDefaults.GuidRepresentationMode = _guidRepresentationMode;
            if (_guidRepresentationMode == GuidRepresentationMode.V2)
            {
                BsonDefaults.GuidRepresentation = _guidRepresentation;
            }
#pragma warning restore 618
        }

        public override string ToString()
        {
            return _guidRepresentationMode == GuidRepresentationMode.V2 ? $"V2:{_guidRepresentation}" : "V3";
        }
    }
}
