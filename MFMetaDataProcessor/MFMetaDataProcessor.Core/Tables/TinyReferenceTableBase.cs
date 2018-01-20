﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Base class for all metadata tables with lookup functionality. Stores items in Mono.Cecil
    /// format in dictionary and allows to get item ID by item value (using custom comparer).
    /// </summary>
    /// <typeparam name="T">Type of stored metadata item (in Mono.Cecil format).</typeparam>
    public abstract class TinyReferenceTableBase<T> : ITinyTable
    {
        /// <summary>
        /// Lookup table for finding item ID by item value.
        /// </summary>
        private readonly Dictionary<T, UInt16> _idsByItemsDictionary;

        /// <summary>
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </summary>
        protected readonly TinyTablesContext _context;

        /// <summary>
        /// Creates new instance of <see cref="TinyReferenceTableBase{T}"/> object.
        /// </summary>
        /// <param name="tinyTableItems">List of items for initial loading.</param>
        /// <param name="comparer">Custom comparer for items (type-specific).</param>
        /// <param name="context">
        /// Assembly tables context - contains all tables used for building target assembly.
        /// </param>
        protected TinyReferenceTableBase(
            IEnumerable<T> tinyTableItems,
            IEqualityComparer<T> comparer,
            TinyTablesContext context)
        {
            _idsByItemsDictionary = tinyTableItems
                .Select((reference, index) => new { reference, index })
                .ToDictionary(item => item.reference, item => (UInt16)item.index,
                    comparer);

            _context = context;
        }

        /// <inheritdoc/>
        public void Write(
            TinyBinaryWriter writer)
        {
            foreach (var item in _idsByItemsDictionary
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
            {
                WriteSingleItem(writer, item);
            }
        }

        public void ForEachItems(Action<UInt32, T> action)
        {
            foreach (var item in _idsByItemsDictionary
                .OrderBy(item => item.Value))
            {
                action(item.Value, item.Key);
            }
        }

        /// <summary>
        /// Helper method for allocating strings from table before table will be written.
        /// </summary>
        public void AllocateStrings()
        {
            foreach (var item in _idsByItemsDictionary
                .OrderBy(item => item.Value)
                .Select(item => item.Key))
            {
                AllocateSingleItemStrings(item);
            }
        }

        /// <summary>
        /// Writes string reference ID related to passed string value into output stream.
        /// </summary>
        /// <param name="writer">Target binary writer for writing reference ID.</param>
        /// <param name="value">String value for obtaining reference and writing.</param>
        protected void WriteStringReference(
            TinyBinaryWriter writer,
            String value)
        {
            writer.WriteUInt16(GetOrCreateStringId(value));
        }

        /// <summary>
        /// Gets existing or creates new string reference ID for provided string value.
        /// </summary>
        /// <param name="value">String value for lookup in string literals table.</param>
        /// <returns>String reference ID which can be used for filling metadata and byte code.</returns>
        protected UInt16 GetOrCreateStringId(
            String value)
        {
            return _context.StringTable.GetOrCreateStringId(value);
        }

        /// <summary>
        /// Helper method for lookup in internal dictionary. Wraps
        /// <see cref="IDictionary{TKey,TValue}.TryGetValue"/> method.
        /// </summary>
        /// <param name="key">Key value for lookup.</param>
        /// <param name="id">Item reference identifier.</param>
        /// <returns>Returns <c>true</c> if item found, overwise returns <c>false</c>.</returns>
        protected Boolean TryGetIdByValue(
            T key,
            out UInt16 id)
        {
            return _idsByItemsDictionary.TryGetValue(key, out id);
        }

        /// <summary>
        /// Provides concrete implementation for string allocation method in this table.
        /// </summary>
        /// <param name="item">Table item for allocating strings.</param>
        protected virtual void AllocateSingleItemStrings(T item)
        {
        }

        /// <summary>
        /// Inherited class should provides concrete implementation for writing single table item here.
        /// </summary>
        /// <param name="writer">Target binary writer for writing item data.</param>
        /// <param name="item">Single table item for writing into ouptut stream.</param>
        protected abstract void WriteSingleItem(
            TinyBinaryWriter writer,
            T item);
    }
}
