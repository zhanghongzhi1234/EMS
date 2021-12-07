using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using OpcLibrary.Com;


namespace OpcLibrary
{
    public class OpcCallback : OpcRcw.Da.IOPCDataCallback
    {
        /// <summary>
			/// Initializes the object with the containing subscription object.
			/// </summary>
			public OpcCallback(object handle, int filters, OpcGroup group) 
			{ 
				m_handle  = handle; 
				m_filters = filters;
                m_group = group;
			}
			
			/// <summary>
			/// Updates the result filters and subscription handle.
			/// </summary>
			public void SetFilters(object handle, int filters)
			{
				lock (this)
				{
					m_handle  = handle; 
					m_filters = filters;
				}
			}

			/// <summary>
			/// Adds an asynchrounous request.
			/// </summary>
			public void BeginRequest(OpcRequest request)
			{
				lock (this)
				{
					m_requests[request.RequestID] = request;
				}
			}

			/// <summary>
			/// Returns true is an asynchrounous request can be cancelled.
			/// </summary>
			public bool CancelRequest(OpcRequest request)
			{
				lock (this)
				{
					return m_requests.ContainsKey(request.RequestID);
				}
			}

			/// <summary>
			/// Remvoes an asynchrounous request.
			/// </summary>
			public void EndRequest(OpcRequest request)
			{
				lock (this)
				{
					m_requests.Remove(request.RequestID);
				}
			}

			/// <summary>
			/// The handle to return with any callbacks. 
			/// </summary>
			private object m_handle = null;

			/// <summary>
			/// The current request options for the subscription.
			/// </summary>
			private int m_filters = (int)ResultFilter.Minimal;

            private OpcGroup m_group = null;
			/// <summary>
			/// A table of autstanding asynchronous requests.
			/// </summary>
			private Hashtable m_requests = new Hashtable();

			/// <summary>
			/// Raised when data changed callbacks arrive.
			/// </summary>
			public event DataChangedEventHandler DataChanged
			{
				add    {lock (this){ m_dataChanged += value; }}
				remove {lock (this){ m_dataChanged -= value; }}
			}
			/// <remarks/>
			private event DataChangedEventHandler m_dataChanged = null;

			/// <summary>
			/// Called when a data changed event is received.
			/// </summary>
			public void OnDataChange(
				int                  dwTransid,
				int                  hGroup,
				int                  hrMasterquality,
				int                  hrMastererror,
				int                  dwCount,
				int[]                phClientItems,
				object[]             pvValues,
				short[]              pwQualities,
				OpcRcw.Da.FILETIME[] pftTimeStamps,
				int[]                pErrors)
			{
				try
				{
					OpcRequest request = null;

					lock (this)
					{
						// check for an outstanding request.
						if (dwTransid != 0)
						{
							request = (OpcRequest)m_requests[dwTransid];

							if (request != null)
							{
								// remove the request.
								m_requests.Remove(dwTransid);  
							}
						}

						// do nothing if no connections.
						if (m_dataChanged == null) return;

						// unmarshal item values.
						ItemValueResult[] values = UnmarshalValues(
							dwCount, 
							phClientItems, 
							pvValues, 
							pwQualities, 
							pftTimeStamps, 
							pErrors);

						// apply request options.
						//lock (m_group.Items)
						//{
						//	m_group.Items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, values);
						//}

						// invoke the callback.
						m_dataChanged(m_handle, (request != null)?request.Handle:null, values);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			// sends read complete notifications.
			public void OnReadComplete(
				int                  dwTransid,
				int                  hGroup,
				int                  hrMasterquality,
				int                  hrMastererror,
				int                  dwCount,
				int[]                phClientItems,
				object[]             pvValues,
				short[]              pwQualities,
				OpcRcw.Da.FILETIME[] pftTimeStamps,
				int[]                pErrors)
			{
				try
				{
					OpcRequest request = null;
					ItemValueResult[] values  = null;

					lock (this)
					{
						// do nothing if no outstanding requests.
						request = (OpcRequest)m_requests[dwTransid];

						if (request == null)
						{
							return;
						}

						// remove the request.
						m_requests.Remove(dwTransid);              
						
						// unmarshal item values.
						values = UnmarshalValues(
							dwCount, 
							phClientItems, 
							pvValues, 
							pwQualities, 
							pftTimeStamps, 
							pErrors);

						// apply request options.
						lock (m_group.Items)
						{
							m_group.Items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, values);
						}
					}

					// end the request.
					lock (request)
					{
						request.EndRequest(values);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			// handles asynchronous write complete events.
			public void OnWriteComplete(
				int   dwTransid,
				int   hGroup,
				int   hrMastererror,
				int   dwCount,
				int[] phClientItems,
				int[] pErrors)
			{
				try
				{
					OpcRequest  request = null;
					IdentifiedResult[] results = null;

					lock (this)
					{
						// do nothing if no outstanding requests.
						request = (OpcRequest)m_requests[dwTransid];

						if (request == null)
						{
							return;
						}

						// remove the request.
						m_requests.Remove(dwTransid);              
						
						// contruct the item results.
						results = new IdentifiedResult[dwCount];

						for (int ii = 0; ii < results.Length; ii++)
						{
							// lookup the external client handle.
							ItemIdentifier itemID = (ItemIdentifier)m_group.Items[phClientItems[ii]];

							results[ii]                = new IdentifiedResult(itemID);
							results[ii].ClientHandle   = phClientItems[ii];
							results[ii].ResultID       = OpcLibrary.Com.Interop.GetResultID(pErrors[ii]);
							results[ii].DiagnosticInfo = null;

							// convert COM code to unified DA code.
							if (pErrors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
						}
					
						// apply request options.
						lock (m_group.Items)
						{
							m_group.Items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, results);
						}
					}

					// end the request.
					lock (request)
					{
						request.EndRequest(results);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			// handles asynchronous request cancel events.
			public void OnCancelComplete(
				int dwTransid,
				int hGroup)
			{
				try
				{
					OpcRequest request = null;

					lock (this)
					{
						// do nothing if no outstanding requests.
						request = (OpcRequest)m_requests[dwTransid];

						if (request == null)
						{
							return;
						}

						// remove the request.
						m_requests.Remove(dwTransid);
					}

					// end the request.
					lock (request)
					{
						request.EndRequest();
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			/// <summary>
			/// Creates an array of item value result objects from the callback data.
			/// </summary>
			private ItemValueResult[] UnmarshalValues(
				int                  dwCount,
				int[]                phClientItems,
				object[]             pvValues,
				short[]              pwQualities,
				OpcRcw.Da.FILETIME[] pftTimeStamps,
				int[]                pErrors)
			{
				// contruct the item value results.
				ItemValueResult[] values = new ItemValueResult[dwCount];

				for (int ii = 0; ii < values.Length; ii++)
				{
					// lookup the external client handle.
					ItemIdentifier itemID = (ItemIdentifier)m_group.Items[phClientItems[ii]];

					values[ii]                    = new ItemValueResult(itemID);
					//values[ii].ClientHandle       = phClientItems[ii];
					values[ii].Value              = pvValues[ii];
					values[ii].Quality            = new Quality(pwQualities[ii]);
					values[ii].QualitySpecified   = true;
                    FILETIME output = new FILETIME();
                    output.dwLowDateTime = pftTimeStamps[ii].dwLowDateTime;
                    output.dwHighDateTime = pftTimeStamps[ii].dwHighDateTime;
					values[ii].Timestamp          = OpcLibrary.Com.Interop.GetFILETIME(output);
					values[ii].TimestampSpecified = values[ii].Timestamp != DateTime.MinValue;
					values[ii].ResultID           = OpcLibrary.Com.Interop.GetResultID(pErrors[ii]);
					values[ii].DiagnosticInfo     = null;

					// convert COM code to unified DA code.
					if (pErrors[ii] == ResultIDs.E_BADRIGHTS) { values[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results
				return values;
			}
		}
}
