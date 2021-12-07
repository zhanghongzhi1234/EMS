using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpcLibrary
{
    /// <summary>
    /// Defines masks to be used when modifying the subscription or item state.
    /// </summary>
    [Flags]
    public enum StateMask
    {
        /// <summary>
        /// The name of the subscription.
        /// </summary>
        Name = 0x0001,

        /// <summary>
        /// The client assigned handle for the item or subscription.
        /// </summary>
        ClientHandle = 0x0002,

        /// <summary>
        /// The locale to use for results returned to the client from the subscription.
        /// </summary>
        Locale = 0x0004,

        /// <summary>
        /// Whether the item or subscription is active.
        /// </summary>
        Active = 0x0008,

        /// <summary>
        /// The maximum rate at which data update notifications are sent.
        /// </summary>
        UpdateRate = 0x0010,

        /// <summary>
        /// The longest period between data update notifications.
        /// </summary>
        KeepAlive = 0x0020,

        /// <summary>
        /// The requested data type for the item.
        /// </summary>
        ReqType = 0x0040,

        /// <summary>
        /// The deadband for the item or subscription.
        /// </summary>
        Deadband = 0x0080,

        /// <summary>
        /// The rate at which the server should check for changes to an item value.
        /// </summary>
        SamplingRate = 0x0100,

        /// <summary>
        /// Whether the server should buffer multiple changes to a single item.
        /// </summary>
        EnableBuffering = 0x0200,

        /// <summary>
        /// All fields are valid.
        /// </summary>
        All = 0xFFFF
    }

    /// <summary>
    /// Describes the state of a Opc group.
    /// </summary>
    [Serializable]
    public class GroupState : ICloneable
    {
        /// <summary>
        /// A unique name for the subscription controlled by the client.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// A unique identifier for the subscription assigned by the client.
        /// </summary>
        public object ClientHandle
        {
            get { return m_clientHandle; }
            set { m_clientHandle = value; }
        }

        /// <summary>
        /// A unique subscription identifier assigned by the server.
        /// </summary>
        public object ServerHandle
        {
            get { return m_serverHandle; }
            set { m_serverHandle = value; }
        }

        /// <summary>
        /// The locale used for any error messages or results returned to the client.
        /// </summary>
        public string Locale
        {
            get { return m_locale; }
            set { m_locale = value; }
        }

        /// <summary>
        /// Whether the subscription is scanning for updates to send to the client.
        /// </summary>
        public bool Active
        {
            get { return m_active; }
            set { m_active = value; }
        }

        /// <summary>
        /// The rate at which the server checks of updates to send to the client.
        /// </summary>
        public int UpdateRate
        {
            get { return m_updateRate; }
            set { m_updateRate = value; }
        }

        /// <summary>
        /// The minimum percentage change required to trigger a data update for an item.
        /// </summary>
        public float Deadband
        {
            get { return m_deadband; }
            set { m_deadband = value; }
        }

        #region Constructors
        /// <summary>
        /// Initializes object with default values.
        /// </summary>
        public GroupState()
        {
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a shallow copy of the object.
        /// </summary>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
        #endregion

        #region Private Members
        private string m_name = null;
        private object m_clientHandle = null;
        private object m_serverHandle = null;
        private string m_locale = null;
        private bool m_active = true;
        private int m_updateRate = 0;
        private float m_deadband = 0;
        #endregion
    }
}
