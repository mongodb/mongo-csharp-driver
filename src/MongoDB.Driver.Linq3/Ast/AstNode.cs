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

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Linq3.Ast
{
    public abstract class AstNode
    {
        public abstract AstNodeType NodeType { get; }

        public abstract BsonValue Render();

        public override string ToString()
        {
            var jsonWriterSettings = new JsonWriterSettings();
#pragma warning disable CS0618 // Type or member is obsolete
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                jsonWriterSettings.GuidRepresentation = GuidRepresentation.Unspecified;
            }
#pragma warning restore CS0618 // Type or member is obsolete
            return Render().ToJson(jsonWriterSettings);
        }
    }
}
