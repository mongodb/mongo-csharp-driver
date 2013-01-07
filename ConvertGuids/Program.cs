/* Copyright 2010-2013 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Driver;

namespace ConvertGuids {
    public static class Program {
        private static string url;
        private static string fromCollectionName;
        private static GuidRepresentation fromRepresentation;
        private static string toCollectionName;
        private static GuidRepresentation toRepresentation;
        private static bool createIndexes;

        public static int Main(string[] args) {
            if (!ParseArgs(args)) {
                Usage();
                return 1;
            }

            try {
                ConvertCollection();
            } catch (Exception ex) {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex);
                return 1;
            }

            return 0;
        }

        private static bool ParseArgs(
            string[] args
        ) {
            throw new NotImplementedException();
        }

        private static void Usage() {
            Console.WriteLine("Converts GUIDs in a collection from one representation to another");
            Console.WriteLine();
            Console.WriteLine("ConvertGuids url -fromCollection name -fromRepresentation rep -toCollection name -toRepresentation rep [-createIndexes]");
            Console.WriteLine("url: the URL to the database, e.g. \"mongodb://hostname/databasename\"");
            Console.WriteLine("name: the name of a collection");
            Console.WriteLine("rep: a GuidRepresentation (CSharpLegacy, JavaLegacy, PythonLegacy or Standard)");
        }

        private static void ConvertCollection() {
            var database = MongoDatabase.Create(url);

            var fromCollectionSettings = database.CreateCollectionSettings<BsonDocument>(fromCollectionName);
            fromCollectionSettings.GuidRepresentation = fromRepresentation;
            var fromCollection = database.GetCollection(fromCollectionSettings);
            if (!fromCollection.Exists()) {
                throw new ApplicationException("From collection does not exist.");
            }

            var toCollectionSettings = database.CreateCollectionSettings<BsonDocument>(toCollectionName);
            toCollectionSettings.GuidRepresentation = toRepresentation;
            var toCollection = database.GetCollection(toCollectionSettings);
            if (toCollection.Exists()) {
                throw new ApplicationException("To collection already exists.");
            }

            foreach (var document in fromCollection.FindAll()) {
                ConvertDocument(document);
                toCollection.Insert(document);
            }
        }

        private static BsonDocument ConvertDocument(
            BsonDocument document
        ) {
            foreach (var element in document.Elements) {
                element.Value = ConvertValue(element.Value);
            }
            return document;
        }

        private static BsonValue ConvertValue(
            BsonValue value
        ) {
            switch (value.BsonType) {
                case BsonType.Array: return ConvertArray(value.AsBsonArray);
                case BsonType.Binary: return ConvertBinaryData(value.AsBsonBinaryData);
                case BsonType.Document: return ConvertDocument(value.AsBsonDocument);
                case BsonType.JavaScriptWithScope: return ConvertJavaScriptWithScope(value.AsBsonJavaScriptWithScope);
                default: return value;
            }
        }

        private static BsonArray ConvertArray(
            BsonArray array
        ) {
            for (int i = 0; i < array.Count; i++) {
                array[i] = ConvertValue(array[i]);
            }
            return array;
        }

        private static BsonBinaryData ConvertBinaryData(
            BsonBinaryData binary
        ) {
            if (binary.SubType == BsonBinarySubType.Uuid || binary.SubType == BsonBinarySubType.UuidLegacy) {
                if (binary.GuidRepresentation != toRepresentation) {
                    var guid = binary.ToGuid();
                    return new BsonBinaryData(guid, toRepresentation);
                }
            }

            return binary;
        }

        private static BsonJavaScriptWithScope ConvertJavaScriptWithScope(
            BsonJavaScriptWithScope script
        ) {
            return new BsonJavaScriptWithScope(script.Code, ConvertDocument(script.Scope));
        }
    }
}
