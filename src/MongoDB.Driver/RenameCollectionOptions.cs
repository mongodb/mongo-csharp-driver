using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for renaming a collection.
    /// </summary>
    public class RenameCollectionOptions
    {
        // fields
        private bool? _dropTarget;

        // properties
        /// <summary>
        /// Gets or sets the drop target.
        /// </summary>
        public bool? DropTarget
        {
            get { return _dropTarget; }
            set { _dropTarget = value; }
        }
    }
}
