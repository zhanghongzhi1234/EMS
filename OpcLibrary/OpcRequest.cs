using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpcLibrary
{
    /// <summary>
    /// Maintains the state of an asynchronous request.
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// An unique identifier, assigned by the client, for the request.
        /// </summary>
        object Handle { get; }
    }

    public class OpcRequestBase : IRequest
    {
        /// <summary>
		/// The subscription processing the request.
		/// </summary>
        public OpcGroup OpcGroup 
		{
            get { return _group; }
		}
		
		/// <summary>
		/// An unique identifier, assigned by the client, for the request.
		/// </summary>
		public object Handle 
		{
			get { return _handle; }
		}
		
		/// <summary>
		/// Cancels the request, if possible.
		/// </summary>
        public void Cancel(CancelCompleteEventHandler callback) { /*OpcGroup.Cancel(this, callback);*/ }

		#region Constructors
		/// <summary>
		/// Initializes the object with a subscription and a unique id.
		/// </summary>
        public OpcRequestBase(OpcGroup group, object handle)
		{
			_group = group;
			_handle       = handle;
		}
		#endregion
	
		#region Private Members
		private OpcGroup _group = null;
		private object _handle = null;
		#endregion
    }

    public class OpcRequest : OpcRequestBase
    {
        /// <summary>
        /// The unique id assigned by the subscription.
        /// </summary>
        internal int RequestID = 0;

        /// <summary>
        /// The unique id assigned by the server.
        /// </summary>
        internal int CancelID = 0;

        /// <summary>
        /// The callback used when the request completes.
        /// </summary>
        internal Delegate Callback = null;

        /// <summary>
        /// The result filters to use for the request.
        /// </summary>
        internal int Filters = 0;

        /// <summary>
        /// The set of initial results.
        /// </summary>
        internal ItemIdentifier[] InitialResults = null;

        /// <summary>
        /// Initializes the object with a subscription and a unique id.
        /// </summary>
        public OpcRequest(
            OpcGroup group,
            object clientHandle,
            int filters,
            int requestID,
            Delegate callback)
            :
                base(group, clientHandle)
        {
            Filters = filters;
            RequestID = requestID;
            Callback = callback;
            CancelID = 0;
            InitialResults = null;
        }

        /// <summary>
        /// Begins a read request by storing the initial results.
        /// </summary>
        public bool BeginRead(int cancelID, IdentifiedResult[] results)
        {
            CancelID = cancelID;

            ItemValueResult[] values = null;

            // check if results have already arrived.
            if (InitialResults != null)
            {
                if (InitialResults.GetType() == typeof(ItemValueResult[]))
                {
                    values = (ItemValueResult[])InitialResults;
                    InitialResults = results;
                    EndRequest(values);
                    return true;
                }
            }

            // check that at least one valid item existed.
            foreach (IdentifiedResult result in results)
            {
                if (result.ResultID.Succeeded())
                {
                    InitialResults = results;
                    return false;
                }
            }

            // request complete - all items had errors.
            return true;
        }

        /// <summary>
        /// Begins a write request by storing the initial results.
        /// </summary>
        public bool BeginWrite(int cancelID, IdentifiedResult[] results)
        {
            CancelID = cancelID;

            // check if results have already arrived.
            if (InitialResults != null)
            {
                if (InitialResults.GetType() == typeof(IdentifiedResult[]))
                {
                    IdentifiedResult[] callbackResults = (IdentifiedResult[])InitialResults;
                    InitialResults = results;
                    EndRequest(callbackResults);
                    return true;
                }
            }

            // check that at least one valid item existed.
            foreach (IdentifiedResult result in results)
            {
                if (result.ResultID.Succeeded())
                {
                    InitialResults = results;
                    return false;
                }
            }

            // apply filters.		
            for (int ii = 0; ii < results.Length; ii++)
            {
                if ((Filters & (int)ResultFilter.ItemName) == 0) results[ii].ItemName = null;
                if ((Filters & (int)ResultFilter.ItemPath) == 0) results[ii].ItemPath = null;
                if ((Filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null;
            }

            // invoke callback.
            ((WriteCompleteEventHandler)Callback)(Handle, results);

            return true;
        }

        /// <summary>
        /// Begins a refersh request by saving the cancel id.
        /// </summary>
        public bool BeginRefresh(int cancelID)
        {
            // save cancel id.
            CancelID = cancelID;

            // request not complete.
            return false;
        }

        /// <summary>
        /// Completes a read request by processing the values and invoking the callback.
        /// </summary>
        public void EndRequest()
        {
            // check for cancelled request.
            if (typeof(CancelCompleteEventHandler).IsInstanceOfType(Callback))
            {
                ((CancelCompleteEventHandler)Callback)(Handle);
                return;
            }
        }

        /// <summary>
        /// Completes a read request by processing the values and invoking the callback.
        /// </summary>
        public void EndRequest(ItemValueResult[] results)
        {
            // check if the begin request has not completed yet.
            if (InitialResults == null)
            {
                InitialResults = results;
                return;
            }

            // check for cancelled request.
            if (typeof(CancelCompleteEventHandler).IsInstanceOfType(Callback))
            {
                ((CancelCompleteEventHandler)Callback)(Handle);
                return;
            }

            // apply filters.
            for (int ii = 0; ii < results.Length; ii++)
            {
                if ((Filters & (int)ResultFilter.ItemName) == 0) results[ii].ItemName = null;
                if ((Filters & (int)ResultFilter.ItemPath) == 0) results[ii].ItemPath = null;
                if ((Filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null;

                if ((Filters & (int)ResultFilter.ItemTime) == 0)
                {
                    results[ii].Timestamp = DateTime.MinValue;
                    results[ii].TimestampSpecified = false;
                }
            }

            // invoke callback.
            if (typeof(ReadCompleteEventHandler).IsInstanceOfType(Callback))
            {
                ((ReadCompleteEventHandler)Callback)(Handle, results);
            }
        }

        /// <summary>
        /// Completes a write request by processing the values and invoking the callback.
        /// </summary>
        public void EndRequest(IdentifiedResult[] callbackResults)
        {
            // check if the begin request has not completed yet.
            if (InitialResults == null)
            {
                InitialResults = callbackResults;
                return;
            }

            // check for cancelled request.
            if (Callback != null && Callback.GetType() == typeof(CancelCompleteEventHandler))
            {
                ((CancelCompleteEventHandler)Callback)(Handle);
                return;
            }

            // update initial results with callback results.
            IdentifiedResult[] results = (IdentifiedResult[])InitialResults;

            // insert matching value by checking client handle.
            int index = 0;

            for (int ii = 0; ii < results.Length; ii++)
            {
                while (index < callbackResults.Length)
                {
                    // the initial results have the internal handle stores as the server handle.
                    if (callbackResults[ii].ClientHandle.Equals(results[index].ClientHandle))
                    {
                        // swap the handles for return to the client.
                        callbackResults[ii].ServerHandle = results[index].ServerHandle;
                        callbackResults[ii].ClientHandle = results[index].ClientHandle;

                        results[index++] = callbackResults[ii];
                        break;
                    }

                    index++;
                }
            }

            // apply filters.
            for (int ii = 0; ii < results.Length; ii++)
            {
                if ((Filters & (int)ResultFilter.ItemName) == 0) results[ii].ItemName = null;
                if ((Filters & (int)ResultFilter.ItemPath) == 0) results[ii].ItemPath = null;
                if ((Filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null;
            }

            // invoke callback.
            if (Callback != null && Callback.GetType() == typeof(WriteCompleteEventHandler))
            {
                ((WriteCompleteEventHandler)Callback)(Handle, results);
            }
        }
    }
}
