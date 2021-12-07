using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpcLibrary
{
    /// <summary>
    /// The set of possible server states.
    /// </summary>
    public enum serverState
    {
        /// <summary>
        /// The server state is not known.
        /// </summary>
        unknown,

        /// <summary>
        /// The server is running normally.
        /// </summary>
        running,

        /// <summary>
        /// The server is not functioning due to a fatal error.
        /// </summary>
        failed,

        /// <summary>
        /// The server cannot load its configuration information.
        /// </summary>
        noConfig,

        /// <summary>
        /// The server has halted all communication with the underlying hardware.
        /// </summary>
        suspended,

        /// <summary>
        /// The server is disconnected from the underlying hardware.
        /// </summary>
        test,

        /// <summary>
        /// The server cannot communicate with the underlying hardware.
        /// </summary>
        commFault
    }

    public class ServerStatus : ICloneable
    {
        /// <summary>
        /// The vendor name and product name for the server.
        /// </summary>
        public string VendorInfo
        {
            get { return m_vendorInfo; }
            set { m_vendorInfo = value; }
        }

        /// <summary>
        /// A string that contains the server software version number.
        /// </summary>
        public string ProductVersion
        {
            get { return m_productVersion; }
            set { m_productVersion = value; }
        }

        /// <summary>
        /// The current state of the server.
        /// </summary>
        public serverState ServerState
        {
            get { return m_serverState; }
            set { m_serverState = value; }
        }

        /// <summary>
        /// A string that describes the current server state.
        /// </summary>
        public string StatusInfo
        {
            get { return m_statusInfo; }
            set { m_statusInfo = value; }
        }

        /// <summary>
        /// The UTC time when the server started.
        /// </summary>
        public DateTime StartTime
        {
            get { return m_startTime; }
            set { m_startTime = value; }
        }

        /// <summary>
        /// Th current UTC time at the server.
        /// </summary>
        public DateTime CurrentTime
        {
            get { return m_currentTime; }
            set { m_currentTime = value; }
        }

        /// <summary>
        /// The last time the server sent an data update to the client.
        /// </summary>
        public DateTime LastUpdateTime
        {
            get { return m_lastUpdateTime; }
            set { m_lastUpdateTime = value; }
        }

        #region ICloneable Members
        /// <summary>
        /// Creates a deepcopy of the object.
        /// </summary>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
        #endregion

        #region Private Members
        private string m_vendorInfo = null;
        private string m_productVersion = null;
        private serverState m_serverState = serverState.unknown;
        private string m_statusInfo = null;
        private DateTime m_startTime = DateTime.MinValue;
        private DateTime m_currentTime = DateTime.MinValue;
        private DateTime m_lastUpdateTime = DateTime.MinValue;
        #endregion
    }
}
