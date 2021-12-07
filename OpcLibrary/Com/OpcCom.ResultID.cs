using System;
using System.Xml;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpcLibrary.Com
{
	/// <summary>
	/// Contains a unique identifier for a result code.
	/// </summary>
	[Serializable]
	public struct ResultID : ISerializable
	{
		#region Serialization Functions
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string NAME      = "NA";
			internal const string NAMESPACE = "NS";
			internal const string CODE      = "CO";
		}

		//MP During deserialization, SerializationInfo is passed to the class using the constructor provided for this purpose. Any visibility 
		// constraints placed on the constructor are ignored when the object is deserialized; so you can mark the class as public, 
		// protected, internal, or private. However, it is best practice to make the constructor protected unless the class is sealed, in which case 
		// the constructor should be marked private. The constructor should also perform thorough input validation. To avoid misuse by malicious code, 
		// the constructor should enforce the same security checks and permissions required to obtain an instance of the class using any other 
		// constructor. 
		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		private ResultID(SerializationInfo info, StreamingContext context)
		{
			string name = (string)info.GetValue(Names.NAME, typeof(string));
			string ns   = (string)info.GetValue(Names.NAMESPACE, typeof(string));
			m_name = new XmlQualifiedName(name, ns);
			m_code = (int)info.GetValue(Names.CODE, typeof(int));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (m_name != null)
			{
				info.AddValue(Names.NAME, m_name.Name);
				info.AddValue(Names.NAMESPACE, m_name.Namespace);
			}
			info.AddValue(Names.CODE, m_code);
		}
		#endregion
    
		/// <summary>
		/// Used for result codes identified by a qualified name.
		/// </summary>
		public XmlQualifiedName Name 
		{
			get{ return m_name; }
		}

		/// <summary>
		/// Used for result codes identified by a integer.
		/// </summary>
		public int Code
		{
			get{ return m_code; }
		}
		
		/// <summary>
		/// Returns true if the objects are equal.
		/// </summary>
		public static bool operator==(ResultID a, ResultID b) 
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Returns true if the objects are not equal.
		/// </summary>
		public static bool operator!=(ResultID a, ResultID b) 
		{
			return !a.Equals(b);
		}

		/// <summary>
		/// Checks for the 'S_' prefix that indicates a success condition.
		/// </summary>
		public bool Succeeded()
		{
			if (Code != -1)   return (Code >= 0);
			if (Name != null) return Name.Name.StartsWith("S_");
			return false;
		}

		/// <summary>
		/// Checks for the 'E_' prefix that indicates an error condition.
		/// </summary>
		public bool Failed()
		{
			if (Code != -1)   return (Code < 0);
			if (Name != null) return Name.Name.StartsWith("E_");
			return false;
		}

		#region Constructors
		/// <summary>
		/// Initializes a result code identified by a qualified name.
		/// </summary>
		public ResultID(XmlQualifiedName name) 
		{ 
			m_name = name; 
			m_code = -1; 
		}

		/// <summary>
		/// Initializes a result code identified by an integer.
		/// </summary>
		public ResultID(long code) 
		{ 
			m_name = null; 
			
			if (code > Int32.MaxValue)
			{
				code = -(((long)UInt32.MaxValue)+1-code);
			}

			m_code = (int)code;
		}

		/// <summary>
		/// Initializes a result code identified by a qualified name.
		/// </summary>
		public ResultID(string name, string ns) 
		{ 
			m_name = new XmlQualifiedName(name, ns); 
			m_code = -1;
		}

		/// <summary>
		/// Initializes a result code with a general result code and a specific result code.
		/// </summary>
		public ResultID(ResultID resultID, long code) 
		{ 
			m_name = resultID.Name; 

			if (code > Int32.MaxValue)
			{
				code = -(((long)UInt32.MaxValue)+1-code);
			}

			m_code = (int)code;
		}
		#endregion

		#region Object Method Overrides
		/// <summary>
		/// Returns true if the target object is equal to the object.
		/// </summary>
		public override bool Equals(object target)
		{
			if (target != null && target.GetType() == typeof(ResultID))
			{
				ResultID resultID = (ResultID)target;

				// compare by integer if both specify valid integers.
				if (resultID.Code != -1 && Code != -1)
				{
					return (resultID.Code == Code) && (resultID.Name == Name); 
				}

				// compare by name if both specify valid names.
				if (resultID.Name != null && Name != null)
				{
					return (resultID.Name == Name);
				}
			}

			return false;
		}

		/// <summary>
		/// Formats the result identifier as a string.
		/// </summary>
		public override string ToString()
		{
			if (Name != null) return Name.Name;
			return String.Format("0x{0,0:X}", Code);
		}

		/// <summary>
		/// Returns a useful hash code for the object.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region Private Members
		private XmlQualifiedName m_name;
		private int m_code;
		#endregion

		/// <remarks/>
		public static readonly ResultID S_OK                       = new ResultID("S_OK",                       "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID S_FALSE                    = new ResultID("S_FALSE",                    "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_FAIL                     = new ResultID("E_FAIL",                     "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_INVALIDARG               = new ResultID("E_INVALIDARG",               "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_TIMEDOUT                 = new ResultID("E_TIMEDOUT",                 "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_OUTOFMEMORY              = new ResultID("E_OUTOFMEMORY",              "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_NETWORK_ERROR            = new ResultID("E_NETWORK_ERROR",            "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_ACCESS_DENIED            = new ResultID("E_ACCESS_DENIED",            "http://opcfoundation.org/DataAccess/");
		/// <remarks/>
		public static readonly ResultID E_NOTSUPPORTED             = new ResultID("E_NOTSUPPORTED",             "http://opcfoundation.org/DataAccess/");

		/// <summary>
		/// Results codes for Data Access.
		/// </summary>
		public class Da
		{
			/// <remarks/>
			public static readonly ResultID S_DATAQUEUEOVERFLOW        = new ResultID("S_DATAQUEUEOVERFLOW",        "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID S_UNSUPPORTEDRATE          = new ResultID("S_UNSUPPORTEDRATE",          "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID S_CLAMP                    = new ResultID("S_CLAMP",                    "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_INVALIDHANDLE            = new ResultID("E_INVALIDHANDLE",            "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_ITEM_NAME        = new ResultID("E_UNKNOWN_ITEM_NAME",        "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_INVALID_ITEM_NAME        = new ResultID("E_INVALID_ITEM_NAME",        "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_ITEM_PATH        = new ResultID("E_UNKNOWN_ITEM_PATH",        "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_INVALID_ITEM_PATH        = new ResultID("E_INVALID_ITEM_PATH",        "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_INVALID_PID              = new ResultID("E_INVALID_PID",              "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_READONLY                 = new ResultID("E_READONLY",                 "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_WRITEONLY                = new ResultID("E_WRITEONLY",                "http://opcfoundation.org/DataAccess/");
			/// <remarks/> 
			public static readonly ResultID E_BADTYPE                  = new ResultID("E_BADTYPE",                  "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_RANGE                    = new ResultID("E_RANGE",                    "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_INVALID_FILTER           = new ResultID("E_INVALID_FILTER",           "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_INVALIDCONTINUATIONPOINT = new ResultID("E_INVALIDCONTINUATIONPOINT", "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_NO_WRITEQT               = new ResultID("E_NO_WRITEQT",               "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_NO_ITEM_DEADBAND         = new ResultID("E_NO_ITEM_DEADBAND",         "http://opcfoundation.org/DataAccess/");
			/// <remarks/> 
			public static readonly ResultID E_NO_ITEM_SAMPLING         = new ResultID("E_NO_ITEM_SAMPLING",         "http://opcfoundation.org/DataAccess/");
			/// <remarks/>
			public static readonly ResultID E_NO_ITEM_BUFFERING        = new ResultID("E_NO_ITEM_BUFFERING",        "http://opcfoundation.org/DataAccess/");
		}

	}

	/// <summary>
	/// Used to raise an exception with associated with a specified result code.
	/// </summary>
	[Serializable]
	public class ResultIDException : ApplicationException
	{	/// <remarks/>
		public ResultID Result {get{ return m_result; }}
	
		/// <remarks/>
		public ResultIDException(ResultID result) : base(result.ToString()) { m_result = result; } 
		/// <remarks/>
		public ResultIDException(ResultID result, string message) : base(result.ToString() + "\r\n" + message) { m_result = result; } 
		/// <remarks/>
		public ResultIDException(ResultID result, string message, Exception e) : base(result.ToString() + "\r\n" + message, e) { m_result = result; } 
		/// <remarks/>
		protected ResultIDException(SerializationInfo info, StreamingContext context) : base(info, context) {}
		
		/// <remarks/>
		private ResultID m_result = ResultID.E_FAIL;
	}

    /// <summary>
    /// Defines all well known COM DA HRESULT codes.
    /// </summary>
    public struct ResultIDs
    {
        /// <remarks/>
        public const int S_OK = +0x00000000; // 0x00000000
        /// <remarks/>
        public const int S_FALSE = +0x00000001; // 0x00000001
        /// <remarks/>
        public const int E_NOTIMPL = -0x7FFFBFFF; // 0x80004001
        /// <remarks/>
        public const int E_OUTOFMEMORY = -0x7FF8FFF2; // 0x8007000E
        /// <remarks/>
        public const int E_INVALIDARG = -0x7FF8FFA9; // 0x80070057
        /// <remarks/>
        public const int E_NOINTERFACE = -0x7FFFBFFE; // 0x80004002
        /// <remarks/>
        public const int E_POINTER = -0x7FFFBFFD; // 0x80004003
        /// <remarks/>
        public const int E_FAIL = -0x7FFFBFFB; // 0x80004005
        /// <remarks/>
        public const int CONNECT_E_NOCONNECTION = -0x7FFBFE00; // 0x80040200
        /// <remarks/>
        public const int CONNECT_E_ADVISELIMIT = -0x7FFBFDFF; // 0x80040201
        /// <remarks/>
        public const int DISP_E_TYPEMISMATCH = -0x7FFDFFFB; // 0x80020005
        /// <remarks/>
        public const int DISP_E_OVERFLOW = -0x7FFDFFF6; // 0x8002000A
        /// <remarks/>
        public const int E_INVALIDHANDLE = -0x3FFBFFFF; // 0xC0040001
        /// <remarks/>
        public const int E_BADTYPE = -0x3FFBFFFC; // 0xC0040004
        /// <remarks/>
        public const int E_PUBLIC = -0x3FFBFFFB; // 0xC0040005
        /// <remarks/>
        public const int E_BADRIGHTS = -0x3FFBFFFA; // 0xC0040006
        /// <remarks/>
        public const int E_UNKNOWNITEMID = -0x3FFBFFF9; // 0xC0040007
        /// <remarks/>
        public const int E_INVALIDITEMID = -0x3FFBFFF8; // 0xC0040008
        /// <remarks/>
        public const int E_INVALIDFILTER = -0x3FFBFFF7; // 0xC0040009
        /// <remarks/>
        public const int E_UNKNOWNPATH = -0x3FFBFFF6; // 0xC004000A
        /// <remarks/>
        public const int E_RANGE = -0x3FFBFFF5; // 0xC004000B
        /// <remarks/>
        public const int E_DUPLICATENAME = -0x3FFBFFF4; // 0xC004000C
        /// <remarks/>
        public const int S_UNSUPPORTEDRATE = +0x0004000D; // 0x0004000D
        /// <remarks/>
        public const int S_CLAMP = +0x0004000E; // 0x0004000E
        /// <remarks/>
        public const int S_INUSE = +0x0004000F; // 0x0004000F
        /// <remarks/>
        public const int E_INVALIDCONFIGFILE = -0x3FFBFFF0; // 0xC0040010
        /// <remarks/>
        public const int E_NOTFOUND = -0x3FFBFFEF; // 0xC0040011
        /// <remarks/>
        public const int E_INVALID_PID = -0x3FFBFDFD; // 0xC0040203
        /// <remarks/>
        public const int E_DEADBANDNOTSET = -0x3FFBFC00; // 0xC0040400
        /// <remarks/>
        public const int E_DEADBANDNOTSUPPORTED = -0x3FFBFBFF; // 0xC0040401
        /// <remarks/>
        public const int E_NOBUFFERING = -0x3FFBFBFE; // 0xC0040402
        /// <remarks/>
        public const int E_INVALIDCONTINUATIONPOINT = -0x3FFBFBFD; // 0xC0040403
        /// <remarks/>
        public const int S_DATAQUEUEOVERFLOW = +0x00040404; // 0x00040404	
        /// <remarks/>
        public const int E_RATENOTSET = -0x3FFBFBFB; // 0xC0040405
        /// <remarks/>
        public const int E_NOTSUPPORTED = -0x3FFBFBFA; // 0xC0040406
    }
}
