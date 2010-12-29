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
        Type GetActualType(BsonReader bsonReader, Type nominalType);
        BsonValue GetDiscriminator(Type nominalType, Type actualType);
    }

    public abstract class StandardDiscriminatorConvention : IDiscriminatorConvention {
        #region private static fields
        private static ScalarDiscriminatorConvention scalar = new ScalarDiscriminatorConvention("_t");
        private static HierarchicalDiscriminatorConvention hierarchical = new HierarchicalDiscriminatorConvention("_t");
        #endregion

        #region private fields
        private string elementName;
        #endregion

        #region constructors
        protected StandardDiscriminatorConvention(
            string elementName
        ) {
            this.elementName = elementName;
        }
        #endregion

        #region public static properties
        public static ScalarDiscriminatorConvention Scalar {
            get { return scalar; }
        }

        public static HierarchicalDiscriminatorConvention Hierarchical {
            get { return hierarchical; }
        }
        #endregion

        #region public properties
        public string ElementName {
            get { return elementName; }
        }
        #endregion

        #region public methods
        // BsonReader is sitting at the value whose actual type needs to be found
        public Type GetActualType(
            BsonReader bsonReader,
            Type nominalType
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonReader.State == BsonReaderState.Value) {
                Type primitiveType = null;
                switch (bsonType) {
                    case BsonType.Boolean: primitiveType = typeof(bool); break;
                    case BsonType.Binary:
                        var bookmark = bsonReader.GetBookmark();
                        byte[] bytes;
                        BsonBinarySubType subType;
                        bsonReader.ReadBinaryData(out bytes, out subType);
                        if (subType == BsonBinarySubType.Uuid && bytes.Length == 16) {
                            primitiveType = typeof(Guid);
                        }
                        bsonReader.ReturnToBookmark(bookmark);
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
            }

            if (bsonType == BsonType.Document) {
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                var actualType = nominalType;
                if (bsonReader.FindElement(elementName)) {
                    var discriminator = BsonValue.ReadFrom(bsonReader);
                    if (discriminator.IsBsonArray) {
                        discriminator = discriminator.AsBsonArray.Last(); // last item is leaf class discriminator
                    }
                    actualType = BsonDefaultSerializer.LookupActualType(nominalType, discriminator);
                }
                bsonReader.ReturnToBookmark(bookmark);
                return actualType;
            }

            return nominalType;
        }

        public abstract BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType
        );
        #endregion
    }

    public class ScalarDiscriminatorConvention : StandardDiscriminatorConvention {
        #region constructors
        public ScalarDiscriminatorConvention(
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
            var classMap = BsonClassMap.LookupClassMap(actualType);
            return classMap.Discriminator;
        }
        #endregion
    }

    public class HierarchicalDiscriminatorConvention : StandardDiscriminatorConvention {
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
            var classMap = BsonClassMap.LookupClassMap(actualType);
            if (actualType != nominalType || classMap.DiscriminatorIsRequired || classMap.HasRootClass) {
                if (classMap.HasRootClass && !classMap.IsRootClass) {
                    var values = new List<BsonValue>();
                    for (; !classMap.IsRootClass; classMap = classMap.BaseClassMap) {
                        values.Add(classMap.Discriminator);
                    }
                    values.Add(classMap.Discriminator); // add the root class's discriminator
                    return new BsonArray(values.Reverse<BsonValue>()); // reverse to put leaf class last
                } else {
                    return classMap.Discriminator;
                }
            }

            return null;
        }
        #endregion
    }
}
