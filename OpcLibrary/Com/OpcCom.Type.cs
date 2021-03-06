using System;
using System.Xml;
using System.Collections;
using System.Reflection;

namespace OpcLibrary.Com
{
	/// <summary>
	/// Defines constants for standard data types.
	/// </summary>
	public class Type
	{
		/// <remarks/>
		public static System.Type SBYTE          = typeof(sbyte);
		/// <remarks/>
		public static System.Type BYTE           = typeof(byte);
		/// <remarks/>
		public static System.Type SHORT          = typeof(short);
		/// <remarks/>
		public static System.Type USHORT         = typeof(ushort);
		/// <remarks/>
		public static System.Type INT            = typeof(int);
		/// <remarks/>
		public static System.Type UINT           = typeof(uint);
		/// <remarks/>
		public static System.Type LONG           = typeof(long);
		/// <remarks/>
		public static System.Type ULONG          = typeof(ulong);
		/// <remarks/>
		public static System.Type FLOAT          = typeof(float);
		/// <remarks/>
		public static System.Type DOUBLE         = typeof(double);
		/// <remarks/>
		public static System.Type DECIMAL        = typeof(decimal);
		/// <remarks/>
		public static System.Type BOOLEAN        = typeof(bool);
		/// <remarks/>
		public static System.Type DATETIME       = typeof(DateTime);
		/// <remarks/>
		public static System.Type DURATION       = typeof(TimeSpan);
		/// <remarks/>
		public static System.Type STRING         = typeof(string);
		/// <remarks/>
		public static System.Type ANY_TYPE       = typeof(object);
		/// <remarks/>
		public static System.Type BINARY         = typeof(byte[]);
		/// <remarks/>
		public static System.Type ARRAY_SHORT    = typeof(short[]);
		/// <remarks/>
		public static System.Type ARRAY_USHORT   = typeof(ushort[]);
		/// <remarks/>
		public static System.Type ARRAY_INT      = typeof(int[]);
		/// <remarks/>
		public static System.Type ARRAY_UINT     = typeof(uint[]);
		/// <remarks/>
		public static System.Type ARRAY_LONG     = typeof(long[]);
		/// <remarks/>
		public static System.Type ARRAY_ULONG    = typeof(ulong[]);
		/// <remarks/>
		public static System.Type ARRAY_FLOAT    = typeof(float[]);
		/// <remarks/>
		public static System.Type ARRAY_DOUBLE   = typeof(double[]);
		/// <remarks/>
		public static System.Type ARRAY_DECIMAL  = typeof(decimal[]);
		/// <remarks/>
		public static System.Type ARRAY_BOOLEAN  = typeof(bool[]);
		/// <remarks/>
		public static System.Type ARRAY_DATETIME = typeof(DateTime[]);
		/// <remarks/>
		public static System.Type ARRAY_STRING   = typeof(string[]);
		/// <remarks/>
		public static System.Type ARRAY_ANY_TYPE = typeof(object[]);
		/// <remarks/>
		public static System.Type ILLEGAL_TYPE   = typeof(Type);

		/// <summary>
		/// Returns an array of all well-known types.
		/// </summary>
		public static System.Type[] Enumerate()
		{
			ArrayList values = new ArrayList();

			FieldInfo[] fields = typeof(OpcLibrary.Com.Type).GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (FieldInfo field in fields)
			{
				values.Add(field.GetValue(typeof(System.Type)));
			}

			return (System.Type[])values.ToArray(typeof(System.Type));
		}
	}
}
