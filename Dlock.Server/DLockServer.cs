using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NTCPMSG.Server;
using NTCPMSG.Event;

using DLock;

namespace DLock.Server
{
    public class DLockServer
    {
        #region fields
        readonly int _TcpPort;
        NTcpListener _Listener;
        #endregion

        #region event handles
        /// <summary>
        /// DataReceived event will be called back when server get message from client which connect to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void ReceiveEventHandler(object sender, ReceiveEventArgs args)
        {
            switch ((DLockEvent)args.Event)
            {
                default:
                    break;
            }
        }

        /// <summary>
        /// RemoteDisconnected event will be called back when specified client disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void DisconnectEventHandler(object sender, DisconnectEventArgs args)
        {
            //Console.WriteLine("Remote socket:{0} disconnected.", args.RemoteIPEndPoint);
        }


        /// <summary>
        /// Accepted event will be called back when specified client connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void AcceptedEventHandler(object sender, AcceptEventArgs args)
        {
            //Console.WriteLine("Remote socket:{0} connected.", args.RemoteIPEndPoint);
        }


        #endregion

        #region private methods


        #endregion

        /// <summary>
        /// constractor
        /// </summary>
        /// <param name="port">host tcp port</param>
        public DLockServer(int port)
        {
            try
            {
                _TcpPort = port;

                _Listener = new NTcpListener(_TcpPort);

                //DataReceived event will be called back when server get message from client which connect to.
                _Listener.DataReceived += new EventHandler<ReceiveEventArgs>(ReceiveEventHandler);

                //RemoteDisconnected event will be called back when specified client disconnected.
                _Listener.RemoteDisconnected += new EventHandler<DisconnectEventArgs>(DisconnectEventHandler);

                //Accepted event will be called back when specified client connected
                _Listener.Accepted += new EventHandler<AcceptEventArgs>(AcceptedEventHandler);

                //Start listening.
                //This function will not block current thread.
                _Listener.Listen();

                DLock.Report.WriteAppLog("DLock host at port {0} successful.", _TcpPort);
            }
            catch (Exception e)
            {
                DLock.Report.WriteErrorLog("Initial DLock server fail.", e);

                throw e;
            }
        }
    }
}
