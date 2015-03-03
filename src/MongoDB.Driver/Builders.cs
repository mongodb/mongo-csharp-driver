using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// A static helper class containing various builders.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class Builders<TDocument>
    {
        private static FilterDefinitionBuilder<TDocument> __filter = new FilterDefinitionBuilder<TDocument>();
        private static IndexDefinitionBuilder<TDocument> __index = new IndexDefinitionBuilder<TDocument>();
        private static ProjectionDefinitionBuilder<TDocument> __projection = new ProjectionDefinitionBuilder<TDocument>();
        private static SortDefinitionBuilder<TDocument> __sort = new SortDefinitionBuilder<TDocument>();
        private static UpdateDefinitionBuilder<TDocument> __update = new UpdateDefinitionBuilder<TDocument>();

        /// <summary>
        /// Gets a <see cref="FilterDefinitionBuilder{TDocument}"/>.
        /// </summary>
        public static FilterDefinitionBuilder<TDocument> Filter
        {
            get { return __filter; }
        }

        /// <summary>
        /// Gets an <see cref="IndexDefinitionBuilder{TDocument}"/>.
        /// </summary>
        public static IndexDefinitionBuilder<TDocument> Index
        {
            get { return __index; }
        }

        /// <summary>
        /// Gets a <see cref="ProjectionDefinitionBuilder{TDocument}"/>.
        /// </summary>
        public static ProjectionDefinitionBuilder<TDocument> Projection
        {
            get { return __projection; }
        }

        /// <summary>
        /// Gets a <see cref="SortDefinitionBuilder{TDocument}"/>.
        /// </summary>
        public static SortDefinitionBuilder<TDocument> Sort
        {
            get { return __sort; }
        }

        /// <summary>
        /// Gets an <see cref="UpdateDefinitionBuilder{TDocument}"/>.
        /// </summary>
        public static UpdateDefinitionBuilder<TDocument> Update
        {
            get { return __update; }
        }
    }
}
