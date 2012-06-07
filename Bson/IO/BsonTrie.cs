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
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Trie for rapidly mapping binary input to complex values
    /// </summary>
    public class BsonTrie
    {
        // private static fields
        private static readonly UTF8Encoding __utf8Encoding = new UTF8Encoding(false, true); // throw on invalid bytes

        // private fields
        private readonly byte[] _byteToChildPosition; // 256 element array (one index for each possible byte value)
        private int _childPositionCount;
        private readonly BsonTrieNode _root;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonTrie class.
        /// </summary>
        public BsonTrie()
        {
            this._byteToChildPosition = new byte[256];

            for (var i = 0; i < 256; ++i)
            {
                // initialize with the highest unassigned child position
                this._byteToChildPosition[i] = 255;
            }

            this._root = new BsonTrieNode(0);
        }

        // public properties
        /// <summary>
        /// Gets the root node.
        /// </summary>
        public BsonTrieNode Root
        {
            get
            {
                return this._root;
            }
        }

        /// <summary>
        /// Adds the specified string converted to a UTF8 byte sequence and value to the trie.
        /// </summary>
        /// <param name="str">The key sequence of the value to add.</param>
        /// <param name="value">The value to add. The value can be null for reference types.</param>
        public void Add(string str, object value)
        {
            var bytes = __utf8Encoding.GetBytes(str);

            int endIndex;

            var node = this.FindNode(this._root, bytes, 0, out endIndex);

            if (endIndex < bytes.Length)
            {
                do
                {
                    node = this.AddNode(
                        node,
                        bytes[endIndex]);

                    ++endIndex;
                }
                while (endIndex < bytes.Length);
            }
            else
            {
                if (node.HasValue)
                {
                    throw new InvalidOperationException();
                }
            }

            node.Value = value;
        }

        /// <summary>
        /// Sorts child array positions by byte frequency in order to map more
        /// common bytes to lower child positions. Because the length of a
        /// child array is equal to the largest mapped child position,
        /// this reduces the size of most child arrays.
        /// </summary>
        public void Compact()
        {
            var frequencyCount = new Dictionary<byte, int>();

            var stack = new Stack<BsonTrieNode>();

            // count all byte to child position map references
            var node = this._root;

            for (; ; )
            {
                var list = node.ChildList;

                if (list != null)
                {
                    for (var i = 0; i < list.Length; ++i)
                    {
                        var child = list[i];

                        if (child != null)
                        {
                            // only track bytes where the byte to child position map is consulted
                            Increment(frequencyCount, child.Key);

                            stack.Push(child);
                        }
                    }
                }
                else
                {
                    var child = node.Child;

                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }

                if (stack.Count == 0)
                {
                    break;
                }

                node = stack.Pop();
            }

            // sort the byte to child position map in descending order by
            // frequency
            byte childIndex = 0;

            foreach (var keyValuePair in frequencyCount
                .OrderByDescending(keyValuePair => keyValuePair.Value))
            {
                this._byteToChildPosition[keyValuePair.Key] = childIndex;

                ++childIndex;
            }

            // update all child lists
            node = this._root;

            for (; ; )
            {
                var newList = new List<BsonTrieNode>();

                var list = node.ChildList;

                if (list != null)
                {
                    for (var i = 0; i < list.Length; ++i)
                    {
                        var child = list[i];

                        if (child != null)
                        {
                            childIndex = this._byteToChildPosition[child.Key];

                            // size the list to the largest child index
                            while (childIndex > newList.Count)
                            {
                                newList.Add(null);
                            }

                            if (childIndex < newList.Count)
                            {
                                newList[childIndex] = child;
                            }
                            else
                            {
                                newList.Add(child);
                            }

                            stack.Push(child);
                        }
                    }

                    node.ChildList = newList.ToArray();
                }
                else
                {
                    var child = node.Child;

                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }

                if (stack.Count == 0)
                {
                    break;
                }

                node = stack.Pop();
            }
        }

        /// <summary>
        /// Gets the child node for a given byte value.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="key">The byte value key of the child node to get.</param>
        /// <returns>The child node if the parent node contains a child node with the specified key; otherwise, null.</returns>
        public BsonTrieNode GetNext(BsonTrieNode parent, byte key)
        {
            var next = parent.Child;

            if (next != null)
            {
                if (key == next.Key)
                {
                    return next;
                }
            }
            else
            {
                var list = parent.ChildList;

                if (list != null)
                {
                    var index = this._byteToChildPosition[key];

                    if (index < list.Length)
                    {
                        return list[index];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the value associated with a string when converted to a UTF8 byte sequence.
        /// </summary>
        /// <param name="str">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the BsonTrie contains a value with the specified key sequence; otherwise, false.</returns>
        public bool TryGetValue(string str, out object value)
        {
            int endIndex;

            // first attempt to traverse directly because ascii characters can be cast to bytes
            var node = this.FindNodeAscii(this._root, str, out endIndex);

            if (endIndex == str.Length)
            {
                if (!node.HasValue)
                {
                    value = null;

                    return false;
                }

                value = node.Value;

                return true;
            }

            var c = str[endIndex];

            if (c < 128)
            {
                value = null;

                return false;
            }

            var bytes = __utf8Encoding.GetBytes(str);

            node = this.FindNode(node, bytes, endIndex, out endIndex);

            if (endIndex < bytes.Length ||
                !node.HasValue)
            {
                value = null;

                return false;
            }

            value = node.Value;

            return true;
        }

        /// <summary>
        /// Increments a value in a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the value to increment.</param>
        private static void Increment(
            Dictionary<byte, int> dictionary,
            byte key)
        {
            int value;

            if (!dictionary.TryGetValue(key, out value))
            {
                dictionary.Add(key, 1);
            }
            else
            {
                dictionary[key] = value + 1;
            }
        }

        /// <summary>
        /// Adds a child node for a given byte value.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="key">The child node byte value key.</param>
        /// <returns>The child node</returns>
        private BsonTrieNode AddNode(BsonTrieNode parent, byte key)
        {
            var child = new BsonTrieNode(key);

            var list = parent.ChildList;

            if (list != null)
            {
                var index = this.MapByteToChildPosition(key);

                if (index >= list.Length)
                {
                    list = new BsonTrieNode[index + 1];

                    Array.Copy(
                        parent.ChildList,
                        0,
                        list,
                        0,
                        parent.ChildList.Length);

                    parent.ChildList = list;
                }

                list[index] = child;
            }
            else
            {
                var temp = parent.Child;

                if (temp != null)
                {
                    var index1 = this.MapByteToChildPosition(temp.Key);

                    var index2 = this.MapByteToChildPosition(key);

                    var maxIndex = index1 >= index2 ? index1 : index2;

                    list = new BsonTrieNode[maxIndex + 1];

                    list[index1] = temp;

                    list[index2] = child;

                    parent.Child = null;

                    parent.ChildList = list;
                }
                else
                {
                    parent.Child = child;
                }
            }

            return child;
        }

        /// <summary>
        /// Finds the last matching child node for a given key sequence.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="key">The child node key sequence to traverse.</param>
        /// <param name="startIndex">The zero-based starting index within the key sequence to start traversing from.</param>
        /// <param name="endIndex">The zero-based ending index within the key sequence where traversing stopped.</param>
        /// <returns>The last matching child node</returns>
        private BsonTrieNode FindNode(
            BsonTrieNode parent,
            byte[] key,
            int startIndex,
            out int endIndex)
        {
            var index = startIndex;

            var node = parent;

            for (; index < key.Length; ++index)
            {
                var temp = this.GetNext(node, key[index]);

                if (temp == null)
                {
                    break;
                }

                node = temp;
            }

            endIndex = index;

            return node;
        }

        /// <summary>
        /// Finds the last matching child node for a given key sequence for ASCII input characters.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="str">The child node key sequence to traverse.</param>
        /// <param name="endIndex">The zero-based ending index within the key sequence where traversing stopped.</param>
        /// <returns>The last matching child node</returns>
        private BsonTrieNode FindNodeAscii(
            BsonTrieNode parent,
            string str,
            out int endIndex)
        {
            var index = 0;

            var node = parent;

            for (; index < str.Length; ++index)
            {
                var c = str[index];

                if (c >= 128)
                {
                    break;
                }

                var temp = this.GetNext(node, (byte)c);

                if (temp == null)
                {
                    break;
                }

                node = temp;
            }

            endIndex = index;

            return node;
        }

        /// <summary>
        /// Maps a byte to a child position and allocates a new child position if necessary.
        /// </summary>
        /// <param name="byteValue">The byte value.</param>
        /// <returns>The child index.</returns>
        private byte MapByteToChildPosition(byte byteValue)
        {
            var childIndex = this._byteToChildPosition[byteValue];

            if (childIndex >= this._childPositionCount)
            {
                childIndex = (byte)this._childPositionCount;

                this._byteToChildPosition[byteValue] = childIndex;

                ++this._childPositionCount;
            }

            return childIndex;
        }
    }

    /// <summary>
    /// Trie for rapidly mapping binary input to complex values
    /// </summary>
    public class BsonTrie<TValue> : BsonTrie
    {
        /// <summary>
        /// Adds the specified string converted to a UTF8 byte sequence and value to the trie.
        /// </summary>
        /// <param name="str">The key sequence of the value to add.</param>
        /// <param name="value">The value to add. The value can be null for reference types.</param>
        public void Add(string str, TValue value)
        {
            base.Add(str, value);
        }

        /// <summary>
        /// Gets the value associated with a string when converted to a UTF8 byte sequence.
        /// </summary>
        /// <param name="str">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the BsonTrie&lt;TValue&gt; contains a value with the specified key sequence; otherwise, false.</returns>
        public bool TryGetValue(string str, out TValue value)
        {
            object obj;

            if (!base.TryGetValue(str, out obj))
            {
                value = default(TValue);

                return false;
            }

            value = (TValue)obj;

            return true;
        }
    }

    /// <summary>
    /// Trie node implementation
    /// </summary>
    public sealed class BsonTrieNode
    {
        // internal fields
        internal readonly byte Key; // Direct access readonly speed optimization

        // private fields
        // Value associated with this state
        private object _value;

        // Whether this state has a value
        private bool _hasValue;

        // Special case when there is only 1 child (i.e. list.Length
        // would == 1) because managed array accesses in .Net incur a
        // performance penalty due to bounds checking (as well as an
        // additional layer of indirection).
        private BsonTrieNode _child;

        // List of child nodes
        private BsonTrieNode[] _childList;

        internal BsonTrieNode()
        {
        }

        internal BsonTrieNode(byte key)
        {
            this.Key = key;
        }

        internal BsonTrieNode Child
        {
            get
            {
                return this._child;
            }
            set
            {
                this._child = value;
            }
        }

        internal BsonTrieNode[] ChildList
        {
            get
            {
                return this._childList;
            }
            set
            {
                this._childList = value;
            }
        }

        /// <summary>
        /// Gets whether this node has a value
        /// </summary>
        public bool HasValue
        {
            get
            {
                return this._hasValue;
            }
        }

        /// <summary>
        /// Gets the value for this node
        /// </summary>
        public object Value
        {
            get
            {
                if (!this._hasValue)
                {
                    throw new InvalidOperationException();
                }

                return this._value;
            }
            internal set
            {
                this._value = value;

                this._hasValue = true;
            }
        }
    }
}
