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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a BSON document.
    /// </summary>
    [Serializable]
    public class BsonDocument : BsonValue, IBsonSerializable, IComparable<BsonDocument>, IConvertibleToBsonDocument, IEnumerable<BsonElement>, IEquatable<BsonDocument>
    {
        // private fields
        // use a list and a dictionary because we want to preserve the order in which the elements were added
        // if duplicate names are present only the first one will be in the dictionary (the others can only be accessed by index)
        private List<BsonElement> _elements = new List<BsonElement>();
        private Dictionary<string, int> _indexes = new Dictionary<string, int>(); // maps names to indexes into elements list
        private bool _allowDuplicateNames;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocument class.
        /// </summary>
        public BsonDocument()
            : base(BsonType.Document)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class specifying whether duplicate element names are allowed
        /// (allowing duplicate element names is not recommended).
        /// </summary>
        /// <param name="allowDuplicateNames">Whether duplicate element names are allowed.</param>
        public BsonDocument(bool allowDuplicateNames)
            : base(BsonType.Document)
        {
            _allowDuplicateNames = allowDuplicateNames;
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds one element.
        /// </summary>
        /// <param name="element">An element to add to the document.</param>
        public BsonDocument(BsonElement element)
            : base(BsonType.Document)
        {
            Add(element);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">A dictionary to initialize the document from.</param>
        public BsonDocument(Dictionary<string, object> dictionary)
            : base(BsonType.Document)
        {
            Add(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">A dictionary to initialize the document from.</param>
        /// <param name="keys">A list of keys to select values from the dictionary.</param>
        public BsonDocument(Dictionary<string, object> dictionary, IEnumerable<string> keys)
            : base(BsonType.Document)
        {
            Add(dictionary, keys);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">A dictionary to initialize the document from.</param>
        public BsonDocument(IDictionary<string, object> dictionary)
            : base(BsonType.Document)
        {
            Add(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">A dictionary to initialize the document from.</param>
        /// <param name="keys">A list of keys to select values from the dictionary.</param>
        public BsonDocument(IDictionary<string, object> dictionary, IEnumerable<string> keys)
            : base(BsonType.Document)
        {
            Add(dictionary, keys);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">A dictionary to initialize the document from.</param>
        public BsonDocument(IDictionary dictionary)
            : base(BsonType.Document)
        {
            Add(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">A dictionary to initialize the document from.</param>
        /// <param name="keys">A list of keys to select values from the dictionary.</param>
        public BsonDocument(IDictionary dictionary, IEnumerable keys)
            : base(BsonType.Document)
        {
            Add(dictionary, keys);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds new elements from a list of elements.
        /// </summary>
        /// <param name="elements">A list of elements to add to the document.</param>
        public BsonDocument(IEnumerable<BsonElement> elements)
            : base(BsonType.Document)
        {
            Add(elements);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and adds one or more elements.
        /// </summary>
        /// <param name="elements">One or more elements to add to the document.</param>
        public BsonDocument(params BsonElement[] elements)
            : base(BsonType.Document)
        {
            Add(elements);
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocument class and creates and adds a new element.
        /// </summary>
        /// <param name="name">The name of the element to add to the document.</param>
        /// <param name="value">The value of the element to add to the document.</param>
        public BsonDocument(string name, BsonValue value)
            : base(BsonType.Document)
        {
            Add(name, value);
        }

        // public operators
        /// <summary>
        /// Compares two BsonDocument values.
        /// </summary>
        /// <param name="lhs">The first BsonDocument.</param>
        /// <param name="rhs">The other BsonDocument.</param>
        /// <returns>True if the two BsonDocument values are not equal according to ==.</returns>
        public static bool operator !=(BsonDocument lhs, BsonDocument rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two BsonDocument values.
        /// </summary>
        /// <param name="lhs">The first BsonDocument.</param>
        /// <param name="rhs">The other BsonDocument.</param>
        /// <returns>True if the two BsonDocument values are equal according to ==.</returns>
        public static bool operator ==(BsonDocument lhs, BsonDocument rhs)
        {
            if (object.ReferenceEquals(lhs, null)) { return object.ReferenceEquals(rhs, null); }
            return lhs.Equals(rhs);
        }

        // public properties
        /// <summary>
        /// Gets or sets whether to allow duplicate names (allowing duplicate names is not recommended).
        /// </summary>
        public bool AllowDuplicateNames
        {
            get { return _allowDuplicateNames; }
            set { _allowDuplicateNames = value; }
        }

        // ElementCount could be greater than the number of Names if allowDuplicateNames is true
        /// <summary>
        /// Gets the number of elements.
        /// </summary>
        public int ElementCount
        {
            get { return _elements.Count; }
        }

        /// <summary>
        /// Gets the elements.
        /// </summary>
        public IEnumerable<BsonElement> Elements
        {
            get { return _elements; }
        }

        /// <summary>
        /// Gets the element names.
        /// </summary>
        public IEnumerable<string> Names
        {
            get { return _elements.Select(e => e.Name); }
        }

        /// <summary>
        /// Gets the raw values (see BsonValue.RawValue).
        /// </summary>
        public IEnumerable<object> RawValues
        {
            get { return _elements.Select(e => e.Value.RawValue); }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        public IEnumerable<BsonValue> Values
        {
            get { return _elements.Select(e => e.Value); }
        }

        // public indexers
        // note: the return type of the indexers is BsonValue and NOT BsonElement so that we can write code like:
        //     BsonDocument car;
        //     car["color"] = "red"; // changes value of existing element or adds new element
        //         note: we are using implicit conversion from string to BsonValue
        // to convert the returned BsonValue to a .NET type you have two approaches (explicit cast or As method):
        //     string color = (string) car["color"]; // throws exception if value is not a string (returns null if not found)
        //     string color = car["color"].AsString; // throws exception if value is not a string (results in a NullReferenceException if not found)
        //     string color = car["color", "none"].AsString; // throws exception if value is not a string (default to "none" if not found)
        // the second approach offers a more fluent interface (with fewer parenthesis!)
        //     string name = car["brand"].AsBsonSymbol.Name;
        //     string name = ((BsonSymbol) car["brand"]).Name; // the extra parenthesis are required and harder to read
        // there are also some conversion methods (and note that ToBoolean uses the JavaScript definition of truthiness)
        //     bool ok = result["ok"].ToBoolean(); // works whether ok is false, true, 0, 0.0, 1, 1.0, "", "xyz", BsonNull.Value, etc...
        //     bool ok = result["ok", false].ToBoolean(); // defaults to false if ok element is not found
        //     int n = result["n"].ToInt32(); // works whether n is Int32, Int64, Double or String (if it can be parsed)
        //     long n = result["n"].ToInt64(); // works whether n is Int32, Int64, Double or String (if it can be parsed)
        //     double d = result["n"].ToDouble(); // works whether d is Int32, Int64, Double or String (if it can be parsed)
        // to work in terms of BsonElements use Add, GetElement and SetElement
        //     car.Add(new BsonElement("color", "red")); // might throw exception if allowDuplicateNames is false
        //     car.SetElement(new BsonElement("color", "red")); // replaces existing element or adds new element
        //     BsonElement colorElement = car.GetElement("color"); // returns null if element "color" is not found

        /// <summary>
        /// Gets or sets the value of an element.
        /// </summary>
        /// <param name="index">The zero based index of the element.</param>
        /// <returns>The value of the element.</returns>
        public BsonValue this[int index]
        {
            get { return _elements[index].Value; }
            set {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _elements[index].Value = value;
            }
        }

        /// <summary>
        /// Gets the value of an element or a default value if the element is not found.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="defaultValue">The default value to return if the element is not found.</param>
        /// <returns>Teh value of the element or a default value if the element is not found.</returns>
        public BsonValue this[string name, BsonValue defaultValue]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }
                int index;
                if (_indexes.TryGetValue(name, out index))
                {
                    return _elements[index].Value;
                }
                else
                {
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of an element.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The value of the element.</returns>
        public BsonValue this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }
                int index;
                if (_indexes.TryGetValue(name, out index))
                {
                    return _elements[index].Value;
                }
                else
                {
                    string message = string.Format("Element '{0}' not found.", name);
                    throw new KeyNotFoundException(message);
                }
            }
            set
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                int index;
                if (_indexes.TryGetValue(name, out index))
                {
                    _elements[index].Value = value;
                }
                else
                {
                    Add(new BsonElement(name, value));
                }
            }
        }

        // public static methods
        /// <summary>
        /// Creates a new BsonDocument by mapping an object to a BsonDocument.
        /// </summary>
        /// <param name="value">The object to be mapped to a BsonDocument.</param>
        /// <returns>A BsonDocument.</returns>
        public new static BsonDocument Create(object value)
        {
            if (value != null)
            {
                return (BsonDocument)BsonTypeMapper.MapToBsonValue(value, BsonType.Document);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a JSON string and returns a BsonDocument.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>A BsonDocument.</returns>
        public static BsonDocument Parse(string json)
        {
            using (var bsonReader = BsonReader.Create(json))
            {
                var document = new BsonDocument();
                return (BsonDocument)document.Deserialize(bsonReader, typeof(BsonDocument), null);
            }
        }

        /// <summary>
        /// Reads a BsonDocument from a BsonBuffer.
        /// </summary>
        /// <param name="buffer">The BsonBuffer.</param>
        /// <returns>A BsonDocument.</returns>
        public static BsonDocument ReadFrom(BsonBuffer buffer)
        {
            using (BsonReader bsonReader = BsonReader.Create(buffer))
            {
                return ReadFrom(bsonReader);
            }
        }

        /// <summary>
        /// Reads a BsonDocument from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <returns>A BsonDocument.</returns>
        public static new BsonDocument ReadFrom(BsonReader bsonReader)
        {
            BsonDocument document = new BsonDocument();
            return (BsonDocument)document.Deserialize(bsonReader, typeof(BsonDocument), null);
        }

        /// <summary>
        /// Reads a BsonDocument from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A BsonDocument.</returns>
        public static BsonDocument ReadFrom(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonReader bsonReader = BsonReader.Create(stream))
            {
                return ReadFrom(bsonReader);
            }
        }

        /// <summary>
        /// Reads a BsonDocument from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A BsonDocument.</returns>
        public static BsonDocument ReadFrom(Stream stream)
        {
            using (BsonReader bsonReader = BsonReader.Create(stream))
            {
                return ReadFrom(bsonReader);
            }
        }

        /// <summary>
        /// Reads a BsonDocument from a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <returns>A BsonDocument.</returns>
        public static BsonDocument ReadFrom(string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            using (BsonReader bsonReader = BsonReader.Create(stream))
            {
                return ReadFrom(bsonReader);
            }
        }

        // public methods
        /// <summary>
        /// Adds an element to the document.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(BsonElement element)
        {
            if (element != null)
            {
                bool found;
                int index;
                if ((found = _indexes.TryGetValue(element.Name, out index)) && !_allowDuplicateNames)
                {
                    var message = string.Format("Duplicate element name '{0}'.", element.Name);
                    throw new InvalidOperationException(message);
                }
                else
                {
                    _elements.Add(element);
                    if (!found)
                    {
                        _indexes.Add(element.Name, _elements.Count - 1); // index of the newly added element
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Adds elements to the document from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(Dictionary<string, object> dictionary)
        {
            return Add((IDictionary<string, object>)dictionary);
        }

        /// <summary>
        /// Adds elements to the document from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="keys">Which keys of the hash table to add.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(Dictionary<string, object> dictionary, IEnumerable<string> keys)
        {
            return Add((IDictionary<string, object>)dictionary, keys);
        }

        /// <summary>
        /// Adds elements to the document from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(IDictionary<string, object> dictionary)
        {
            if (dictionary != null)
            {
                foreach (var entry in dictionary)
                {
                    Add(entry.Key, BsonTypeMapper.MapToBsonValue(entry.Value));
                }
            }
            return this;
        }

        /// <summary>
        /// Adds elements to the document from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="keys">Which keys of the hash table to add.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(IDictionary<string, object> dictionary, IEnumerable<string> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            if (dictionary != null)
            {
                foreach (var key in keys)
                {
                    Add(key, BsonTypeMapper.MapToBsonValue(dictionary[key]));
                }
            }
            return this;
        }

        /// <summary>
        /// Adds elements to the document from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(IDictionary dictionary)
        {
            if (dictionary != null)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key.GetType() != typeof(string))
                    {
                        throw new ArgumentOutOfRangeException("One or more keys in the dictionary passed to BsonDocument.Add is not a string.");
                    }
                    Add((string)entry.Key, BsonTypeMapper.MapToBsonValue(entry.Value));
                }
            }
            return this;
        }

        /// <summary>
        /// Adds elements to the document from a dictionary of key/value pairs.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="keys">Which keys of the hash table to add.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(IDictionary dictionary, IEnumerable keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            if (dictionary != null)
            {
                foreach (var key in keys)
                {
                    if (key.GetType() != typeof(string))
                    {
                        throw new ArgumentOutOfRangeException("A key passed to BsonDocument.Add is not a string.");
                    }
                    Add((string)key, BsonTypeMapper.MapToBsonValue(dictionary[key]));
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a list of elements to the document.
        /// </summary>
        /// <param name="elements">The list of elements.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(IEnumerable<BsonElement> elements)
        {
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    Add(element);
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a list of elements to the document.
        /// </summary>
        /// <param name="elements">The list of elements.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(params BsonElement[] elements)
        {
            return Add((IEnumerable<BsonElement>)elements);
        }

        /// <summary>
        /// Creates and adds an element to the document.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value != null)
            {
                Add(new BsonElement(name, value));
            }
            return this;
        }

        /// <summary>
        /// Creates and adds an element to the document, but only if the condition is true.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <param name="condition">Whether to add the element to the document.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Add(string name, BsonValue value, bool condition)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value != null && condition)
            {
                Add(new BsonElement(name, value));
            }
            return this;
        }

        /// <summary>
        /// Clears the document (removes all elements).
        /// </summary>
        public void Clear()
        {
            _elements.Clear();
            _indexes.Clear();
        }

        /// <summary>
        /// Creates a shallow clone of the document (see also DeepClone).
        /// </summary>
        /// <returns>A shallow clone of the document.</returns>
        public override BsonValue Clone()
        {
            BsonDocument clone = new BsonDocument();
            foreach (BsonElement element in _elements)
            {
                clone.Add(element.Clone());
            }
            return clone;
        }

        /// <summary>
        /// Compares this document to another document.
        /// </summary>
        /// <param name="other">The other document.</param>
        /// <returns>A 32-bit signed integer that indicates whether this document is less than, equal to, or greather than the other.</returns>
        public int CompareTo(BsonDocument other)
        {
            if (other == null) { return 1; }
            for (int i = 0; i < _elements.Count && i < other._elements.Count; i++)
            {
                int r = _elements[i].Name.CompareTo(other._elements[i].Name);
                if (r != 0) { return r; }
                r = _elements[i].Value.CompareTo(other._elements[i].Value);
                if (r != 0) { return r; }
            }
            return _elements.Count.CompareTo(other._elements.Count);
        }

        /// <summary>
        /// Compares the BsonDocument to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDocument is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(BsonValue other)
        {
            if (other == null) { return 1; }
            var otherDocument = other as BsonDocument;
            if (otherDocument != null)
            {
                return CompareTo(otherDocument);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Tests whether the document contains an element with the specified name.
        /// </summary>
        /// <param name="name">The name of the element to look for.</param>
        /// <returns>True if the document contains an element with the specified name.</returns>
        public bool Contains(string name)
        {
            return _indexes.ContainsKey(name);
        }

        /// <summary>
        /// Tests whether the document contains an element with the specified value.
        /// </summary>
        /// <param name="value">The value of the element to look for.</param>
        /// <returns>True if the document contains an element with the specified value.</returns>
        public bool ContainsValue(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return _elements.Any(e => e.Value == value);
        }

        /// <summary>
        /// Creates a deep clone of the document (see also Clone).
        /// </summary>
        /// <returns>A deep clone of the document.</returns>
        public override BsonValue DeepClone()
        {
            BsonDocument clone = new BsonDocument();
            foreach (BsonElement element in _elements)
            {
                clone.Add(element.DeepClone());
            }
            return clone;
        }

        /// <summary>
        /// Deserializes the document from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object (ignored, but should be BsonDocument).</param>
        /// <param name="options">The serialization options (ignored).</param>
        /// <returns>The document (which has now been initialized by deserialization), or null.</returns>
        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            if (bsonReader.GetCurrentBsonType() == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var documentSerializationOptions = (options ?? DocumentSerializationOptions.Defaults) as DocumentSerializationOptions;
                if (documentSerializationOptions == null)
                {
                    var message = string.Format(
                        "Serialize method of BsonDocument expected serialization options of type {0}, not {1}.",
                        BsonUtils.GetFriendlyTypeName(typeof(DocumentSerializationOptions)),
                        BsonUtils.GetFriendlyTypeName(options.GetType()));
                    throw new BsonSerializationException(message);
                }
                _allowDuplicateNames = documentSerializationOptions.AllowDuplicateNames;
                
                bsonReader.ReadStartDocument();
                Clear();
                BsonElement element;
                while (BsonElement.ReadFrom(bsonReader, out element))
                {
                    Add(element);
                }
                bsonReader.ReadEndDocument();
                return this;
            }
        }

        /// <summary>
        /// Gets the Id of the document.
        /// </summary>
        /// <param name="id">The Id of the document (the RawValue if it has one, otherwise the element Value).</param>
        /// <param name="idNominalType">The nominal type of the Id.</param>
        /// <param name="idGenerator">The IdGenerator for the Id (or null).</param>
        /// <returns>True (a BsonDocument either has an Id member or one can be added).</returns>
        public bool GetDocumentId(out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            BsonElement idElement;
            if (TryGetElement("_id", out idElement))
            {
                id = idElement.Value;
                idGenerator = BsonSerializer.LookupIdGenerator(id.GetType());

                if (idGenerator == null)
                {
                    var idBinaryData = id as BsonBinaryData;
                    if (idBinaryData != null && (idBinaryData.SubType == BsonBinarySubType.UuidLegacy || idBinaryData.SubType == BsonBinarySubType.UuidStandard))
                    {
                        idGenerator = BsonBinaryDataGuidGenerator.GetInstance(idBinaryData.GuidRepresentation);
                    }
                }
            }
            else
            {
                id = null;
                idGenerator = BsonObjectIdGenerator.Instance;
            }

            idNominalType = typeof(BsonValue);
            return true;
        }

        /// <summary>
        /// Compares this document to another document.
        /// </summary>
        /// <param name="rhs">The other document.</param>
        /// <returns>True if the two documents are equal.</returns>
        public bool Equals(BsonDocument rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return object.ReferenceEquals(this, rhs) || _elements.SequenceEqual(rhs._elements);
        }

        /// <summary>
        /// Compares this BsonDocument to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonDocument and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BsonDocument); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets an element of this document.
        /// </summary>
        /// <param name="index">The zero based index of the element.</param>
        /// <returns>The element.</returns>
        public BsonElement GetElement(int index)
        {
            return _elements[index];
        }

        /// <summary>
        /// Gets an element of this document.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A BsonElement.</returns>
        public BsonElement GetElement(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            int index;
            if (_indexes.TryGetValue(name, out index))
            {
                return _elements[index];
            }
            else
            {
                string message = string.Format("Element '{0}' not found.", name);
                throw new KeyNotFoundException(message);
            }
        }

        /// <summary>
        /// Gets an enumerator that can be used to enumerate the elements of this document.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public IEnumerator<BsonElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * BsonType.GetHashCode();
            foreach (BsonElement element in _elements)
            {
                hash = 37 * hash + element.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Gets the value of an element.
        /// </summary>
        /// <param name="index">The zero based index of the element.</param>
        /// <returns>The value of the element.</returns>
        public BsonValue GetValue(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Gets the value of an element.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The value of the element.</returns>
        public BsonValue GetValue(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return this[name];
        }

        /// <summary>
        /// Gets the value of an element or a default value if the element is not found.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="defaultValue">The default value returned if the element is not found.</param>
        /// <returns>The value of the element or the default value if the element is not found.</returns>
        public BsonValue GetValue(string name, BsonValue defaultValue)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return this[name, defaultValue];
        }

        /// <summary>
        /// Inserts a new element at a specified position.
        /// </summary>
        /// <param name="index">The position of the new element.</param>
        /// <param name="element">The element.</param>
        public void InsertAt(int index, BsonElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (_indexes.ContainsKey(element.Name) && !_allowDuplicateNames)
            {
                var message = string.Format("Duplicate element name '{0}' not allowed.", element.Name);
                throw new InvalidOperationException(message);
            }
            else
            {
                _elements.Insert(index, element);
                RebuildDictionary();
            }
        }

        /// <summary>
        /// Merges another document into this one. Existing elements are not overwritten.
        /// </summary>
        /// <param name="document">The other document.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Merge(BsonDocument document)
        {
            Merge(document, false); // don't overwriteExistingElements
            return this;
        }

        /// <summary>
        /// Merges another document into this one, specifying whether existing elements are overwritten.
        /// </summary>
        /// <param name="document">The other document.</param>
        /// <param name="overwriteExistingElements">Whether to overwrite existing elements.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Merge(BsonDocument document, bool overwriteExistingElements)
        {
            if (document != null)
            {
                foreach (BsonElement element in document)
                {
                    if (overwriteExistingElements || !Contains(element.Name))
                    {
                        this[element.Name] = element.Value;
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Removes an element from this document (if duplicate element names are allowed
        /// then all elements with this name will be removed).
        /// </summary>
        /// <param name="name">The name of the element to remove.</param>
        public void Remove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (_indexes.ContainsKey(name))
            {
                _elements.RemoveAll(e => e.Name == name);
                RebuildDictionary();
            }
        }

        /// <summary>
        /// Removes an element from this document.
        /// </summary>
        /// <param name="index">The zero based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            _elements.RemoveAt(index);
            RebuildDictionary();
        }

        /// <summary>
        /// Removes an element from this document.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        public void RemoveElement(BsonElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            _elements.Remove(element);
            RebuildDictionary();
        }

        /// <summary>
        /// Serializes this document to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominalType.</param>
        /// <param name="options">The serialization options (can be null).</param>
        public void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            if (bsonWriter == null)
            {
                throw new ArgumentNullException("bsonWriter");
            }
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            var documentSerializationOptions = (options ?? DocumentSerializationOptions.Defaults) as DocumentSerializationOptions;
            if (documentSerializationOptions == null)
            {
                var message = string.Format(
                    "Serialize method of BsonDocument expected serialization options of type {0}, not {1}.",
                    BsonUtils.GetFriendlyTypeName(typeof(DocumentSerializationOptions)),
                    BsonUtils.GetFriendlyTypeName(options.GetType()));
                throw new BsonSerializationException(message);
            }

            bsonWriter.WriteStartDocument();
            int idIndex;
            if (documentSerializationOptions.SerializeIdFirst && _indexes.TryGetValue("_id", out idIndex))
            {
                _elements[idIndex].WriteTo(bsonWriter);
            }
            else
            {
                idIndex = -1; // remember that when TryGetValue returns false it sets idIndex to 0
            }

            for (int i = 0; i < _elements.Count; i++)
            {
                // if serializeIdFirst is false then idIndex will be -1 and no elements will be skipped
                if (i != idIndex)
                {
                    _elements[i].WriteTo(bsonWriter);
                }
            }

            bsonWriter.WriteEndDocument();
        }

        /// <summary>
        /// Sets the value of an element.
        /// </summary>
        /// <param name="index">The zero based index of the element whose value is to be set.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Set(int index, BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            this[index] = value;
            return this;
        }

        /// <summary>
        /// Sets the value of an element (an element will be added if no element with this name is found).
        /// </summary>
        /// <param name="name">The name of the element whose value is to be set.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The document (so method calls can be chained).</returns>
        public BsonDocument Set(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            this[name] = value;
            return this;
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="id">The value of the Id.</param>
        public void SetDocumentId(object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            var idBsonValue = (BsonValue)id;

            BsonElement idElement;
            if (TryGetElement("_id", out idElement))
            {
                idElement.Value = idBsonValue;
            }
            else
            {
                InsertAt(0, new BsonElement("_id", idBsonValue));
            }
        }

        /// <summary>
        /// Sets an element of the document (replacing the existing element at that position).
        /// </summary>
        /// <param name="index">The zero based index of the element to replace.</param>
        /// <param name="element">The new element.</param>
        /// <returns>The document.</returns>
        public BsonDocument SetElement(int index, BsonElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            _elements[index] = element;
            RebuildDictionary();
            return this;
        }

        /// <summary>
        /// Sets an element of the document (replaces any existing element with the same name or adds a new element if an element with the same name is not found).
        /// </summary>
        /// <param name="element">The new element.</param>
        /// <returns>The document.</returns>
        public BsonDocument SetElement(BsonElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            int index;
            if (_indexes.TryGetValue(element.Name, out index))
            {
                _elements[index] = element;
            }
            else
            {
                Add(element);
            }
            return this;
        }

        /// <summary>
        /// Converts the BsonDocument to a Dictionary&lt;string, object&gt;.
        /// </summary>
        /// <returns>A dictionary.</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var options = new BsonTypeMapperOptions
            {
                DuplicateNameHandling = DuplicateNameHandling.ThrowException,
                MapBsonArrayTo = typeof(object[]), // TODO: should this be List<object>?
                MapBsonDocumentTo = typeof(Dictionary<string, object>),
                MapOldBinaryToByteArray = false
            };
            return (Dictionary<string, object>)BsonTypeMapper.MapToDotNetValue(this, options);
        }

        /// <summary>
        /// Converts the BsonDocument to a Hashtable.
        /// </summary>
        /// <returns>A hashtable.</returns>
        public Hashtable ToHashtable()
        {
            var options = new BsonTypeMapperOptions
            {
                DuplicateNameHandling = DuplicateNameHandling.ThrowException,
                MapBsonArrayTo = typeof(object[]), // TODO: should this be ArrayList?
                MapBsonDocumentTo = typeof(Hashtable),
                MapOldBinaryToByteArray = false
            };
            return (Hashtable)BsonTypeMapper.MapToDotNetValue(this, options);
        }

        /// <summary>
        /// Returns a string representation of the document.
        /// </summary>
        /// <returns>A string representation of the document.</returns>
        public override string ToString()
        {
            return this.ToJson();
        }

        /// <summary>
        /// Tries to get an element of this document.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The element.</param>
        /// <returns>True if an element with that name was found.</returns>
        public bool TryGetElement(string name, out BsonElement value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            int index;
            if (_indexes.TryGetValue(name, out index))
            {
                value = _elements[index];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to get the value of an element of this document.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>True if an element with that name was found.</returns>
        public bool TryGetValue(string name, out BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            int index;
            if (_indexes.TryGetValue(name, out index))
            {
                value = _elements[index].Value;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Writes the document to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        public new void WriteTo(BsonWriter bsonWriter)
        {
            Serialize(bsonWriter, typeof(BsonDocument), null);
        }

        /// <summary>
        /// Writes the document to a BsonBuffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public void WriteTo(BsonBuffer buffer)
        {
            using (BsonWriter bsonWriter = BsonWriter.Create(buffer))
            {
                WriteTo(bsonWriter);
            }
        }

        /// <summary>
        /// Writes the document to a Stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void WriteTo(Stream stream)
        {
            using (BsonWriter bsonWriter = BsonWriter.Create(stream))
            {
                WriteTo(bsonWriter);
            }
        }

        /// <summary>
        /// Writes the document to a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        public void WriteTo(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                WriteTo(stream);
            }
        }

        // private methods
        private void RebuildDictionary()
        {
            _indexes.Clear();
            for (int index = 0; index < _elements.Count; index++)
            {
                BsonElement element = _elements[index];
                if (!_indexes.ContainsKey(element.Name))
                {
                    _indexes.Add(element.Name, index);
                }
            }
        }

        // explicit interface implementations
        BsonDocument IConvertibleToBsonDocument.ToBsonDocument()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _elements.GetEnumerator();
        }
    }
}
