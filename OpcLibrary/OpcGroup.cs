using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
//using System.Windows.Forms;

using OpcRcw.Da;
using OpcLibrary.Com;

namespace OpcLibrary
{
   public  class OpcGroup
    {
       private object m_group;
       private object m_clientHandle;
       private object m_serverHandle;

       private string m_name;
       private int m_updateRate;
       private string m_locale;
       private bool m_active;
       private float m_deadband;
       private int m_filters = (int)ResultFilter.All;
       private int m_counter = 0;

       private OpcCallback m_callback = null;
       protected ConnectionPoint m_connection = null;
       private ItemTable m_items = new ItemTable();

       public OpcGroup(string name, int updateRate, bool active)
       {
           this.m_name = name;
           this.m_updateRate = updateRate;
           this.m_active = active;
           this.m_deadband = 0;
           this.m_locale = string.Empty;
           this.m_clientHandle = Guid.NewGuid().ToString();

           m_callback = new OpcCallback(this.ClientHandle, 0, this);
       }

       public ItemResult[] AddItems(string[] itemIds)
       {
           int count = itemIds.Count();
           if (count <= 0)
               return new ItemResult[0];

           // construct OpcItems
           //OpcRcw.Da.OPCITEMDEF[] output = null;
           OpcItem[] items = new OpcItem[count];

           for (int ii = 0; ii < count; ii++)
           {
               items[ii] = new OpcItem();
               items[ii].ItemName = itemIds[ii];
               items[ii].ClientHandle = Guid.NewGuid();
           }

           // get opc item def
           OpcRcw.Da.OPCITEMDEF[] definitions = GetOPCITEMDEFs(items);

           // initialize output parameters.
           IntPtr pResults = IntPtr.Zero;
           IntPtr pErrors = IntPtr.Zero;

           try
           {
               ((IOPCItemMgt)m_group).AddItems(
                   count,
                   definitions,
                   out pResults,
                   out pErrors);
           }
           catch (Exception e)
           {
               throw OpcLibrary.Com.Interop.CreateException("IOPCItemMgt.AddItems", e);
           }

           // unmarshal output parameters.
           int[] serverHandles = this.GetItemResults(ref pResults, count, true);
           int[] errors = OpcLibrary.Com.Interop.GetInt32s(ref pErrors, count, true);

           // construct result list.
           ItemResult[] results = new ItemResult[count];

           for (int ii = 0; ii < count; ii++)
           {
               // create a new ResultIDs.
               results[ii] = new ItemResult(items[ii]);

               // save server handles.
               results[ii].ServerHandle = serverHandles[ii];
               //results[ii].ClientHandle = definitions[ii].hClient;

               // items created active by default.
               if (!results[ii].ActiveSpecified)
               {
                   results[ii].Active = true;
                   results[ii].ActiveSpecified = true;
               }

               // update result id.
               results[ii].ResultID = OpcLibrary.Com.Interop.GetResultID(errors[ii]);
               results[ii].DiagnosticInfo = null;

               // save client handle.
               results[ii].ClientHandle = items[ii].ClientHandle;

               // add new item table.
               if (results[ii].ResultID.Succeeded())
               {
                   lock (m_items)
                   {
                       m_items[items[ii].ClientHandle.GetHashCode()] = new ItemIdentifier(results[ii]);
                   }

                   // restore internal handle.
                  // results[ii].ClientHandle = definitions[ii].hClient;
               }
           }

           // return results.
           lock (m_items)
           {
               return (ItemResult[])m_items.ApplyFilters(m_filters, results);
           }
       }

       public IdentifiedResult[] RemoveItems(object[] itemClientHandles)
       {
           if (itemClientHandles == null) throw new ArgumentNullException("items");

           // check if nothing to do.
           if (itemClientHandles.Length == 0)
           {
               return new IdentifiedResult[0];
           }

           lock (this)
           {
               if (this.m_group == null)
               {
                  // MessageBox.Show("No connection!");
                   return new IdentifiedResult[0];
               }

               // get item ids.
               ItemIdentifier[] itemIDs = null;
               lock (m_items)
               {
                   itemIDs = m_items.GetItemIDs(itemClientHandles);
               }


               // fetch server handles.
               int count = itemClientHandles.Length;
               int[] serverHandles = new int[count];
               for (int ii = 0; ii < count; ii++)
               {
                   serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
               }

               // initialize output parameters.
               IntPtr pErrors = IntPtr.Zero;

               try
               {
                   ((IOPCItemMgt)this.m_group).RemoveItems(count, serverHandles, out pErrors);
               }
               catch (Exception e)
               {
                   throw OpcLibrary.Com.Interop.CreateException("IOPCItemMgt.RemoveItems", e);
               }

               // unmarshal output parameters.
               int[] errors = OpcLibrary.Com.Interop.GetInt32s(ref pErrors, count, true);

               // process results.
               IdentifiedResult[] results = new IdentifiedResult[count];

               lock (m_items)
               {
                   for (int ii = 0; ii < count; ii++)
                   {
                       results[ii] = new IdentifiedResult(itemIDs[ii]);

                       results[ii].ResultID = OpcLibrary.Com.Interop.GetResultID(errors[ii]);
                       results[ii].DiagnosticInfo = null;

                       // flag item for removal from local list.
                       if (results[ii].ResultID.Succeeded())
                       {
                           m_items[results[ii].ClientHandle.GetHashCode()] = null;
                       }
                   }
               }

               return results;
           }
       }

       public IdentifiedResult[] AsyncRead(
            object[] itemClientHandles,
            object requestHandle,
            ReadCompleteEventHandler callback,
            out IRequest request)
       {
           if (itemClientHandles == null) throw new ArgumentNullException("items");
           if (callback == null) throw new ArgumentNullException("callback");

           request = null;

           // check if nothing to do.
           if (itemClientHandles.Length == 0)
           {
               return new IdentifiedResult[0];
           }

           lock (this)
           {
               if (this.m_group == null) throw new Exception("The remote server is not currently connected.");

               // ensure a callback connection is established with the server.
               if (m_connection == null)
               {
                   Advise();
               }

               // get item ids.
               ItemIdentifier[] itemIDs = null;

               lock (m_items)
               {
                   itemIDs = m_items.GetItemIDs(itemClientHandles);
               }

               // create request object.
               OpcRequest internalRequest = new OpcRequest(
                   this,
                   requestHandle,
                   m_filters,
                   m_counter++,
                   callback);

               // register request with callback object.
               m_callback.BeginRequest(internalRequest);
               request = internalRequest;

               // begin read request.
               IdentifiedResult[] results = null;
               int cancelID = 0;

               try
               {
                   results = BeginRead(itemIDs, internalRequest.RequestID, out cancelID);
               }
               catch (Exception e)
               {
                   m_callback.EndRequest(internalRequest);
                   throw e;
               }

               // apply request options.
               //lock (m_items)
               //{
               //    m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, results);
               //}

               lock (internalRequest)
               {
                   // check if all results have already arrived - this invokes the callback if this is the case.
                   if (internalRequest.BeginRead(cancelID, results))
                   {
                       m_callback.EndRequest(internalRequest);
                       request = null;
                   }
               }

               // return initial results.
               return results;
           }
       }

       /// <summary>
       /// Begins an asynchronous read of a set of items using DA2.0 interfaces.
       /// </summary>
       private IdentifiedResult[] BeginRead(
           ItemIdentifier[] itemIDs,
           int requestID,
           out int cancelID)
       {
           try
           {
               // marshal input parameters.
               int[] serverHandles = new int[itemIDs.Length];

               for (int ii = 0; ii < itemIDs.Length; ii++)
               {
                   serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
               }

               // initialize output parameters.
               IntPtr pErrors = IntPtr.Zero;

               ((IOPCAsyncIO2)m_group).Read(
                   itemIDs.Length,
                   serverHandles,
                   requestID,
                   out cancelID,
                   out pErrors);

               // unmarshal output parameters.
               int[] errors = OpcLibrary.Com.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

               // create item results.
               IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

               for (int ii = 0; ii < itemIDs.Length; ii++)
               {
                   results[ii] = new IdentifiedResult(itemIDs[ii]);
                   results[ii].ResultID = OpcLibrary.Com.Interop.GetResultID(errors[ii]);
                   results[ii].DiagnosticInfo = null;

                   // convert COM code to unified DA code.
                   if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }
               }

               // return results.
               return results;
           }
           catch (Exception e)
           {
               throw OpcLibrary.Com.Interop.CreateException("IOPCAsyncIO2.Read", e);
           }
       }


       public IdentifiedResult[] AsyncWrite(
            ItemValue[] items,
            object requestHandle,
            WriteCompleteEventHandler callback,
            out IRequest request)
       {
           if (items == null) throw new ArgumentNullException("items");
           if (callback == null) throw new ArgumentNullException("callback");

           request = null;

           // check if nothing to do.
           if (items.Length == 0)
           {
               return new IdentifiedResult[0];
           }

           lock (this)
           {
               if (m_group == null) throw new Exception("The remote server is not currently connected."); 

               // ensure a callback connection is established with the server.
               if (m_connection == null)
               {
                   Advise();
               }

 
               // get item ids.
               ItemIdentifier[] itemIDs = null;

               lock (m_items)
               {
                   itemIDs = m_items.GetItemIDs(items);
               }

               // create request object.
               OpcRequest internalRequest = new OpcRequest(
                   this,
                   requestHandle,
                   m_filters,
                   m_counter++,
                   callback);

               // register request with callback object.
               m_callback.BeginRequest(internalRequest);
               request = internalRequest;

               // begin write request.
               IdentifiedResult[] results = null;
               int cancelID = 0;

               try
               {
                   results = BeginWrite(itemIDs, items, internalRequest.RequestID, out cancelID);
               }
               catch (Exception e)
               {
                   m_callback.EndRequest(internalRequest);
                   throw e;
               }

               // apply request options.
               lock (m_items)
               {
                   m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, results);
               }

               lock (internalRequest)
               {
                   // check if all results have already arrived - this invokes the callback if this is the case.
                   if (internalRequest.BeginWrite(cancelID, results))
                   {
                       m_callback.EndRequest(internalRequest);
                       request = null;
                   }
               }

               // return initial results.
               return results;
           }
       }

       private IdentifiedResult[] BeginWrite(
            ItemIdentifier[] itemIDs,
            ItemValue[] items,
            int requestID,
            out int cancelID)
       {
           cancelID = 0;

           ArrayList validItems = new ArrayList();
           ArrayList validValues = new ArrayList();

           // construct initial result list.
           IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

           for (int ii = 0; ii < itemIDs.Length; ii++)
           {
               results[ii] = new IdentifiedResult(itemIDs[ii]);

               results[ii].ResultID = ResultID.S_OK;
               results[ii].DiagnosticInfo = null;

               if (items[ii].QualitySpecified || items[ii].TimestampSpecified)
               {
                   results[ii].ResultID = ResultID.Da.E_NO_WRITEQT;
                   results[ii].DiagnosticInfo = null;
                   continue;
               }

               validItems.Add(results[ii]);
               validValues.Add(OpcLibrary.Com.Interop.GetVARIANT(items[ii].Value));
           }

           // check if any valid items exist.
           if (validItems.Count == 0)
           {
               return results;
           }

           try
           {
               // initialize input parameters.
               int[] serverHandles = new int[validItems.Count];

               for (int ii = 0; ii < validItems.Count; ii++)
               {
                   serverHandles[ii] = (int)((IdentifiedResult)validItems[ii]).ServerHandle;
               }

               // write to sever.
               IntPtr pErrors = IntPtr.Zero;

               ((IOPCAsyncIO2)m_group).Write(
                   validItems.Count,
                   serverHandles,
                   (object[])validValues.ToArray(typeof(object)),
                   requestID,
                   out cancelID,
                   out pErrors);

               // unmarshal results.
               int[] errors = OpcLibrary.Com.Interop.GetInt32s(ref pErrors, validItems.Count, true);

               // create result list.
               for (int ii = 0; ii < validItems.Count; ii++)
               {
                   IdentifiedResult result = (IdentifiedResult)validItems[ii];

                   result.ResultID = OpcLibrary.Com.Interop.GetResultID(errors[ii]);
                   result.DiagnosticInfo = null;

                   // convert COM code to unified DA code.
                   if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
               }
           }
           catch (Exception e)
           {
               throw OpcLibrary.Com.Interop.CreateException("IOPCAsyncIO2.Write", e);
           }

           // return results.
           return results;
       }

       public void Refresh()
       {
           lock (this)
           {
               if (m_group == null) throw new Exception("The remote server is not currently connected.");

               try
               {
                   int cancelID = 0;
                   ((IOPCAsyncIO2)m_group).Refresh2(OPCDATASOURCE.OPC_DS_CACHE, ++m_counter, out cancelID);
               }
               catch (Exception e)
               {
                   throw OpcLibrary.Com.Interop.CreateException("IOPCAsyncIO2.Refresh", e);
               }
           }
       }

       /// <summary>
       /// Changes the state of a group.
       /// </summary>
       /// <param name="masks">A bit mask that indicates which elements of the group state are changing.</param>
       /// <param name="state">The new group state.</param>
       /// <returns>The actual group state after applying the changes.</returns>
       public GroupState SetState(int masks, GroupState state)
       {
           if (state == null) throw new ArgumentNullException("state");

           lock (this)
           {
               // update the group name.
               if ((masks & (int)StateMask.Name) != 0 && state.Name != m_name)
               {
                   try
                   {
                       ((IOPCGroupStateMgt)m_group).SetName(state.Name);
                       m_name = state.Name;
                   }
                   catch (Exception e)
                   {
                       throw Interop.CreateException("IOPCGroupStateMgt.SetName", e);
                   }
               }

               // update the client handle.
               if ((masks & (int)StateMask.ClientHandle) != 0)
               {
                   m_clientHandle = state.ClientHandle;

                   // update the callback object.
                   m_callback.SetFilters(m_clientHandle, m_filters);
               }

               // update the group state.
               int active = (state.Active) ? 1 : 0;
               int localeID = ((masks & (int)StateMask.Locale) != 0) ? Interop.GetLocale(state.Locale) : 0;

               GCHandle hActive = GCHandle.Alloc(active, GCHandleType.Pinned);
               GCHandle hLocale = GCHandle.Alloc(localeID, GCHandleType.Pinned);
               GCHandle hUpdateRate = GCHandle.Alloc(state.UpdateRate, GCHandleType.Pinned);
               GCHandle hDeadband = GCHandle.Alloc(state.Deadband, GCHandleType.Pinned);

               int updateRate = 0;

               try
               {
                   ((IOPCGroupStateMgt)m_group).SetState(
                       ((masks & (int)StateMask.UpdateRate) != 0) ? hUpdateRate.AddrOfPinnedObject() : IntPtr.Zero,
                       out updateRate,
                       ((masks & (int)StateMask.Active) != 0) ? hActive.AddrOfPinnedObject() : IntPtr.Zero,
                       IntPtr.Zero,
                       ((masks & (int)StateMask.Deadband) != 0) ? hDeadband.AddrOfPinnedObject() : IntPtr.Zero,
                       ((masks & (int)StateMask.Locale) != 0) ? hLocale.AddrOfPinnedObject() : IntPtr.Zero,
                       IntPtr.Zero);
               }
               catch (Exception e)
               {
                   throw Interop.CreateException("IOPCGroupStateMgt.SetState", e);
               }
               finally
               {
                   if (hActive.IsAllocated) hActive.Free();
                   if (hLocale.IsAllocated) hLocale.Free();
                   if (hUpdateRate.IsAllocated) hUpdateRate.Free();
                   if (hDeadband.IsAllocated) hDeadband.Free();
               }

               // return the current state.
               return GetState();
           }
       }

       /// <summary>
       /// Returns the current state of the subscription.
       /// </summary>
       /// <returns>The current state of the subscription.</returns>
       public GroupState GetState()
       {
           lock (this)
           {
               GroupState state = new GroupState();

               state.ClientHandle = m_clientHandle;

               try
               {
                   string name = null;
                   int active = 0;
                   int updateRate = 0;
                   float deadband = 0;
                   int timebias = 0;
                   int localeID = 0;
                   int clientHandle = 0;
                   int serverHandle = 0;

                   ((IOPCGroupStateMgt)m_group).GetState(
                       out updateRate,
                       out active,
                       out name,
                       out timebias,
                       out deadband,
                       out localeID,
                       out clientHandle,
                       out serverHandle);

                   state.Name = name;
                   state.ServerHandle = serverHandle;
                   state.Active = active != 0;
                   state.UpdateRate = updateRate;
                   state.Deadband = deadband;
                   state.Locale = Interop.GetLocale(localeID);

                   // cache the name separately.
                   m_name = state.Name;
                 
               }
               catch (Exception e)
               {
                   throw Interop.CreateException("IOPCGroupStateMgt.GetState", e);
               }

               return state;
           }
       }

       /// <summary>
       /// An event to receive data change updates.
       /// </summary>
       public event DataChangedEventHandler DataChanged
       {
           add { lock (this) { m_callback.DataChanged += value; Advise(); } }
           remove { lock (this) { m_callback.DataChanged -= value; Unadvise(); } }
       }

       private OpcRcw.Da.OPCITEMDEF[] GetOPCITEMDEFs(OpcItem[] input)
       {
           OpcRcw.Da.OPCITEMDEF[] output = null;

           if (input != null)
           {
               output = new OpcRcw.Da.OPCITEMDEF[input.Length];

               for (int ii = 0; ii < input.Length; ii++)
               {
                   output[ii] = new OpcRcw.Da.OPCITEMDEF();

                   output[ii].szItemID = input[ii].ItemName;
                   output[ii].szAccessPath = (input[ii].ItemPath == null) ? String.Empty : input[ii].ItemPath;
                   output[ii].bActive =  1;
                   output[ii].vtRequestedDataType = (short)VarEnum.VT_EMPTY;//(short)OpcLibrary.Com.Interop.GetType(input[ii].ReqType);
                   output[ii].hClient = input[ii].ClientHandle.GetHashCode();
                   output[ii].dwBlobSize = 0;
                   output[ii].pBlob = IntPtr.Zero;
               }
           }

           return output;
       }

       private int[] GetItemResults(ref IntPtr pInput, int count, bool deallocate)
       {
           int[] output = null;

           if (pInput != IntPtr.Zero && count > 0)
           {
               output = new int[count];

               IntPtr pos = pInput;

               for (int ii = 0; ii < count; ii++)
               {
                   OpcRcw.Da.OPCITEMRESULT result = (OpcRcw.Da.OPCITEMRESULT)Marshal.PtrToStructure(pos, typeof(OpcRcw.Da.OPCITEMRESULT));

                   output[ii] = result.hServer;

                   if (deallocate)
                   {
                       Marshal.FreeCoTaskMem(result.pBlob);
                       result.pBlob = IntPtr.Zero;

                       Marshal.DestroyStructure(pos, typeof(OpcRcw.Da.OPCITEMRESULT));
                   }

                   pos = (IntPtr)(pos.ToInt32() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMRESULT)));
               }

               if (deallocate)
               {
                   Marshal.FreeCoTaskMem(pInput);
                   pInput = IntPtr.Zero;
               }
           }

           return output;
       }


       /// <summary>
       /// Establishes a connection point callback with the COM server.
       /// </summary>
       private void Advise()
       {
           if (m_connection == null)
           {
               m_connection = new ConnectionPoint(m_group, typeof(OpcRcw.Da.IOPCDataCallback).GUID);
               m_connection.Advise(m_callback);
           }
       }

       /// <summary>
       /// Closes a connection point callback with the COM server.
       /// </summary>
       private void Unadvise()
       {
           if (m_connection != null)
           {
               if (m_connection.Unadvise() == 0)
               {
                   m_connection.Dispose();
                   m_connection = null;
               }
           }
       }

       #region Properties
       public object ComGroup
       {
           get { return this.m_group; }
           set { this.m_group = value; }
       }

       public ItemTable Items
       {
           get { return this.m_items; }
       }
       /// <summary>
       /// A unique name for the subscription controlled by the client.
       /// </summary>
       public string Name
       {
           get { return this.m_name; }
           set { this.m_name = value; }
       }

       /// <summary>
       /// The rate at which the server checks of updates to send to the client.
       /// </summary>
       public int UpdateRate
       {
           get { return this.m_updateRate; }
           set { this.m_updateRate = value; }
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
       /// The minimum percentage change required to trigger a data update for an item.
       /// </summary>
       public float Deadband
       {
           get { return m_deadband; }
           set { m_deadband = value; }
       }
       #endregion
    }
}
