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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer;

namespace MongoDB.Bson {
    [Serializable]
    public class BsonDocument : BsonValue, IBsonSerializable, IComparable<BsonDocument>, IConvertibleToBsonDocument, IEnumerable<BsonElement>, IEquatable<BsonDocument> {
        #region private fields
        // use a list and a dictionary because we want to preserve the order in which the elements were added
        // if duplicate names are present only the first one will be in the dictionary (the others can only be accessed by index)
        private List<BsonElement> elements = new List<BsonElement>();
        private Dictionary<string, int> indexes = new Dictionary<string, int>(); // maps names to indexes into elements list
        private bool allowDuplicateNames;
        #endregion

        #region constructors
        public BsonDocument()
            : base(BsonType.Document) {
        }

        public BsonDocument(
            bool allowDuplicateNames
        )
            : base(BsonType.Document) {
            this.allowDuplicateNames = allowDuplicateNames;
        }

        public BsonDocument(
            BsonElement element
        )
            : base(BsonType.Document) {
            Add(element);
        }

        public BsonDocument(
            IDictionary<string, object> dictionary
        )
            : base(BsonType.Document) {
            Add(dictionary);
        }

        public BsonDocument(
            IDictionary<string, object> dictionary,
            IEnumerable<string> keys
        )
            : base(BsonType.Document) {
            Add(dictionary, keys);
        }

        public BsonDocument(
            IEnumerable<BsonElement> elements
        )
            : base(BsonType.Document) {
            Add(elements);
        }

        public BsonDocument(
            params BsonElement[] elements
        )
            : base(BsonType.Document) {
            Add(elements);
        }

        public BsonDocument(
            string name,
            BsonValue value
        )
            : base(BsonType.Document) {
            Add(name, value);
        }
        #endregion

        #region public properties
        public bool AllowDuplicateNames {
            get { return allowDuplicateNames; }
            set { allowDuplicateNames = value; }
        }

        // ElementCount could be greater than the number of Names if allowDuplicateNames is true
        public int ElementCount {
            get { return elements.Count; }
        }

        public IEnumerable<BsonElement> Elements {
            get { return elements; }
        }

        public IEnumerable<string> Names {
            get { return elements.Select(e => e.Name); }
        }

        public IEnumerable<object> RawValues {
            get { return elements.Select(e => e.Value.RawValue); }
        }

        public IEnumerable<BsonValue> Values {
            get { return elements.Select(e => e.Value); }
        }
        #endregion

        #region public indexers
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

        public BsonValue this[
            int index
        ] {
            get { return elements[index].Value; }
            set { elements[index].Value = value; }
        }

        public BsonValue this[
            string name,
            BsonValue defaultValue
        ] {
            get {
                int index;
                if (indexes.TryGetValue(name, out index)) {
                    return elements[index].Value;
                } else {
                    return defaultValue;
                }
            }
        }

        public BsonValue this[
            string name
        ] {
            get {
                int index;
                if (indexes.TryGetValue(name, out index)) {
                    return elements[index].Value;
                } else {
                    string message = string.Format("Element \"{0}\" not found", name);
                    throw new KeyNotFoundException(message);
                }
            }
            set {
                int index;
                if (indexes.TryGetValue(name, out index)) {
                    elements[index].Value = value;
                } else {
                    Add(new BsonElement(name, value));
                }
            }
        }
        #endregion

        #region public static methods
        public new static BsonDocument Create(
            object value
        ) {
            if (value != null) {
                return (BsonDocument) BsonTypeMapper.MapToBsonValue(value, BsonType.Document);
            } else {
                return null;
            }
        }

        public static BsonDocument ReadFrom(
            BsonBuffer buffer
        ) {
            using (BsonReader bsonReader = BsonReader.Create(buffer)) {
                return ReadFrom(bsonReader);
            }
        }

        public static new BsonDocument ReadFrom(
            BsonReader bsonReader
        ) {
            BsonDocument document = new BsonDocument();
            return (BsonDocument) document.Deserialize(bsonReader, typeof(BsonDocument), null);
        }

        public static BsonDocument ReadFrom(
            byte[] bytes
        ) {
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonReader bsonReader = BsonReader.Create(stream)) {
                return ReadFrom(bsonReader);
            }
        }

        public static BsonDocument ReadFrom(
            Stream stream
        ) {
            using (BsonReader bsonReader = BsonReader.Create(stream)) {
                return ReadFrom(bsonReader);
            }
        }

        public static BsonDocument ReadFrom(
            string filename
        ) {
            FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            using (BsonReader bsonReader = BsonReader.Create(stream)) {
                return ReadFrom(bsonReader);
            }
        }

        public static BsonDocumentWrapper Wrap<T>(
            T value
        ) {
            if (value != null) {
                return new BsonDocumentWrapper(typeof(T), value);
            } else {
                return null;
            }
        }

        public static IEnumerable<BsonDocumentWrapper> WrapMultiple<T>(
            IEnumerable<T> values
        ) {
            if (values != null) {
                return values.Where(v => v != null).Select(v => new BsonDocumentWrapper(typeof(T), v));
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public BsonDocument Add(
            BsonElement element
        ) {
            if (element != null) {
                bool found;
                int index;
                if ((found = indexes.TryGetValue(element.Name, out index)) && !allowDuplicateNames) {
                    throw new InvalidOperationException("Duplicate element names not allowed");
                } else {
                    elements.Add(element);
                    if (!found) {
                        indexes.Add(element.Name, elements.Count - 1); // index of the newly added element
                    }
                }
            }
            return this;
        }

        public BsonDocument Add(
            IDictionary<string, object> dictionary
        ) {
            if (dictionary != null) {
                Add(dictionary, dictionary.Keys);
            }
            return this;
        }

        public BsonDocument Add(
            IDictionary<string, object> dictionary,
            IEnumerable<string> keys
        ) {
            if (dictionary != null) {
                foreach (var key in keys) {
                    Add(key, BsonValue.Create(dictionary[key]));
                }
            }
            return this;
        }

        public BsonDocument Add(
            IEnumerable<BsonElement> elements
        ) {
            if (elements != null) {
                foreach (var element in elements) {
                    Add(element);
                }
            }
            return this;
        }

        public BsonDocument Add(
            string name,
            BsonValue value
        ) {
            if (value != null) {
                Add(new BsonElement(name, value));
            }
            return this;
        }

        public BsonDocument Add(
            string name,
            BsonValue value,
            bool condition
        ) {
            if (condition) {
                Add(name, value);
            }
            return this;
        }

        public void Clear() {
            elements.Clear();
            indexes.Clear();
        }

        public override BsonValue Clone() {
            BsonDocument clone = new BsonDocument();
            foreach (BsonElement element in elements) {
                clone.Add(element.Clone());
            }
            return clone;
        }

        public int CompareTo(
            BsonDocument other
        ) {
            if (other == null) { return 1; }
            for (int i = 0; i < elements.Count && i < other.elements.Count; i++) {
                int r = elements[i].Name.CompareTo(other.elements[i].Name);
                if (r != 0) { return r; }
                r = elements[i].Value.CompareTo(other.elements[i].Value);
                if (r != 0) { return r; }
            }
            return elements.Count.CompareTo(other.elements.Count);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherDocument = other as BsonDocument;
            if (otherDocument != null) {
                return CompareTo(otherDocument);
            }
            return CompareTypeTo(other);
        }

        public bool Contains(
            string name
        ) {
            return indexes.ContainsKey(name);
        }

        public bool ContainsValue(
            BsonValue value
        ) {
            return elements.Any(e => e.Value == value);
        }

        public override BsonValue DeepClone() {
            BsonDocument clone = new BsonDocument();
            foreach (BsonElement element in elements) {
                clone.Add(element.DeepClone());
            }
            return clone;
        }

        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            if (bsonReader.CurrentBsonType == Bson.BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartDocument();
                Clear();
                BsonElement element;
                while (BsonElement.ReadFrom(bsonReader, out element)) {
                    Add(element);
                }
                bsonReader.ReadEndDocument();
                return this;
            }
        }

        // note: always returns true (if necessary SetDocumentId will add an _id element)
        public bool GetDocumentId(
            out object id,
            out IIdGenerator idGenerator
        ) {
            BsonElement idElement;
            if (TryGetElement("_id", out idElement)) {
                id = idElement.Value.RawValue;
                if (id != null) {
                    idGenerator = BsonSerializer.LookupIdGenerator(id.GetType());
                } else {
                    idGenerator = ObjectIdGenerator.Instance;
                }
            } else {
                id = null;
                idGenerator = ObjectIdGenerator.Instance;
            }
            return true;
        }

        public bool Equals(
            BsonDocument rhs
        ) {
            if (rhs == null) { return false; }
            return object.ReferenceEquals(this, rhs) || this.elements.SequenceEqual(rhs.elements);
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonDocument); // works even if obj is null
        }

        public BsonElement GetElement(
            int index
        ) {
            return elements[index];
        }

        public BsonElement GetElement(
            string name
        ) {
            int index;
            if (indexes.TryGetValue(name, out index)) {
                return elements[index];
            } else {
                string message = string.Format("Element \"{0}\" not found", name);
                throw new KeyNotFoundException(message);
            }
        }

        public IEnumerator<BsonElement> GetEnumerator() {
            return elements.GetEnumerator();
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * bsonType.GetHashCode();
            foreach (BsonElement element in elements) {
                hash = 37 * hash + element.GetHashCode();
            }
            return hash;
        }

        public BsonValue GetValue(
            int index
        ) {
            return this[index];
        }

        public BsonValue GetValue(
            string name
        ) {
            return this[name];
        }

        public BsonValue GetValue(
            string name,
            BsonValue defaultValue
        ) {
            return this[name, defaultValue];
        }

        public void InsertAt(
            int index,
            BsonElement element
        ) {
            if (element != null) {
                if (indexes.ContainsKey(element.Name) && !allowDuplicateNames) {
                    throw new InvalidOperationException("Duplicate element names not allowed");
                } else {
                    elements.Insert(index, element);
                    RebuildDictionary();
                }
            }
        }

        public BsonDocument Merge(
            BsonDocument document
        ) {
            if (document != null) {
                foreach (BsonElement element in document) {
                    if (!Contains(element.Name)) {
                        Add(element);
                    }
                }
            }
            return this;
        }

        // if multiple elements have the same name they will all be removed
        public void Remove(
            string name
        ) {
            if (indexes.ContainsKey(name)) {
                elements.RemoveAll(e => e.Name == name);
                RebuildDictionary();
            }
        }

        public void RemoveAt(
            int index
        ) {
            elements.RemoveAt(index);
            RebuildDictionary();
        }

        public void RemoveElement(
            BsonElement element
        ) {
            elements.Remove(element);
            RebuildDictionary();
        }

        public void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            bsonWriter.WriteStartDocument();

            var documentOptions = (options == null) ? DocumentSerializationOptions.Defaults : (DocumentSerializationOptions) options;
            int idIndex;
            if (documentOptions.SerializeIdFirst && indexes.TryGetValue("_id", out idIndex)) {
                elements[idIndex].WriteTo(bsonWriter);
            } else {
                idIndex = -1; // remember that when TryGetValue returns false it sets idIndex to 0
            }

            for (int i = 0; i < elements.Count; i++) {
                // if serializeIdFirst is false then idIndex will be -1 and no elements will be skipped
                if (i != idIndex) {
                    elements[i].WriteTo(bsonWriter);
                }
            }

            bsonWriter.WriteEndDocument();
        }

        // keep name short (Set instead of SetValue) to facilitate use in fluent interface
        public BsonDocument Set(
            int index,
            BsonValue value
        ) {
            this[index] = value;
            return this;
        }

        public BsonDocument Set(
            string name,
            BsonValue value
        ) {
            this[name] = value;
            return this;
        }

        public void SetDocumentId(
            object id
        ) {
            BsonElement idElement;
            if (TryGetElement("_id", out idElement)) {
                idElement.Value = BsonValue.Create(id);
            } else {
                idElement = new BsonElement("_id", BsonValue.Create(id));
                InsertAt(0, idElement);
            }
        }

        public BsonDocument SetElement(
            int index,
            BsonElement element
        ) {
            elements[index] = element;
            RebuildDictionary();
            return this;
        }

        public BsonDocument SetElement(
            BsonElement element
        ) {
            int index;
            if (indexes.TryGetValue(element.Name, out index)) {
                elements[index] = element;
            } else {
                Add(element);
            }
            return this;
        }

        public bool TryGetElement(
            string name,
            out BsonElement value
        ) {
            int index;
            if (indexes.TryGetValue(name, out index)) {
                value = elements[index];
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public bool TryGetValue(
            string name,
            out BsonValue value
        ) {
            int index;
            if (indexes.TryGetValue(name, out index)) {
                value = elements[index].Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public new void WriteTo(
            BsonWriter bsonWriter
        ) {
            Serialize(bsonWriter, typeof(BsonDocument), null);
        }

        public void WriteTo(
            BsonBuffer buffer
        ) {
            using (BsonWriter bsonWriter = BsonWriter.Create(buffer)) {
                WriteTo(bsonWriter);
            }
        }

        public void WriteTo(
            Stream stream
        ) {
            using (BsonWriter bsonWriter = BsonWriter.Create(stream)) {
                WriteTo(bsonWriter);
            }
        }

        public void WriteTo(
            string filename
        ) {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
                WriteTo(stream);
            }
        }
        #endregion

        #region private methods
        private void RebuildDictionary() {
            indexes.Clear();
            for (int index = 0; index < elements.Count; index++) {
                BsonElement element = elements[index];
                if (!indexes.ContainsKey(element.Name)) {
                    indexes.Add(element.Name, index);
                }
            }
        }
        #endregion

        #region explicit interface implementations
        BsonDocument IConvertibleToBsonDocument.ToBsonDocument() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return elements.GetEnumerator();
        }
        #endregion
    }
}
