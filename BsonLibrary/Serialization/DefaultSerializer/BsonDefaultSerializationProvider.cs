/* Copyright 2010 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.DefaultSerializer {
    public class BsonDefaultSerializationProvider : IBsonSerializationProvider {
        #region private static fields
        private static BsonDefaultSerializationProvider singleton = new BsonDefaultSerializationProvider();
        #endregion

        #region constructors
        private BsonDefaultSerializationProvider() {
        }
        #endregion

        #region public static properties
        public static BsonDefaultSerializationProvider Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void Initialize() {
            RegisterSerializers();
        }
        #endregion

        #region private static methods
        // automatically register all BsonSerializers found in the BsonLibrary
        private static void RegisterSerializers() {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes()) {
                if (typeof(IBsonSerializer).IsAssignableFrom(type) && type != typeof(IBsonSerializer)) {
                    var registerSerializersInfo = type.GetMethod("RegisterSerializers", BindingFlags.Public | BindingFlags.Static);
                    if (registerSerializersInfo != null) {
                        registerSerializersInfo.Invoke(null, null);
                    }
                }
            }
        }
        #endregion

        #region public methods
        public IBsonSerializer GetSerializer(
            Type type
        ) {
            if (type.IsPrimitive || type.IsInterface) {
                var message = string.Format("No serializer found for type: {0}", type.FullName);
                throw new BsonSerializationException(message);
            }

           return BsonDefaultSerializer.Singleton;
        }
        #endregion
    }
}
