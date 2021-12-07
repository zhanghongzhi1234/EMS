using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpcLibrary.Com;

namespace OpcLibrary
{
    /// <summary>
    /// A interface used to access result information associated with a single item/value.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// The error id for the result of an operation on an item.
        /// </summary>
        ResultID ResultID { get; set; }

        /// <summary>
        /// Vendor specific diagnostic information (not the localized error text).
        /// </summary>
        string DiagnosticInfo { get; set; }
    }

    public class ItemIdentifier : ICloneable
    {
        /// <summary>
		/// The primary identifier for an item within the server namespace.
		/// </summary>
		public string ItemName
		{
			get { return m_itemName;  }
			set { m_itemName = value; }
		}
		/// <summary>
		/// An secondary identifier for an item within the server namespace.
		/// </summary>
		public string ItemPath
		{
			get { return m_itemPath;  }
			set { m_itemPath = value; }
		}

		/// <summary>
		/// A unique item identifier assigned by the client.
		/// </summary>
		public object ClientHandle
		{
			get { return mm_clientHandle;  }
			set { mm_clientHandle = value; }
		}

		/// <summary>
		/// A unique item identifier assigned by the server.
		/// </summary>
		public object ServerHandle
		{
			get { return m_serverHandle;  }
			set { m_serverHandle = value; }
		}

		/// <summary>
		/// Create a string that can be used as index in a hash table for the item.
		/// </summary>
		public string Key
		{ 
			get 
			{
				return new StringBuilder(64)
					.Append((ItemName == null)?"null":ItemName)
					.Append("\r\n")
					.Append((ItemPath == null)?"null":ItemPath)
					.ToString();
			}
		}

		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemIdentifier() {}

		/// <summary>
		/// Initializes the object with the specified item name.
		/// </summary>
		public ItemIdentifier(string itemName)
		{
			ItemPath = null;
			ItemName = itemName;
		}

		/// <summary>
		/// Initializes the object with the specified item path and item name.
		/// </summary>
		public ItemIdentifier(string itemPath, string itemName)
		{
			ItemPath = itemPath;
			ItemName = itemName;
		}
		
		/// <summary>
		/// Initializes the object with the specified item identifier.
		/// </summary>
        public ItemIdentifier(ItemIdentifier itemID)
		{
			if (itemID != null)
			{
				ItemPath     = itemID.ItemPath;
				ItemName     = itemID.ItemName;
				ClientHandle = itemID.ClientHandle;
				ServerHandle = itemID.ServerHandle;
			}
		}

        #region ICloneable Members
        /// <summary>
        /// Creates a shallow copy of the object.
        /// </summary>
        public virtual object Clone() { return MemberwiseClone(); }
        #endregion

		#region Private Members
		private string m_itemName = null;
		private string m_itemPath = null;
		private object mm_clientHandle = null;
		private object m_serverHandle = null;
		#endregion
	}

    public class ItemValue : ItemIdentifier
    {
        /// <summary>
        /// The item value.
        /// </summary>
        public object Value
        {
            get { return m_value; }
            set { m_value = value; }
        }

        /// <summary>
        /// The quality of the item value.
        /// </summary>
        public Quality Quality
        {
            get { return m_quality; }
            set { m_quality = value; }
        }

        /// <summary>
        /// Whether the quality is specified.
        /// </summary>
        public bool QualitySpecified
        {
            get { return m_qualitySpecified; }
            set { m_qualitySpecified = value; }
        }

        /// <summary>
        /// The UTC timestamp for the item value.
        /// </summary>
        public DateTime Timestamp
        {
            get { return m_timestamp; }
            set { m_timestamp = value; }
        }

        /// <summary>
        /// Whether the timestamp is specified.
        /// </summary>
        public bool TimestampSpecified
        {
            get { return m_timestampSpecified; }
            set { m_timestampSpecified = value; }
        }

        #region Constructors
        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public ItemValue() { }

        /// <summary>
        /// Initializes the object with and OpcItem object.
        /// </summary>
        public ItemValue(ItemIdentifier item)
        {
            if (item != null)
            {
                ItemName = item.ItemName;
                ItemPath = item.ItemPath;
                ClientHandle = item.ClientHandle;
                ServerHandle = item.ServerHandle;
            }
        }

        /// <summary>
        /// Initializes the object with the specified item name.
        /// </summary>
        public ItemValue(string itemName)
            : base(itemName)
        {
        }

        /// <summary>
        /// Initializes object with the specified ItemValue object.
        /// </summary>
        public ItemValue(ItemValue item)
            : base(item)
        {
            if (item != null)
            {
                Value = OpcLibrary.Com.Convert.Clone(item.Value);
                Quality = item.Quality;
                QualitySpecified = item.QualitySpecified;
                Timestamp = item.Timestamp;
                TimestampSpecified = item.TimestampSpecified;
            }
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a deep copy of the object.
        /// </summary>
        public override object Clone()
        {
            ItemValue clone = (ItemValue)MemberwiseClone();
            clone.Value = OpcLibrary.Com.Convert.Clone(Value);
            return clone;
        }
        #endregion

        #region Private Members
        private object m_value = null;
        private Quality m_quality = Quality.Bad;
        private bool m_qualitySpecified = false;
        private DateTime m_timestamp = DateTime.MinValue;
        private bool m_timestampSpecified = false;
        #endregion
    }

    public class OpcItem : ItemIdentifier
    {
        /// <summary>
        /// The data type to use when returning the item value.
        /// </summary>
        public System.Type ReqType
        {
            get { return m_reqType; }
            set { m_reqType = value; }
        }

        /// <summary>
        /// The oldest (in milliseconds) acceptable cached value when reading an item.
        /// </summary>
        public int MaxAge
        {
            get { return m_maxAge; }
            set { m_maxAge = value; }
        }

        /// <summary>
        /// Whether the Max Age is specified.
        /// </summary>
        public bool MaxAgeSpecified
        {
            get { return m_maxAgeSpecified; }
            set { m_maxAgeSpecified = value; }
        }


        /// <summary>
        /// Whether the server should send data change updates. 
        /// </summary>
        public bool Active
        {
            get { return m_active; }
            set { m_active = value; }
        }

        /// <summary>
        /// Whether the Active state is specified.
        /// </summary>
        public bool ActiveSpecified
        {
            get { return m_activeSpecified; }
            set { m_activeSpecified = value; }
        }

        /// <summary>
        /// The minimum percentage change required to trigger a data update for an item. 
        /// </summary>
        public float Deadband
        {
            get { return m_deadband; }
            set { m_deadband = value; }
        }

        /// <summary>
        /// Whether the Deadband is specified.
        /// </summary>
        public bool DeadbandSpecified
        {
            get { return m_deadbandSpecified; }
            set { m_deadbandSpecified = value; }
        }

        /// <summary>
        /// How frequently the server should sample the item value.
        /// </summary>
        public int SamplingRate
        {
            get { return m_samplingRate; }
            set { m_samplingRate = value; }
        }

        /// <summary>
        /// Whether the Sampling Rate is specified.
        /// </summary>
        public bool SamplingRateSpecified
        {
            get { return m_samplingRateSpecified; }
            set { m_samplingRateSpecified = value; }
        }

        /// <summary>
        /// Whether the server should buffer multiple data changes between data updates.
        /// </summary>
        public bool EnableBuffering
        {
            get { return m_enableBuffering; }
            set { m_enableBuffering = value; }
        }

        /// <summary>
        /// Whether the Enable Buffering is specified.
        /// </summary>
        public bool EnableBufferingSpecified
        {
            get { return m_enableBufferingSpecified; }
            set { m_enableBufferingSpecified = value; }
        }

        #region Constructors
        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public OpcItem() { }

        /// <summary>
        /// Initializes object with the specified ItemIdentifier object.
        /// </summary>
        public OpcItem(ItemIdentifier item)
        {
            if (item != null)
            {
                ItemName = item.ItemName;
                ItemPath = item.ItemPath;
                ClientHandle = item.ClientHandle;
                ServerHandle = item.ServerHandle;
            }
        }

        /// <summary>
        /// Initializes object with the specified Item object.
        /// </summary>
        public OpcItem(OpcItem item)
            : base(item)
        {
            if (item != null)
            {
                ReqType = item.ReqType;
                MaxAge = item.MaxAge;
                MaxAgeSpecified = item.MaxAgeSpecified;
                Active = item.Active;
                ActiveSpecified = item.ActiveSpecified;
                Deadband = item.Deadband;
                DeadbandSpecified = item.DeadbandSpecified;
                SamplingRate = item.SamplingRate;
                SamplingRateSpecified = item.SamplingRateSpecified;
                EnableBuffering = item.EnableBuffering;
                EnableBufferingSpecified = item.EnableBufferingSpecified;
            }
        }
        #endregion

        #region Private Members
        private System.Type m_reqType = null;
        private int m_maxAge = 0;
        private bool m_maxAgeSpecified = false;
        private bool m_active = true;
        private bool m_activeSpecified = false;
        private float m_deadband = 0;
        private bool m_deadbandSpecified = false;
        private int m_samplingRate = 0;
        private bool m_samplingRateSpecified = false;
        private bool m_enableBuffering = false;
        private bool m_enableBufferingSpecified = false;
        #endregion
    }

    public class ItemResult : OpcItem, IResult
    {
        #region Constructors
        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public ItemResult() { }

        /// <summary>
        /// Initializes the object with an ItemIdentifier object.
        /// </summary>
        public ItemResult(ItemIdentifier item) : base(item) { }

        /// <summary>
        /// Initializes the object with an ItemIdentifier object and ResultID.
        /// </summary>
        public ItemResult(ItemIdentifier item, ResultID resultID)
            : base(item)
        {
            ResultID = ResultID;
        }

        /// <summary>
        /// Initializes the object with an Item object.
        /// </summary>
        public ItemResult(OpcItem item) : base(item) { }

        /// <summary>
        /// Initializes the object with an Item object and ResultID.
        /// </summary>
        public ItemResult(OpcItem item, ResultID resultID)
            : base(item)
        {
            ResultID = resultID;
        }

        /// <summary>
        /// Initializes object with the specified ItemResult object.
        /// </summary>
        public ItemResult(ItemResult item)
            : base(item)
        {
            if (item != null)
            {
                ResultID = item.ResultID;
                DiagnosticInfo = item.DiagnosticInfo;
            }
        }
        #endregion

        #region IResult Members
        /// <summary>
        /// The error id for the result of an operation on an property.
        /// </summary>
        public ResultID ResultID
        {
            get { return m_resultID; }
            set { m_resultID = value; }
        }

        /// <summary>
        /// Vendor specific diagnostic information (not the localized error text).
        /// </summary>
        public string DiagnosticInfo
        {
            get { return m_diagnosticInfo; }
            set { m_diagnosticInfo = value; }
        }
        #endregion

        #region Private Members
        private ResultID m_resultID = ResultID.S_OK;
        private string m_diagnosticInfo = null;
        #endregion
    }

    public class ItemValueResult : ItemValue, IResult
    {
        #region Constructors
        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public ItemValueResult() { }

        /// <summary>
        /// Initializes the object with an ItemIdentifier object.
        /// </summary>
        public ItemValueResult(ItemIdentifier item) : base(item) { }

        /// <summary>
        /// Initializes the object with an ItemValue object.
        /// </summary>
        public ItemValueResult(ItemValue item) : base(item) { }

        /// <summary>
        /// Initializes object with the specified ItemValueResult object.
        /// </summary>
        public ItemValueResult(ItemValueResult item)
            : base(item)
        {
            if (item != null)
            {
                ResultID = item.ResultID;
                DiagnosticInfo = item.DiagnosticInfo;
            }
        }

        /// <summary>
        /// Initializes the object with the specified item name and result code.
        /// </summary>
        public ItemValueResult(string itemName, ResultID resultID)
            : base(itemName)
        {
            ResultID = resultID;
        }

        /// <summary>
        /// Initializes the object with the specified item name, result code and diagnostic info.
        /// </summary>
        public ItemValueResult(string itemName, ResultID resultID, string diagnosticInfo)
            : base(itemName)
        {
            ResultID = resultID;
            DiagnosticInfo = diagnosticInfo;
        }

        /// <summary>
        /// Initialize object with the specified ItemIdentifier and result code.
        /// </summary>
        public ItemValueResult(ItemIdentifier item, ResultID resultID)
            : base(item)
        {
            ResultID = resultID;
        }

        /// <summary>
        /// Initializes the object with the specified ItemIdentifier, result code and diagnostic info.
        /// </summary>
        public ItemValueResult(ItemIdentifier item, ResultID resultID, string diagnosticInfo)
            : base(item)
        {
            ResultID = resultID;
            DiagnosticInfo = diagnosticInfo;
        }
        #endregion

        #region IResult Members
        /// <summary>
        /// The error id for the result of an operation on an property.
        /// </summary>
        public ResultID ResultID
        {
            get { return m_resultID; }
            set { m_resultID = value; }
        }

        /// <summary>
        /// Vendor specific diagnostic information (not the localized error text).
        /// </summary>
        public string DiagnosticInfo
        {
            get { return m_diagnosticInfo; }
            set { m_diagnosticInfo = value; }
        }
        #endregion

        #region Private Members
        private ResultID m_resultID = ResultID.S_OK;
        private string m_diagnosticInfo = null;
        #endregion
    }

    public class IdentifiedResult : ItemIdentifier, IResult
    {
        /// <summary>
        /// Initialize object with default values.
        /// </summary>
        public IdentifiedResult() { }

        /// <summary>
        /// Initialize object with the specified ItemIdentifier object.
        /// </summary>
        public IdentifiedResult(ItemIdentifier item)
            : base(item)
        {
        }

        /// <summary>
        /// Initialize object with the specified IdentifiedResult object.
        /// </summary>
        public IdentifiedResult(IdentifiedResult item)
            : base(item)
        {
            if (item != null)
            {
                ResultID = item.ResultID;
                DiagnosticInfo = item.DiagnosticInfo;
            }
        }

        /// <summary>
        /// Initializes the object with the specified item name and result code.
        /// </summary>
        public IdentifiedResult(string itemName, ResultID resultID)
            : base(itemName)
        {
            ResultID = resultID;
        }

        /// <summary>
        /// Initialize object with the specified item name, result code and diagnostic info.
        /// </summary>
        public IdentifiedResult(string itemName, ResultID resultID, string diagnosticInfo)
            : base(itemName)
        {
            ResultID = resultID;
            DiagnosticInfo = diagnosticInfo;
        }

        /// <summary>
        /// Initialize object with the specified ItemIdentifier and result code.
        /// </summary>
        public IdentifiedResult(ItemIdentifier item, ResultID resultID)
            : base(item)
        {
            ResultID = resultID;
        }

        /// <summary>
        /// Initialize object with the specified ItemIdentifier, result code and diagnostic info.
        /// </summary>
        public IdentifiedResult(ItemIdentifier item, ResultID resultID, string diagnosticInfo)
            : base(item)
        {
            ResultID = resultID;
            DiagnosticInfo = diagnosticInfo;
        }

        #region IResult Members
        /// <summary>
        /// The error id for the result of an operation on an item.
        /// </summary>
        public ResultID ResultID
        {
            get { return m_resultID; }
            set { m_resultID = value; }
        }

        /// <summary>
        /// Vendor specific diagnostic information (not the localized error text).
        /// </summary>
        public string DiagnosticInfo
        {
            get { return m_diagnosticInfo; }
            set { m_diagnosticInfo = value; }
        }
        #endregion

        #region Private Members
        private ResultID m_resultID = ResultID.S_OK;
        private string m_diagnosticInfo = null;
        #endregion
    }
}
