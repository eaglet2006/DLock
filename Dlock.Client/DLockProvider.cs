using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using NTCPMSG.Client;
using NTCPMSG.Event;

namespace DLock.Client
{
    public class DLockProvider
    {
        object _LockObj = new object();

        SingleConnectionCable _Connection;

        readonly DLock.Client.ObjManager.MutexManager _MutexMangager;

        internal DLock.Client.ObjManager.MutexManager MutexMgr
        {
            get
            {
                return _MutexMangager;
            }
        }

        internal bool Connected
        {
            get
            {
                return _Connection.Connected;
            }
        }

        /// <summary>
        /// DataReceived event will be called back when server get message from client which connect to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void ReceiveEventHandler(object sender, ReceiveEventArgs args)
        {
            switch ((DLockEvent.GlobalEvent)args.Event)
            {
                case DLockEvent.GlobalEvent.MutexEvent:
                    _MutexMangager.EventReceivedHandler(DLockEvent.FromBytes<MutexEvent>(args.Data));
                    break;
            }
        }

        /// <summary>
        /// Ocurred when connection cabled connected.
        /// Do some initial things in this event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void ConnectedEventHandler(object sender, CableConnectedEventArgs args)
        {

        }


        void DisconnectedEventHandler(object sender, DisconnectEventArgs args)
        {
            _MutexMangager.Disconnect();
        }


        private DLockObject CreateObj(string name, DLockEvent.GlobalEvent globalEvent)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name can't be NULL or Empty");
            }

            if (!_Connection.Connected)
            {
                throw new DLockException("Disconnect to DLock server", DLockException.ErrorType.Disconnect);
            }

            DLockObject result = null;

            switch (globalEvent)
            {
                case DLockEvent.GlobalEvent.MutexEvent:
                    result = new Mutex(this, name);
                    break;
                    
            }

            return result;
        }

        internal DLockProvider(IPEndPoint ipEndPoint)
        {
            _MutexMangager = new ObjManager.MutexManager(this);

            _Connection = new SingleConnectionCable(ipEndPoint, 1);

            _Connection.ReceiveEventHandler += new EventHandler<ReceiveEventArgs>(ReceiveEventHandler);
            _Connection.ConnectedEventHandler += new EventHandler<CableConnectedEventArgs>(ConnectedEventHandler);
            _Connection.RemoteDisconnected += new EventHandler<DisconnectEventArgs>(DisconnectedEventHandler);
            _Connection.Connect(5000);
        }

        internal void ASend(DLockEvent dEvent)
        {
            _Connection.AsyncSend((uint)dEvent.GetGlobalEvent(), dEvent.GetBytes());
        }

        #region public methods

        /// <summary>
        /// Create a distributed mutex
        /// </summary>
        /// <param name="name">mutex name</param>
        /// <returns></returns>
        public DLock.Client.Mutex CreateMutex(string name)
        {
            return CreateObj(name, DLockEvent.GlobalEvent.MutexEvent) as Mutex;
        }

        #endregion
    }
}
