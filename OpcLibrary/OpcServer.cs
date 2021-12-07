using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using OpcRcw.Da;
using OpcLibrary.Com;

namespace OpcLibrary
{
    public class OpcServer
    {
        private string m_clsid;
        private string m_hostName;

        private bool m_isConnected;
        private object m_server;

        private ArrayList m_groups;

        public OpcServer(string clsid, string hostName)
        {
            this.m_clsid = clsid;
            this.m_hostName = hostName;
            this.m_isConnected = false;

            this.m_groups = new ArrayList();
        }

        public void Connect()
        {
            Guid guid = new Guid(this.m_clsid);
            try
            {
                m_server = OpcLibrary.Com.Interop.CreateInstance(guid, m_hostName, null);
                IOPCServer server = (IOPCServer)m_server;
                m_isConnected = true;
            }
            catch (Exception e)
            {
                throw new Exception("Could not connect to server.");
            }
        }

        public void Disconnect()
        {
            // remove group first
            if (this.m_groups.Count > 0)
            {
                ArrayList grps = new ArrayList(this.m_groups);
                foreach (OpcGroup grp in grps)
                {
                    this.RemoveGroup(grp);
                }
                grps.Clear();
            }

            if (m_server != null)
            {
                OpcLibrary.Com.Interop.ReleaseServer(m_server);
                m_server = null;
            }

            m_isConnected = false;
        }

        public OpcGroup AddGroup(string groupName, int updateRate, bool bActive)
        {
            OpcGroup grp = new OpcGroup(groupName, updateRate, bActive);
            // initialize arguments.
            Guid iid = typeof(IOPCItemMgt).GUID;
            object group = null;

            int serverHandle = 0;
            int revisedUpdateRate = 0;

            GCHandle hDeadband = GCHandle.Alloc(grp.Deadband, GCHandleType.Pinned);

            // invoke COM method.
            try
            {
                ((IOPCServer)m_server).AddGroup(
                    (grp.Name != null) ? grp.Name : "",
                    (grp.Active) ? 1 : 0,
                    grp.UpdateRate,
                    0,
                    IntPtr.Zero,
                    hDeadband.AddrOfPinnedObject(),
                    OpcLibrary.Com.Interop.GetLocale(grp.Locale),
                    out serverHandle,
                    out revisedUpdateRate,
                    ref iid,
                    out group);
            }
            catch (Exception e)
            {
                throw OpcLibrary.Com.Interop.CreateException("IOPCServer.AddGroup", e);
            }
            finally
            {
                if (hDeadband.IsAllocated) hDeadband.Free();
            }

            // set the revised update rate.
            if (revisedUpdateRate > grp.UpdateRate)
                grp.UpdateRate = revisedUpdateRate;

            // save server handle.
            grp.ServerHandle = serverHandle;

            // save group handle
            grp.ComGroup = group;

            // add group to server list
            this.m_groups.Add(grp);

            return grp;
        }

        public void RemoveGroup(OpcGroup group)
        {

            // invoke COM method.
            try
            {
                ((IOPCServer)m_server).RemoveGroup((int)group.ServerHandle, 0);
            }
            catch (Exception e)
            {
                throw OpcLibrary.Com.Interop.CreateException("IOPCServer.RemoveGroup", e);
            }


            this.m_groups.Remove(group);
        }

        public OpcGroup FindGroupByName(string groupName)
        {
            foreach (OpcGroup grp in this.m_groups)
            {
                if (grp.Name == groupName)
                {
                    return grp;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the current server status.
        /// </summary>
        /// <returns>The current server status.</returns>
        public ServerStatus GetStatus()
        {
            ServerStatus output = null;
            lock (this)
            {
                if (m_server == null) throw new Exception("The remote server is not currently connected.");

                // initialize arguments.
                IntPtr pStatus = IntPtr.Zero;

                // invoke COM method.
                try
                {
                    ((IOPCServer)m_server).GetStatus(out pStatus);
                }
                catch (Exception e)
                {
                    throw Interop.CreateException("IOPCServer.GetStatus", e);
                }

                
                if (pStatus != IntPtr.Zero)
                {
                    OpcRcw.Da.OPCSERVERSTATUS status = (OpcRcw.Da.OPCSERVERSTATUS)Marshal.PtrToStructure(pStatus, typeof(OpcRcw.Da.OPCSERVERSTATUS));

                    output = new ServerStatus();

                    output.VendorInfo = status.szVendorInfo;
                    output.ProductVersion = String.Format("{0}.{1}.{2}", status.wMajorVersion, status.wMinorVersion, status.wBuildNumber);
                    output.ServerState = (serverState)status.dwServerState;
                    output.StatusInfo = null;
                    output.StartTime = Interop.GetFILETIME(OpcLibrary.Com.Convert.GetFileTime(status.ftStartTime));
                    output.CurrentTime = Interop.GetFILETIME(OpcLibrary.Com.Convert.GetFileTime(status.ftCurrentTime));
                    output.LastUpdateTime = Interop.GetFILETIME(OpcLibrary.Com.Convert.GetFileTime(status.ftLastUpdateTime));

                    Marshal.DestroyStructure(pStatus, typeof(OpcRcw.Da.OPCSERVERSTATUS));
                    Marshal.FreeCoTaskMem(pStatus);
                    pStatus = IntPtr.Zero;
                }
            }
            return output;
        }

        #region Properties
        public string Clsid
        {
            get { return this.m_clsid; }
            set { this.m_clsid = value; }
        }

        public string HostName
        {
            get { return this.m_hostName; }
            set { this.m_hostName = value; }
        }

        public bool IsConnected
        {
            get { return this.m_isConnected; }
            set { this.m_isConnected = value; }
        }
        #endregion
    }
}
