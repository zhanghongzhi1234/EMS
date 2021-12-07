using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpcLibrary
{
    [Flags]
    public enum ResultFilter
    {
        /// <summary>
        /// Include the ItemName in the ItemIdentifier if bit is set.
        /// </summary>
        ItemName = 0x01,

        /// <summary>
        /// Include the ItemPath in the ItemIdentifier if bit is set.
        /// </summary>
        ItemPath = 0x02,

        /// <summary>
        /// Include the ClientHandle in the ItemIdentifier if bit is set.
        /// </summary>
        ClientHandle = 0x04,

        /// <summary>
        /// Include the Timestamp in the ItemValue if bit is set.
        /// </summary>
        ItemTime = 0x08,

        /// <summary>
        /// Include verbose, localized error text with result if bit is set. 
        /// </summary>
        ErrorText = 0x10,

        /// <summary>
        /// Include additional diagnostic information with result if bit is set.
        /// </summary>
        DiagnosticInfo = 0x20,

        /// <summary>
        /// Include the ItemName and Timestamp if bit is set.
        /// </summary>
        Minimal = ItemName | ItemTime,

        /// <summary>
        /// Include all information in the results if bit is set.
        /// </summary>
        All = 0x3F
    }
}
