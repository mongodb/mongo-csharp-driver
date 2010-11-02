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
using System.Linq;
using System.Text;
using System.Reflection;

using MongoDB.Bson.IO;

namespace MongoDB.Bson.DefaultSerializer.Conventions {
    public interface IDiscriminatorConvention {
        string ElementName { get; }
        Type GetActualDocumentType(BsonReader bsonReader, Type nominalType);
        Type GetActualElementType(BsonReader bsonReader, Type nominalType);
        BsonValue GetDiscriminator(Type nominalType, Type actualType);
    }

    public class ScalarDiscriminatorConvention : IDiscriminatorConvention {
        #region private fields
        private string elementName;
        #endregion

        #region constructors
        public ScalarDiscriminatorConvention(
            string elementName
        ) {
            this.elementName = elementName;
        }
        #endregion

        #region public properties
        public string ElementName {
            get { return elementName; }
        }
        #endregion

        #region public methods
        // BsonReader is sitting at the first element of the document whose actual type needs to be found
        public Type GetActualDocumentType(
            BsonReader bsonReader,
            Type nominalType
        ) {
            bsonReader.PushBookmark();
            var actualType = nominalType;
            if (bsonReader.FindElement(elementName)) {
                var discriminator = BsonElement.ReadFrom(bsonReader, elementName).Value;
                if (discriminator.IsBsonArray) {
                    discriminator = discriminator.AsBsonArray.Last(); // last item is leaf class discriminator
                }
                actualType = BsonDefaultSerializer.LookupActualType(nominalType, discriminator);
            }
            bsonReader.PopBookmark();
            return actualType;
        }

        // BsonReader is sitting at the element whose actual type needs to be found
        public Type GetActualElementType(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.PeekBsonType();

            Type primitiveType = null;
            string ignoredName;
            switch (bsonType) {
                case BsonType.Boolean: primitiveType = typeof(bool); break;
                case BsonType.Binary:
                    bsonReader.PushBookmark();
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData(out ignoredName, out bytes, out subType);
                    if (subType == BsonBinarySubType.Uuid && bytes.Length == 16) {
                        primitiveType = typeof(Guid);
                    }
                    bsonReader.PopBookmark();
                    break;
                case BsonType.DateTime: primitiveType = typeof(DateTime); break;
                case BsonType.Double: primitiveType = typeof(double); break;
                case BsonType.Int32: primitiveType = typeof(int); break;
                case BsonType.Int64: primitiveType = typeof(long); break;
                case BsonType.ObjectId: primitiveType = typeof(ObjectId); break;
                case BsonType.String: primitiveType = typeof(string); break;
            }

            if (primitiveType != null && nominalType.IsAssignableFrom(primitiveType)) {
                return primitiveType;
            }

            if (bsonType == BsonType.Document) {
                bsonReader.PushBookmark();
                bsonReader.ReadDocumentName(out ignoredName);
                bsonReader.ReadStartDocument();
                var actualType = nominalType;
                if (bsonReader.FindElement(elementName)) {
                    var discriminator = BsonElement.ReadFrom(bsonReader, elementName).Value;
                    if (discriminator.IsBsonArray) {
                        discriminator = discriminator.AsBsonArray.Last(); // last item is leaf class discriminator
                    }
                    actualType = BsonDefaultSerializer.LookupActualType(nominalType, discriminator);
                }
                bsonReader.PopBookmark();
                return actualType;
            }

            return nominalType;
        }

        public virtual BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType
        ) {
            var classMap = BsonClassMap.LookupClassMap(actualType);
            return classMap.Discriminator;
        }
        #endregion
    }

    public class HierarchicalDiscriminatorConvention : ScalarDiscriminatorConvention {
        #region constructors
        public HierarchicalDiscriminatorConvention(
            string elementName
        )
            : base(elementName) {
        }
        #endregion

        #region public methods
        public override BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType
        ) {
            if (actualType == nominalType) {
                return null;
            }

            var values = new List<BsonValue>();
            var hierarchicalType = actualType;
            while (hierarchicalType != typeof(object) && hierarchicalType != nominalType) {
                var classMap = BsonClassMap.LookupClassMap(hierarchicalType);
                values.Add(classMap.Discriminator);
                hierarchicalType = hierarchicalType.BaseType;
            }

            if (values.Count == 1) {
                return values[0];
            } else {
                return new BsonArray(values.Reverse<BsonValue>()); // reverse to put leaf class last
            }
        }
        #endregion
    }
}
