using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Linq
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly TKey _key;
        private readonly IEnumerable<TElement> _group;

        /// <summary>
        /// Initializes a new instance of the <see cref="Grouping&lt;TKey, TElement&gt;"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="group">The group.</param>
        public Grouping(TKey key, IEnumerable<TElement> group)
        {
            _key = key;
            _group = group;
        }

        /// <summary>
        /// Gets the key of the <see cref="T:System.Linq.IGrouping`2"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The key of the <see cref="T:System.Linq.IGrouping`2"/>.
        /// </returns>
        public TKey Key
        {
            get { return _key; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TElement> GetEnumerator()
        {
            return _group.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _group.GetEnumerator();
        }
    }
}
