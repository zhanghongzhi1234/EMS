using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using OpcLibrary.Com;

namespace OpcLibrary
{
    /// <summary>
    /// A table of item identifiers indexed by internal handle.
    /// </summary>
    public class ItemTable
    {
        /// <summary>
        /// Looks up an item identifier for the specified internal handle.
        /// </summary>
        public ItemIdentifier this[object handle]
        {
            get
            {
                if (handle != null)
                {
                    return (ItemIdentifier)m_items[handle];
                }

                return null;
            }

            set
            {
                if (handle != null)
                {
                    if (value == null)
                    {
                        m_items.Remove(handle);
                        return;
                    }

                    m_items[handle] = value;
                }
            }
        }

        /// <summary>
        /// Returns a server handle that must be treated as invalid by the server,
        /// </summary>
        /// <returns></returns>
        private int GetInvalidHandle()
        {
            int invalidHandle = 0;

            foreach (ItemIdentifier item in m_items.Values)
            {
                if (item.ServerHandle != null && item.ServerHandle.GetType() == typeof(int))
                {
                    if (invalidHandle < (int)item.ServerHandle)
                    {
                        invalidHandle = (int)item.ServerHandle + 1;
                    }
                }
            }

            return invalidHandle;
        }
        
        /// <summary>
        /// Copies a set of items an substitutes the client and server handles for use by the server.
        /// </summary>
        public ItemIdentifier[] GetItemIDs(ItemIdentifier[] items)
        {
            List<object> clientHandles = new List<object>();
            foreach (ItemIdentifier item in items)
            {
                clientHandles.Add(item.ClientHandle);
            }

            return GetItemIDs(clientHandles.ToArray());
        }
        
        public ItemIdentifier[] GetItemIDs(object[] itemClientHandles)
        {
            // create an invalid server handle.
            int invalidHandle = GetInvalidHandle();

            // copy the items.
            ItemIdentifier[] itemIDs = new ItemIdentifier[itemClientHandles.Length];

                for (int ii = 0; ii < itemClientHandles.Length; ii++)
                {
                    // lookup server handle.
                    ItemIdentifier itemID = this[itemClientHandles[ii].GetHashCode()];

                    // copy the item id.
                    if (itemID != null)
                    {
                        itemIDs[ii] = (ItemIdentifier)itemID.Clone();
                    }

                    // create an invalid item id.
                    else
                    {
                        itemIDs[ii] = new ItemIdentifier();
                        itemIDs[ii].ServerHandle = invalidHandle;
                    }

                    // store the internal handle as the client handle.
                    //itemIDs[ii].ClientHandle = items[ii].ServerHandle;
               }

            // return the item copies.
            return itemIDs;
        }


        /// <summary>
        /// Creates a item result list from a set of items and sets the handles for use by the server.
        /// </summary>
        public ItemResult[] CreateItems(OpcItem[] items)
        {
            if (items == null) { return null; }

            ItemResult[] results = new ItemResult[items.Length];

            for (int ii = 0; ii < items.Length; ii++)
            {
                // initialize result with the item
                results[ii] = new ItemResult((OpcItem)items[ii]);

                // lookup the cached identifier.
                ItemIdentifier itemID = this[items[ii].ServerHandle];

                if (itemID != null)
                {
                    results[ii].ItemName = itemID.ItemName;
                    results[ii].ItemPath = itemID.ItemName;
                    results[ii].ServerHandle = itemID.ServerHandle;

                    // update the client handle.
                    itemID.ClientHandle = items[ii].ClientHandle;
                }

                // check if handle not found.
                if (results[ii].ServerHandle == null)
                {
                    results[ii].ResultID = ResultID.Da.E_INVALIDHANDLE;
                    results[ii].DiagnosticInfo = null;
                    continue;
                }

                // replace client handle with internal handle.
                results[ii].ClientHandle = items[ii].ServerHandle;
            }

            return results;
        }

        /// <summary>
        /// Updates a result list based on the request options and sets the handles for use by the client.
        /// </summary>
        public ItemIdentifier[] ApplyFilters(int filters, ItemIdentifier[] results)
        {
            if (results == null) { return null; }

            foreach (ItemIdentifier result in results)
            {
                ItemIdentifier itemID = this[result.ClientHandle];

                if (itemID != null)
                {
                    result.ItemName = ((filters & (int)ResultFilter.ItemName) != 0) ? itemID.ItemName : null;
                    result.ItemPath = ((filters & (int)ResultFilter.ItemPath) != 0) ? itemID.ItemPath : null;
                    result.ServerHandle = result.ClientHandle;
                    result.ClientHandle = ((filters & (int)ResultFilter.ClientHandle) != 0) ? itemID.ClientHandle : null;
                }

                if ((filters & (int)ResultFilter.ItemTime) == 0)
                {
                    if (result.GetType() == typeof(ItemValueResult))
                    {
                        ((ItemValueResult)result).Timestamp = DateTime.MinValue;
                        ((ItemValueResult)result).TimestampSpecified = false;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// The table of known item identifiers.
        /// </summary>
        private Hashtable m_items = new Hashtable();
    }
}
