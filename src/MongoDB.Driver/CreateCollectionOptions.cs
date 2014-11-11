using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for creating a collection.
    /// </summary>
    public class CreateCollectionOptions
    {
        // fields
        private bool? _autoIndexId;
        private bool? _capped;
        private long? _maxDocuments;
        private long? _maxSize;
        private bool? _usePowerOf2Sizes;

        // properties
        /// <summary>
        /// Gets or sets the automatic index identifier.
        /// </summary>
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
            set { _autoIndexId = value; }
        }

        /// <summary>
        /// Gets or sets the capped.
        /// </summary>
        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

        /// <summary>
        /// Gets or sets the maximum documents.
        /// </summary>
        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size.
        /// </summary>
        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

        /// <summary>
        /// Gets or sets the use power of2 sizes.
        /// </summary>
        public bool? UsePowerOf2Sizes
        {
            get { return _usePowerOf2Sizes; }
            set { _usePowerOf2Sizes = value; }
        }
    }
}
