/* Copyright 2010-2012 10gen Inc.
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

using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.IdGenerators
{
    /// <summary>
    /// Represents an Id generator for Guids stored in BsonBinaryData values.
    /// </summary>
    public class BsonBinaryDataGuidGenerator : IIdGenerator
    {
        // private static fields
        private static BsonBinaryDataGuidGenerator __csharpLegacyInstance = new BsonBinaryDataGuidGenerator(GuidRepresentation.CSharpLegacy);
        private static BsonBinaryDataGuidGenerator __javaLegacyInstance = new BsonBinaryDataGuidGenerator(GuidRepresentation.JavaLegacy);
        private static BsonBinaryDataGuidGenerator __pythonLegacyInstance = new BsonBinaryDataGuidGenerator(GuidRepresentation.PythonLegacy);
        private static BsonBinaryDataGuidGenerator __standardInstance = new BsonBinaryDataGuidGenerator(GuidRepresentation.Standard);
        private static BsonBinaryDataGuidGenerator __unspecifiedInstance = new BsonBinaryDataGuidGenerator(GuidRepresentation.Unspecified);
        private static byte[] __emptyGuidBytes = Guid.Empty.ToByteArray();

        // private fields
        private GuidRepresentation _guidRepresentation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryDataGuidGenerator class.
        /// </summary>
        /// <param name="guidRepresentation">The GuidRepresentation to use when generating new Id values.</param>
        public BsonBinaryDataGuidGenerator(GuidRepresentation guidRepresentation)
        {
            _guidRepresentation = guidRepresentation;
        }

        // public static properties
        /// <summary>
        /// Gets an instance of BsonBinaryDataGuidGenerator for CSharpLegacy GuidRepresentation.
        /// </summary>
        public static BsonBinaryDataGuidGenerator CSharpLegacyInstance
        {
            get { return __csharpLegacyInstance; }
        }

        /// <summary>
        /// Gets an instance of BsonBinaryDataGuidGenerator for JavaLegacy GuidRepresentation.
        /// </summary>
        public static BsonBinaryDataGuidGenerator JavaLegacyInstance
        {
            get { return __javaLegacyInstance; }
        }

        /// <summary>
        /// Gets an instance of BsonBinaryDataGuidGenerator for PythonLegacy GuidRepresentation.
        /// </summary>
        public static BsonBinaryDataGuidGenerator PythonLegacyInstance
        {
            get { return __pythonLegacyInstance; }
        }

        /// <summary>
        /// Gets an instance of BsonBinaryDataGuidGenerator for Standard GuidRepresentation.
        /// </summary>
        public static BsonBinaryDataGuidGenerator StandardInstance
        {
            get { return __standardInstance; }
        }

        /// <summary>
        /// Gets an instance of BsonBinaryDataGuidGenerator for Unspecifed GuidRepresentation.
        /// </summary>
        public static BsonBinaryDataGuidGenerator UnspecifedInstance
        {
            get { return __unspecifiedInstance; }
        }

        // public static methods
        /// <summary>
        /// Gets the instance of BsonBinaryDataGuidGenerator for a GuidRepresentation.
        /// </summary>
        /// <param name="guidRepresentation">The GuidRepresentation.</param>
        /// <returns>The instance of BsonBinaryDataGuidGenerator for a GuidRepresentation.</returns>
        public static BsonBinaryDataGuidGenerator GetInstance(GuidRepresentation guidRepresentation)
        {
            switch (guidRepresentation)
            {
                case GuidRepresentation.CSharpLegacy: return __csharpLegacyInstance;
                case GuidRepresentation.JavaLegacy: return __javaLegacyInstance;
                case GuidRepresentation.PythonLegacy: return __pythonLegacyInstance;
                case GuidRepresentation.Standard: return __standardInstance;
                case GuidRepresentation.Unspecified: return __unspecifiedInstance;
                default: throw new ArgumentOutOfRangeException("guidRepresentation");
            }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            return BsonBinaryData.Create(Guid.NewGuid(), _guidRepresentation);
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            if (id == null || ((BsonValue)id).IsBsonNull)
            {
                return true;
            }

            var idBsonBinaryData = (BsonBinaryData)id;
            var subType = idBsonBinaryData.SubType;
            if (subType != BsonBinarySubType.UuidLegacy && subType != BsonBinarySubType.UuidStandard)
            {
                throw new ArgumentOutOfRangeException("The binary sub type of the id value passed to the BsonBinaryDataGuidGenerator IsEmpty method is not UuidLegacy or UuidStandard.");
            }
            return idBsonBinaryData.Bytes.SequenceEqual(__emptyGuidBytes);
        }
    }

    /// <summary>
    /// Represents an Id generator for BsonObjectIds.
    /// </summary>
    public class BsonObjectIdGenerator : IIdGenerator
    {
        // private static fields
        private static BsonObjectIdGenerator __instance = new BsonObjectIdGenerator();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonObjectIdGenerator class.
        /// </summary>
        public BsonObjectIdGenerator()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of ObjectIdGenerator.
        /// </summary>
        public static BsonObjectIdGenerator Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            return BsonObjectId.GenerateNewId();
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null || ((BsonValue)id).IsBsonNull || ((BsonObjectId)id).Value == ObjectId.Empty;
        }
    }

    /// <summary>
    /// Represents an Id generator for Guids using the COMB algorithm.
    /// </summary>
    public class CombGuidGenerator : IIdGenerator
    {
        // private static fields
        private static CombGuidGenerator __instance = new CombGuidGenerator();

        // constructors
        /// <summary>
        /// Initializes a new instance of the CombGuidGenerator class.
        /// </summary>
        public CombGuidGenerator()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of CombGuidGenerator.
        /// </summary>
        public static CombGuidGenerator Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            var baseDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            var days = (ushort)(now - baseDate).TotalDays;
            var milliseconds = (int)now.TimeOfDay.TotalMilliseconds;

            // replace last 6 bytes of a new Guid with 2 bytes from days and 4 bytes from milliseconds
            // see: The Cost of GUIDs as Primary Keys by Jimmy Nilson
            // at: http://www.informit.com/articles/article.aspx?p=25862&seqNum=7

            var bytes = Guid.NewGuid().ToByteArray();
            Array.Copy(BitConverter.GetBytes(days), 0, bytes, 10, 2);
            Array.Copy(BitConverter.GetBytes(milliseconds), 0, bytes, 12, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 10, 2);
                Array.Reverse(bytes, 12, 4);
            }
            return new Guid(bytes);
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null || (Guid)id == Guid.Empty;
        }
    }

    /// <summary>
    /// Represents an Id generator for Guids.
    /// </summary>
    public class GuidGenerator : IIdGenerator
    {
        // private static fields
        private static GuidGenerator __instance = new GuidGenerator();

        // constructors
        /// <summary>
        /// Initializes a new instance of the GuidGenerator class.
        /// </summary>
        public GuidGenerator()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of GuidGenerator.
        /// </summary>
        public static GuidGenerator Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null || (Guid)id == Guid.Empty;
        }
    }

    /// <summary>
    /// Represents an Id generator that only checks that the Id is not null.
    /// </summary>
    public class NullIdChecker : IIdGenerator
    {
        // private static fields
        private static NullIdChecker __instance = new NullIdChecker();

        // constructors
        /// <summary>
        /// Initializes a new instance of the NullIdChecker class.
        /// </summary>
        public NullIdChecker()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of NullIdChecker.
        /// </summary>
        public static NullIdChecker Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            throw new InvalidOperationException("Id cannot be null.");
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null;
        }
    }

    /// <summary>
    /// Represents an Id generator for ObjectIds.
    /// </summary>
    public class ObjectIdGenerator : IIdGenerator
    {
        // private static fields
        private static ObjectIdGenerator __instance = new ObjectIdGenerator();

        // constructors
        /// <summary>
        /// Initializes a new instance of the ObjectIdGenerator class.
        /// </summary>
        public ObjectIdGenerator()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of ObjectIdGenerator.
        /// </summary>
        public static ObjectIdGenerator Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            return ObjectId.GenerateNewId();
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null || (ObjectId)id == ObjectId.Empty;
        }
    }

    /// <summary>
    /// Represents an Id generator for ObjectIds represented internally as strings.
    /// </summary>
    public class StringObjectIdGenerator : IIdGenerator
    {
        // private static fields
        private static StringObjectIdGenerator __instance = new StringObjectIdGenerator();

        // constructors
        /// <summary>
        /// Initializes a new instance of the StringObjectIdGenerator class.
        /// </summary>
        public StringObjectIdGenerator()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of StringObjectIdGenerator.
        /// </summary>
        public static StringObjectIdGenerator Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            return ObjectId.GenerateNewId().ToString();
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return string.IsNullOrEmpty((string)id);
        }
    }

    /// <summary>
    /// Represents an Id generator that only checks that the Id is not all zeros.
    /// </summary>
    /// <typeparam name="T">The type of the Id.</typeparam>
    // TODO: is it worth trying to remove the dependency on IEquatable<T>?
    public class ZeroIdChecker<T> : IIdGenerator where T : struct, IEquatable<T>
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the ZeroIdChecker class.
        /// </summary>
        public ZeroIdChecker()
        {
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            throw new InvalidOperationException("Id cannot be default value (all zeros).");
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null || ((T)id).Equals(default(T));
        }
    }
}
