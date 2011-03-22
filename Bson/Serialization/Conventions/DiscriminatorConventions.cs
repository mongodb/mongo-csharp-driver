﻿/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Bson.Serialization.Conventions {
    /// <summary>
    /// Represents a discriminator convention.
    /// </summary>
    public interface IDiscriminatorConvention {
        /// <summary>
        /// Gets the discriminator element name.
        /// </summary>
        string ElementName { get; }

        /// <summary>
        /// Gets the actual type of an object by reading the discriminator from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The reader.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <returns>The actual type.</returns>
        Type GetActualType(BsonReader bsonReader, Type nominalType);

        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        BsonValue GetDiscriminator(Type nominalType, Type actualType);
    }

    /// <summary>
    /// Represents the standard discriminator conventions (see ScalarDiscriminatorConvention and HierarchicalDiscriminatorConvention).
    /// </summary>
    public abstract class StandardDiscriminatorConvention : IDiscriminatorConvention {
        #region private static fields
        private static ScalarDiscriminatorConvention scalar = new ScalarDiscriminatorConvention("_t");
        private static HierarchicalDiscriminatorConvention hierarchical = new HierarchicalDiscriminatorConvention("_t");
        #endregion

        #region private fields
        private string elementName;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the StandardDiscriminatorConvention class.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        protected StandardDiscriminatorConvention(
            string elementName
        ) {
            this.elementName = elementName;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the ScalarDiscriminatorConvention.
        /// </summary>
        public static ScalarDiscriminatorConvention Scalar {
            get { return scalar; }
        }

        /// <summary>
        /// Gets an instance of the HierarchicalDiscriminatorConvention.
        /// </summary>
        public static HierarchicalDiscriminatorConvention Hierarchical {
            get { return hierarchical; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the discriminator element name.
        /// </summary>
        public string ElementName {
            get { return elementName; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the actual type of an object by reading the discriminator from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The reader.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <returns>The actual type.</returns>
        public Type GetActualType(
            BsonReader bsonReader,
            Type nominalType
        ) {
            // the BsonReader is sitting at the value whose actual type needs to be found
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

        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        public abstract BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType
        );
        #endregion
    }

    /// <summary>
    /// Represents a discriminator convention where the discriminator is provided by the class map of the actual type.
    /// </summary>
    public class ScalarDiscriminatorConvention : StandardDiscriminatorConvention {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the ScalarDiscriminatorConvention class.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        public ScalarDiscriminatorConvention(
            string elementName
        )
            : base(elementName) {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        public override BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType
        ) {
            var classMap = BsonClassMap.LookupClassMap(actualType);
            return classMap.Discriminator;
        }
        #endregion
    }

    /// <summary>
    /// Represents a discriminator convention where the discriminator is an array of all the discriminators provided by the class maps of the root class down to the actual type.
    /// </summary>
    public class HierarchicalDiscriminatorConvention : StandardDiscriminatorConvention {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the HierarchicalDiscriminatorConvention class.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        public HierarchicalDiscriminatorConvention(
            string elementName
        )
            : base(elementName) {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
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
